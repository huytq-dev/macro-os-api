# "4 món biến mất" (Food Log) + tối ưu Food Search — BE report (2026-10-26)

Yêu cầu của user: "khi tôi log food tôi log 4 món nhưng mà khi quay lại thì nó lại không có ấy" và "tối ưu cái phần search food … FE và BE". Mọi đo/chạy trên LocalDB TẠM; không API, không DB dùng chung, không migration, không commit.

## A. Món biến mất: nguyên nhân gốc

**Tái hiện (handler THẬT, mỗi request một DbContext + UnitOfWork như prod, `tests/FoodLogLostEntriesRepro`):**

| Kịch bản (cùng một ngày) | Trước khi sửa | Sau khi sửa |
|---|---|---|
| 4 POST entries **tuần tự** (đường `logPlate` của FE) | đủ 4 | đủ 4 |
| 4 POST entries **song song**, ngày chưa có dòng FoodLog (5 vòng) | **5/5 vòng sai**: chỉ 1–3 request OK, FE thấy 1–3 món. Lỗi `DbUpdateException … duplicate key … IX_FoodLogs_UserId_LogDate` hoặc `DbUpdateConcurrencyException` | 5/5 vòng đủ 4, 4/4 request OK |
| 4 POST entries song song, ngày đã có dòng FoodLog | 5/5 vòng sai (`DbUpdateConcurrencyException`, FE thấy 2–3/5) | 5/5 đủ 5 |
| 4 quick-add song song | 5/5 sai (chỉ 1/4 OK, `duplicate key`) | 5/5 đủ 4 |
| 2 batch (2+2 món, khác key) song song | 5/5 sai (1/2 OK) | 5/5 đủ 4 |
| hỗn hợp song song: 3 thêm + lô 3 món + sửa gram + xoá 1 món | 5/5 sai (3/6 OK, FE thấy 2/7) | 5/5 đúng 7 |
| **Dùng lại IdempotencyKey cho lô KHÁC** (2 món, rồi 2 món khác) | `200 Replayed=true`, **2 món sau bị bỏ im lặng**, FE thấy 2/4 | lô hai bị **409 `IDEMPOTENCY_KEY_REUSED`**, lô một còn nguyên |
| Batch 4 món + replay cùng key | đúng 4 | đúng 4 (replay trả cùng id) |
| Qua `IMediator` thật + `AddApplicationServices` (pipeline đăng ký như API), 4 Send song song | (không chạy: chưa có behavior) | 5/5 đủ 4 |

**Cơ chế (hai lỗ hổng, cùng đường dùng chung `GetOrCreateAsync` → `SaveAndRebuildAsync`):**
1. **Ghi đồng thời cùng ngày không được tuần tự hoá.** Mỗi request nạp/tạo dòng `FoodLogs` của (user, ngày), thêm entry, `RecalcTotals(log)` rồi `SaveChanges` cập nhật tổng ngày của CÙNG dòng đó. Các request đầu tiên của một ngày mới cùng INSERT dòng FoodLog ⇒ vi phạm UNIQUE `(UserId, LogDate)` (lỗi, request thua không lưu món); các request sau đụng `RowVersion` của dòng FoodLog ⇒ `DbUpdateConcurrencyException`. Mỗi request thua trả LỖI và món của nó KHÔNG được lưu; không có retry. Người dùng bấm "+" liên tiếp / nhiều tab / FE gọi `logPlate` chồng nhau thấy "đã thêm" rồi quay lại chỉ còn một phần.
2. **IdempotencyKey không gắn với nội dung lô.** `POST /food-logs/entries/batch` coi mọi lần gọi cùng key là replay và trả `200 Replayed=true` kể cả khi lô là món KHÁC, nên các món mới bị bỏ mà client tưởng đã lưu.

**Đã kiểm và KHÔNG phải nguyên nhân (có bằng chứng):** tuần tự 4 POST luôn đủ 4 (test 1); entry xoá mềm / `LoadDaysAsync` / S29 / B8 auto-clear Incomplete không ẩn entry (NdmSafetyRepro 65 PASS gồm xoá/clear/derived; `GetDayLog` đọc đúng 4/4 món); ngày local vs UTC: FE gửi `logDate` tường minh, BE chỉ dùng đồng hồ user khi thiếu (không thấy lỗi). **UNKNOWN:** chưa có log thật của lần user mất món; tái hiện là cơ chế khả dĩ nhất (đua ghi + lỗi bị nuốt phía FE, xem dưới), chưa phải bằng chứng của đúng sự cố đó.

**Phía FE (để MOS-FE xử lý, ngoài phạm vi BE):** `useLogMutation` (FE `use-food-log.ts`) chỉ `logger.error` khi lỗi, KHÔNG toast, trừ khi truyền `failureTitle`; `useAddEntry` không truyền ⇒ một request thua đua im lặng. Nên toast "Couldn't add …" cho mọi lỗi thêm món, và/hoặc log cả plate bằng `POST /food-logs/entries/batch` (một giao dịch, IdempotencyKey mới mỗi lần bấm).

