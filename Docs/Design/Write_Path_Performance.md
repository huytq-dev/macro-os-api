# Hiệu năng đường ghi: POST /food-logs/entries và POST /weights

## Triệu chứng
Mỗi lần POST entry/weight mất ~3–5 s trên DB ở xa dù tính toán chỉ vài chục ms.

## Nguyên nhân (đo bằng đếm lệnh SQL + giả lập RTT)
Thời gian bị chi phối bởi **số vòng đi về tới DB**, không phải CPU hay thời gian chạy SQL (tổng thời gian trong SQL chỉ vài chục ms).

| Đường | Số lệnh SQL trước | Phần lớn là |
|---|---|---|
| POST entry | 18 | dựng lại aggregate dinh dưỡng + snapshot expenditure/daily progress (≈10 truy vấn) + tra mã chất ×2 + nạp món/serving + nạp hồ sơ ~75 chất |
| POST weight | 8–14 | đọc cân nặng nhiều lần, lưu nhiều lần, rồi dựng lại expenditure |

Với RTT 200 ms: 18 lệnh ≈ 3.6 s.

## Sửa (kết quả cuối cùng không đổi)
1. **(ĐÃ THAY THẾ bởi A7, xem `Docs/Design/A7_Derived_Data_Rebuild_Design.md` ở gốc MacroOS: phần gần dựng đồng bộ có dừng hội tụ + trần dòng, phần còn lại là job bền `RecalculationJob`; không còn `DerivedDataScheduler` trong bộ nhớ và `Algorithm:RebuildDerivedInBackground`.)** Dựng dữ liệu dẫn xuất ở nền (`IDerivedDataScheduler`, `DerivedDataScheduler`): sau khi lưu dữ liệu nguồn (entry/cân nặng), request trả về ngay; nền chạy `NutritionAggregates.RefreshAsync` + `ExpenditureRebuilder.RebuildAsync` + lưu, trong scope/DbContext riêng.
   - Mỗi user tối đa MỘT worker; yêu cầu đến lúc đang chạy được gộp (ngày sớm nhất, hợp các ngày) và xử lý ngay sau đó → hai POST liên tiếp không đua nhau chèn cùng snapshot.
   - Lỗi: ghi log, thử lại 1 lần sau 1 s; dữ liệu nguồn không bị ảnh hưởng.
   - Tắt bằng `Algorithm:RebuildDerivedInBackground = false` (quay về đồng bộ như cũ). `UnitOfWork` dựng thủ công (test/công cụ dòng lệnh) luôn đồng bộ.
2. **Cân nặng: một lần đọc, một lần lưu** (`UpsertWeightEntryCommandHandler`, `DeleteWeightEntryCommandHandler`, `WeightTrendRefresher.Apply`): đọc mọi dòng cân của user một lần, tính trend trong bộ nhớ, `SaveChanges` một lần.
3. **Cache tham chiếu** (`IReferenceCache`, `UnitOfWork.Cache`): mã chất ↔ id (10 phút) và món hệ thống + hồ sơ chất (5 phút) trong `FoodLogEntryFactory.LoadAsync`. Giá trị cache chỉ đọc, không gắn vào DbContext; món của user (custom/quick-add) luôn đọc DB.

## Hành vi cần biết (eventually consistent)
Các bảng dẫn xuất — `NutritionDayAggregate` (Overview tháng/năm), `ExpenditureSnapshot`/`DailyProgressSnapshot` (Expenditure, Dashboard) — trễ vài giây sau POST. Dữ liệu nguồn đúng ngay khi phản hồi: tổng ngày của `FoodLog`, entry, cân nặng và trend. FE nên refetch dashboard/expenditure sau vài giây nếu cần số mới nhất ngay.
App dừng đột ngột giữa lúc nền chạy thì lượt đó mất; lần ghi kế tiếp (hoặc dựng lại thủ công) dựng lại đúng vì phép dựng idempotent và tính từ dữ liệu nguồn.
Món hệ thống đổi (reseed/fix-usda-servings) hiện sau tối đa 5 phút ở đường ghi.

## Số đo (DB cục bộ, 120 ngày dữ liệu, RTT giả lập mỗi lệnh)
| | RTT 0 | RTT 80 | RTT 200 |
|---|---|---|---|
| POST entry (trước, ước tính theo 18 lệnh) | ~20–60 ms | ~1.7 s | ~3.6 s |
| POST entry (sau) | 10–35 ms | ~280 ms | ~640 ms |
| POST weight (trước, 8–14 lệnh) | — | ~0.75 s | ~1.6–2.8 s |
| POST weight (sau) | 3–5 ms | ~95 ms | ~205 ms |

(Lần gọi đầu sau khi khởi động app chậm hơn do JIT/dựng model EF, không liên quan DB.)

## Kiểm
- `dotnet run --project tests/AlgorithmChecks`: kiểm `ReferenceCache` (TTL, sai kiểu, thiếu khoá) và mọi kiểm cũ.
- Scratch (SQL Server cục bộ): dữ liệu dẫn xuất dựng nền == dựng đồng bộ từ đầu (kể cả hai POST đồng thời cùng ngày), so từng dòng `ExpenditureSnapshot`, `DailyProgressSnapshot`, `NutritionDayAggregate`, `WeightEntry`.