## A. Sửa

Gốc, ở đường chung (không sửa từng handler):
* `Application/Behaviors/FoodLogWriteLockBehavior.cs` (đăng ký trong cùng của pipeline MediatR, sau Validation/Performance): 18 lệnh ghi Food Log (`AddFoodLogEntry`, `AddEntriesBatch`, `QuickAdd`, `CopyFoodLog`, `BulkCopy/Move/Delete`, `Update/DeleteFoodLogEntry`, `ClearDayLog`, `QuickEditDay`, `SaveNutritionData`, `DeleteNutritionData`, `RemoveManualOverride`, `SetDayFasting`, `SetDayIncomplete`, `AddWater`, `DeleteWater`) chạy trong transaction + khoá `sp_getapplock` theo user (`macroos:foodlog:{userId}`, an toàn nhiều instance). Request sau chờ request trước (tối đa 15 s, quá thì 409 `reasonCode CONCURRENT` thử lại được) rồi chạy trên dữ liệu mới nhất ⇒ không còn đua tạo dòng FoodLog / RowVersion / ghi đè tổng.
* `FoodLogSupport.SaveAndRebuildAsync` commit transaction (nhả khoá) NGAY sau khi lưu nguồn và TRƯỚC khi dựng dữ liệu dẫn xuất (`DerivedDataSync` tự mở transaction + khoá riêng); lệnh không đi qua đó (nước) được behavior commit sau handler; lỗi trước commit ⇒ rollback toàn bộ.
* `AddEntriesBatchCommandHandler`: replay chỉ hợp lệ khi lô gửi lại GIỐNG lô đã lưu (cùng món/recipe, kể cả món đã xoá mềm); khác ⇒ `409 IDEMPOTENCY_KEY_REUSED` (`errors.reasonCode`).

**Tests:** `tests/FoodLogLostEntriesRepro` 9 kiểm tra (`NOLOCK=1` bỏ behavior = trạng thái trước sửa: kịch bản 2, 3, 4, 6, 8 FAIL; kịch bản 7 fail trước khi thêm kiểm tra replay; có behavior: 9/9 PASS). AlgorithmChecks 856, DerivedDataRepro 44, NdmSafetyRepro 65 PASS sau thay đổi.

## B. Food Search

**Thực trạng quan trọng:** FE chỉ gọi `GET /foods` (danh sách phẳng, `apiRoutes.foods.root`) cho mọi chỗ tìm món; `GET /foods/search` (chia nhóm kiểu MacroFactor) chưa có trong `api-routes.ts`. Cả hai endpoint nay dùng chung một engine.

**Đo (p50 / p95 ms, 25 lần sau 3 lần làm nóng, LocalDB, danh mục 12 215 món = seed thật + 12k USDA tổng hợp + 8 món Việt có dấu, 901 alias, 126 entry log; mỗi lần gọi một DbContext mới; máy có nhiễu ±30%, nên bảng dưới là khoảng của 2 lần chạy xen kẽ cũ/mới trong cùng tiến trình):**

| `GET /foods/search` | NEW p50 / p95 | OLD p50 / p95 | JSON bytes (mới) |
|---|---|---|---|
| (rỗng, History) | 13–16 / 17–22 | 34–39 / 44–50 | 3 880 |
| `c` | 22–24 / 29–31 | 49–52 / 68–88 | 5 736 |
| `chicken` | 16–17 / 18–21 | 43–48 / 50–56 | 4 885 |
| `chicken breast` | 15–16 / 19–21 | 46 / 51–54 | 2 873 |
| `pho` / `phở` | 13–17 / 17–22 | 38–47 / 42–54 | 2 327 |
| `tuna` | 14–17 / 20–23 | 42–48 / 49–54 | 4 885 |
| `section=common&page=2` (`chicken`) | 11–13 / 16–19 | 37–56 / 43–91 | 8 635 |

`GET /foods` (bản SQL cũ, đo trước thay đổi, nhiễu hơn: `chicken` 56–225 ms p50 tuỳ lúc máy bận, `c` 75–160) → mới (RAM): `chicken` 11–18, `c` 12–22, rỗng (cả danh mục, trang đầu) 11–13 ms p50. Lần gọi đầu của tiến trình (nạp chỉ mục) ~370–500 ms, được làm nóng lúc khởi động (`ApplyMigrationsAsync`). Số lệnh SQL mỗi lần gõ của `GET /foods/search`: **7 → 4** (log gần nhất, món tự tạo, icon riêng + cài đặt; còn 3 khi cài đặt đã cache 60 s), không còn kéo hàng nghìn dòng; round trip tới DB từ xa giảm tương ứng (số ms thực trên MonsterASP **UNKNOWN**, chưa đo).

**Nguyên nhân chậm (trước):** (a) grouped: tải mọi món khớp (tới 9 265 dòng cho `c`) về app rồi xếp/phân trang trong RAM, (b) mỗi nhóm một lần quét bảng, (c) 7–8 round trip, (d) `LIKE '%x%'` không dùng được index nên mỗi lần gõ quét ~12k dòng (index `Name` có nhưng vô dụng với `%x%`), (e) so khớp phân biệt dấu (collation `SQL_Latin1_General_CP1_CI_AS`): "pho" không ra "Phở" trừ khi có alias.

**Cách mới (không migration):**
* `FoodCatalogIndex` (Application): ảnh chụp trong RAM của danh mục HỆ THỐNG (mọi món không phải của user/QuickAdd) + alias, tên đã gấp (chữ thường, bỏ dấu, đ→d); mỗi dòng mang luôn macro, khẩu phần mặc định, icon (không cần truy vấn chi tiết). ~12k dòng ≈ vài MB; nạp lười + làm nóng lúc khởi động, hết hạn 30 phút, `Invalidate()` sau import USDA và sau khi lưu món Open Food Facts từ mã vạch; trần 300 000 món (vượt thì cần chỉ mục ở DB). Nhiều instance: mỗi instance tự làm mới (trễ tối đa TTL).
* `FoodSearchContext` dùng chung cho `GET /foods` và `GET /foods/search`: khớp theo TỪNG từ (không phân biệt hoa/thường/**dấu**, từ MỘT ký tự chỉ khớp đầu từ), mức khớp **0 trùng tên/trước dấu phẩy/đại diện alias › 1 alias cùng canonical › 2 tên bắt đầu bằng từ khoá › 3 đầu một TỪ (mới) › 4 chứa**, rồi **món user từng log gần nhất lên trước (mới, mọi nhóm Common/Custom/Branded)**, đại diện, verified, catalog trước USDA, tên ngắn, tên. Món của user và lịch sử đọc từ DB theo user; `OrderBy(...).Take(skip+top)` (sắp một phần); cancellation token truyền tới mọi truy vấn (FE huỷ request cũ khi gõ tiếp).
* Cài đặt Show Branded / Show OFF đọc qua `FoodSearchPrefs` (cache 60 s theo user; `UpdateFoodLogSettings` ghi đè ngay).

**Kiểm (`tests/FoodSearchBench`, 17 PASS):** tổng/số dòng từng nhóm giống hệt bản cũ trên 10 từ khoá × (tổng quan, section) cho từ khoá ASCII (P1); `chicken` top 5 đều bắt đầu bằng "Chicken"; `pho`/`phở` ra "Phở bò tái", "Phở gà", "Beef pho"; `com` ra "Cơm tấm…", `bun` ra "Bún chả…"; món user hay log (tuna) nằm top 3; không từ khoá = History gần nhất trước; phân trang không trùng; `c` chỉ khớp đầu từ; Invalidate thấy món OFF mới ngay; `GET /foods`: tổng = Common của `/foods/search`, Mine/System/category đúng, không từ khoá = log gần nhất lên đầu + tổng cả danh mục.

## Contract diff (cho MOS-FE; JSON hình dạng KHÔNG đổi)
* `GET /foods`: cùng tham số/response. Hành vi đổi: khớp **không phân biệt dấu** ("pho" ↔ "Phở"); từ MỘT ký tự chỉ khớp đầu từ (TotalCount của `c` giảm: 9 267 → 7 317); thứ tự: món user đã log đứng trước món chưa log cùng mức khớp; `TotalCount` là tổng khớp (không đổi nghĩa).
* `GET /foods/search`: hình dạng giữ nguyên (`History/Common/Custom/Branded/OpenFoodFacts`, `Items`, `TotalCount`); FE có thể chuyển sang endpoint này cho UI chia nhóm (một request đã có chi tiết), dùng `section` + `page` cho "See more".
* Ghi Food Log: **409 `reasonCode: CONCURRENT`** (mới cho 18 lệnh trên, chỉ khi chờ khoá > 15 s: thử lại được); **409 `reasonCode: IDEMPOTENCY_KEY_REUSED`** cho `POST /food-logs/entries/batch` khi key đã dùng cho lô khác (FE phải tạo key MỚI mỗi lần bấm Log). Các lệnh song song nay xếp hàng thay vì lỗi: không còn mất món.
* Replay batch trả `entryIds` sắp theo thời điểm tạo, không đảm bảo theo thứ tự request (như trước, không đổi).

## Đề xuất (CHƯA làm, cần duyệt): khi danh mục > ~250k món
Thêm cột `FoodItems.NameFolded` (đã gấp, collation nhị phân) + index, hoặc full-text index, rồi tìm ở SQL; hiện chỉ mục RAM đủ cho ~12k–300k món. Migration (một cột + backfill, trên LocalDB trước) chỉ khi duyệt.
