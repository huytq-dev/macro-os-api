# Tìm món trên mạng (online food search) — đề xuất & những gì đã làm

Nguồn: user báo "Egg white, boiled" không tìm ra, app chưa hỗ trợ tìm trên mạng. Trạng thái: **phần không cần khoá/tài khoản/dependency mới ĐÃ làm**; phần gọi API ngoài **chờ user chọn** (mục 5).

## 1. Vì sao "Egg white, boiled" không ra

| Nguyên nhân | Chứng cứ | Xử lý |
|---|---|---|
| Danh mục seed không có lòng trắng/lòng đỏ | `CanonicalCatalogData.CountableFoods` chỉ có Egg whole raw/fried/scrambled/poached + hard-boiled (seed cũ) | **Đã thêm** `Egg white, raw` (đại diện), `Egg white, cooked, no added fat` (tên đúng USDA, KHÔNG gọi "boiled", không alias boiled), `Egg yolk, raw` (xem §7) |
| USDA không có bản ghi "hard-boiled egg white" | File FDC cục bộ (`BE/.usda-data`): SR Legacy 172183 *Egg, white, raw, fresh*, 172184 *Egg, yolk, raw, fresh*; FNDDS 2707170 *Egg, white, cooked, no added fat*, 2707168 *Egg, white only, raw* | "boiled" ≙ "cooked no added fat" (52 kcal/100 g, P10.7 C2.35 F0 Na281; 1 egg white = 33 g) |
| Tìm theo từ khoá nên "boiled" ≠ "cooked", "eggs" ≠ "egg", tiếng Việt không khớp tên Anh | `FoodSearchContext.Matches` (so chuỗi con theo từ) | `FoodSearchSynonyms` chỉ còn **số nhiều→số ít** ("eggs"→"egg"). Bản đầu có bảng VN→EN và boiled↔cooked đã **gỡ** (quyết định user 2026-10-26: không dịch bằng mapping thiếu kiểm chứng, tách raw/cooked/boiled; vd "bo" là cả "bò" lẫn "bơ" nên không thể map an toàn). Tiếng Việt chỉ qua alias catalog |
| Bulk importer chỉ nạp phần "common foods" | `UsdaCommonFoods.Plan` lọc; egg white nằm trong file nhưng ngoài lọc | Có thể nạp thêm bằng CLI `-- import-usda-bulk --dir BE/.usda-data` (cần user chạy; ghi DB) — chưa chạy |

Số liệu trên đọc từ file FDC cục bộ, chưa đối chiếu API → dòng seed mới ở trạng thái Unverified (đúng quy ước hiện có).

## 2. Đề xuất thiết kế: local-first → fallback server-side

```
GET /foods/search?q=...
  1. Tìm trong RAM (FoodCatalogIndex) — luôn chạy, không bao giờ chờ mạng.
  2. Nếu < N kết quả (vd 5) và q >= 3 ký tự và cờ ExternalFoodSearch bật:
       gọi USDA FDC /foods/search (server-side, timeout 1.5–2 s, 1 lần, không retry)
       → trả thêm nhóm "Từ USDA" (chưa lưu), mỗi dòng gắn source=USDA.
       Lỗi/timeout/429 → bỏ qua âm thầm (log), kết quả local vẫn trả bình thường.
  3. User chọn dòng ngoài → gọi lệnh import có sẵn (ImportUsdaFoodsCommand) lưu thành FoodItem
       (Source=USDA, ProviderCode/ExternalFoodId, QualityStatus nhãn nguồn) rồi mới log; từ đó tìm local.
```

- Cache: kết quả đã import chính là cache bền (lần sau ra từ local). Thêm `IMemoryCache` truy vấn→kết quả ngoài 24 h để giảm gọi lặp.
- Giới hạn: đếm gọi theo user (vd 20/phút) + ngân sách toàn cục theo giờ để không cháy hạn mức key.
- FE không gọi USDA trực tiếp (lộ key, CORS, không kiểm soát hạn mức).
- Không cần migration: bảng/cột nguồn đã có (`FoodSource`, `ProviderCode`, `ExternalFoodId`).

## 3. So sánh nguồn (tra cứu ngày 2026-10-26; giá/hạn mức có thể đổi — kiểm lại trang chính thức trước khi mua)

| Nguồn | Phù hợp | Hạn mức / chi phí | Ghi chú |
|---|---|---|---|
| **USDA FoodData Central** (khuyên dùng) | Món nguyên liệu/nấu chín, kiểu "Egg white, cooked" | Key miễn phí đăng ký api.data.gov: ~1.000 req/giờ/key. `DEMO_KEY`: 30 req/giờ, 50/ngày theo IP — **không dùng cho production** | Public domain (CC0). Code đã có `IUsdaClient` (mặc định DEMO_KEY, header `X-Api-Key`) |
| Open Food Facts | Sản phẩm đóng gói/mã vạch | Không key; search ~10 req/phút, bắt buộc User-Agent | ODbL (phải ghi nguồn, chia sẻ tương tự). Đang dùng cho mã vạch; **không** phù hợp "egg white" |
| FatSecret Platform | Nhà hàng/thương hiệu, nhiều món | Gói Basic miễn phí ~5.000 call/ngày, dữ liệu chủ yếu Mỹ, bắt buộc hiển thị attribution | Cần đăng ký tài khoản + OAuth2 — chờ user |
| Edamam / Nutritionix | Câu tự nhiên ("1 boiled egg white") | Trả phí / gói free hạn chế — **UNKNOWN** chưa xác minh giá hiện hành | Cần tài khoản + thẻ; chưa khuyến nghị |
| Bảng thành phần VN (VnFct) | Món Việt | Đã có trong catalog (`FoodSource.VnFct`) | Không cần gọi mạng |

Khuyến nghị: **USDA FDC với key riêng** làm fallback duy nhất giai đoạn 1 (miễn phí, không giấy phép phiền, khớp đúng loại món bị thiếu); OFF giữ cho mã vạch; VN dùng VnFct + bảng đồng nghĩa. Edamam/Nutritionix/FatSecret chỉ cân nhắc nếu user muốn món nhà hàng/thương hiệu.

## 4. Chi phí & rủi ro

- Tiền: 0 đồng với USDA (key miễn phí). Chỉ tốn thời gian triển khai (~1 ngày BE + 0,5 ngày FE hiển thị nhóm "Từ USDA").
- Độ trễ: thêm tối đa timeout (2 s) chỉ khi local ít kết quả; local không bị chặn.
- Chất lượng: tên Anh, một số mục FNDDS gộp; cần hiển thị nguồn "USDA" và chưa Verified cho tới khi user xác nhận.
- Hạn mức: 1.000/giờ/key đủ cho giai đoạn đầu; vượt thì cache bền + giảm tần suất (debounce phía FE ≥ 400 ms).
- Bảo mật: key nằm trong `appsettings.json` như các giá trị deploy khác (repo PRIVATE); không log key.

## 5. Cái gì cần user quyết (không làm tự ý)

1. **Có bật fallback USDA online không?** Cần user đăng ký key tại api.data.gov (miễn phí) và đưa vào cấu hình `Usda:ApiKey`. Trước khi có key vẫn dùng `DEMO_KEY` (chỉ để dev, 30 req/giờ).
2. Có muốn thêm nguồn trả phí/tài khoản (FatSecret/Edamam/Nutritionix) không — nếu có thì cần user mở tài khoản.
3. Có chạy bulk-import mở rộng `UsdaCommonFoods.Plan` (nạp thêm hàng nghìn món từ file cục bộ vào DB) không — thao tác ghi DB, không tự chạy.

## 6. Đã triển khai (không cần secret/tài khoản/dependency mới)

- `CanonicalCatalogData.Condiments.cs`: 3 món seed mới (Egg white raw [đại diện], Egg white cooked no added fat, Egg yolk raw); seeder idempotent theo `ExternalFoodId`, **không migration**. Dòng mới xuất hiện ở lần seed kế tiếp (khi API khởi động / deploy). Unverified, chưa lưu FDC ID.
- `FoodSearchSynonyms.cs` + `FoodSearchContext.Matches`: chỉ số nhiều→số ít.
- Test: `tests/AlgorithmChecks` (2 check E2: catalog + search).

## 7. Quyết định user 2026-10-26 (E2 APPROVED) và việc còn lại

User DUYỆT tích hợp USDA FDC làm fallback (Decisions_2026-10-26_E1_E2_Protein_Flux.md §2): canonical catalog trước, USDA sau; không tự dịch bằng mapping thiếu kiểm chứng; tách raw/cooked/boiled/fried và egg white/whole/yolk; lưu FDC ID + data source + data type + provenance; không ghi đè Food Item có sẵn; kết quả online KHÔNG coi là Verified nếu mapping chưa chắc; Food Log lịch sử không đổi khi USDA cập nhật (snapshot macro trong entry đã có); key chỉ ở server; cache, rate limit, timeout, fallback khi USDA lỗi. Ưu tiên không đổi schema; đổi DB hoặc luật tự import ⇒ báo user trước.

Nhãn cho dòng egg seed (đề xuất, đã áp dụng ở mức an toàn nhất): tên = đúng mô tả USDA ("cooked, no added fat"); "boiled egg white" KHÔNG là alias vì chưa có bản ghi USDA tương ứng. Tuỳ chọn cần user chọn: (A) cho phép alias "boiled egg white" → dòng cooked (mapping suy luận, ghi rõ nguồn FNDDS 2707170, Unverified), (B) chỉ hiện qua USDA online với nhãn nguồn, (C) bỏ hẳn hai dòng seed Egg white và chỉ dựa vào fallback USDA.
Ghi nhận provenance cần chỗ lưu FDC ID/data type: hiện `FoodItem.ProviderCode/ExternalFoodId` + `Source=USDA` đủ cho FDC ID và nguồn; "data type" (SR Legacy/FNDDS/Branded) có thể nối vào `ProviderCode`/ghi chú nguồn, nếu muốn cột riêng thì cần migration ⇒ hỏi user.

## 8. Bản trình bày để duyệt (round 2, 2026-10-26): API contract, cache, import policy

Nguyên tắc user (Decisions round 2 §5): local canonical catalog trước; USDA chỉ bổ sung; KHÔNG ánh xạ chung `boiled = cooked`; synonym riêng cho egg white chỉ sau khi xác minh dữ liệu; giữ FDC ID + data type + food state + provenance; đề xuất cột data type nhưng KHÔNG tự thêm migration; không auto-import không kiểm soát; API key chỉ ở cấu hình BE.

### 8.1 Xác minh bản chất dữ liệu egg white (đọc file FDC cục bộ `surveyDownload.json`, 2026-10-26)

| FDC ID | Mô tả | Data type | Nội dung đáng chú ý |
|---|---|---|---|
| 2707168 | Egg, white only, raw | Survey (FNDDS) | nguyên liệu "Eggs, Grade A, Large, egg white" 100 g; 52 kcal, P 10.7, C 2.36, F 0, Na 166 mg; khẩu phần "1 egg white" = 33 g |
| 2707170 | Egg, white, cooked, no added fat | Survey (FNDDS), food code 31108110 | nguyên liệu: egg white 100 g **+ Salt, table 0.3 g**; 52 kcal, P 10.7, C 2.35, F 0, Na **281** mg; `foodAttributes` ghi rõ các cách nấu được gộp: **boiled, poached, baked, NS as to cooking method**; "1 egg white" = 33 g |
| 2707169 / 2707171 | cooked, NS as to fat / fat added | Survey (FNDDS) | biến thể khác, không dùng cho "no added fat" |
| 172183 | Egg, white, raw, fresh | SR Legacy | 52 kcal, Na 166 (đã seed làm "Egg white, raw") |

Kết luận: FNDDS không có bản ghi "boiled egg white" riêng, nhưng bản ghi **cooked, no added fat tự liệt kê "boiled" là một trong các cách nấu nó đại diện** (cùng poached/baked), và công thức có thêm 0.3 g muối (nên Na 281 thay vì 166). Vì vậy một synonym RIÊNG cho egg white là có cơ sở dữ liệu: `boiled egg white`, `hard boiled egg white` → `Egg white, cooked, no added fat`, gắn chú thích "FNDDS 2707170: boiled/poached/baked, gồm 0.3 g muối". **Đề xuất, chưa triển khai (cần duyệt).** Không mở rộng sang thực phẩm khác, không thành quy tắc chung boiled = cooked.

### 8.2 API contract (đề xuất)

Giữ tìm local không bao giờ chờ mạng: FE gọi local trước (`GET /foods/search`, `GET /foods`), rồi gọi riêng endpoint ngoài khi kết quả local ít/không phù hợp.

```
GET  /foods/external/search?q=<text>&limit=10            (cần đăng nhập)
  200 { status: "OK" | "UNAVAILABLE" | "RATE_LIMITED" | "DISABLED",
        retryAfterSeconds: int?,
        items: [ { fdcId: int, description: string, dataType: "Foundation"|"SR Legacy"|"Survey (FNDDS)"|"Branded",
                   foodState: "raw"|"cooked"|"boiled"|"fried"|"baked"|"other"|null,   // gợi ý từ mô tả USDA, KHÔNG phải mapping; null khi không chắc
                   per100g: { kcal, proteinG, carbsG, fatG },                          // đơn vị chuẩn USDA, g / 100 g
                   portions: [ { name, grams } ],                                      // khi có (FNDDS/SR có foodPortions)
                   alreadyImported: bool, foodItemId: Guid?,                           // đã có trong catalog thì trả luôn id nội bộ
                   isVerified: false, provenance: { provider: "usda", fdcId, dataType, publishedAt?, retrievedAt } } ] }
  Luôn 200 với status khi USDA lỗi/timeout/429 (không 5xx): local không bị ảnh hưởng.
POST /foods/external/import     { fdcId: int }            (user chọn một dòng; một món mỗi lần)
  200 { foodItemId, created: bool }   // dùng lại ImportUsdaFoodsCommand với FdcIds=[id]; idempotent theo (usda, fdcId); KHÔNG ghi đè món có sẵn
```
Tìm theo tiếng Việt: gửi nguyên câu cho USDA, không tự dịch; chỉ alias catalog đã duyệt (mục 8.1) mới nối tiếng Việt/đồng nghĩa với món. Nguồn tìm của USDA phải gồm `Survey (FNDDS)` (client hiện chỉ gửi Foundation + SR Legacy), nếu không món kiểu "egg white, cooked" không tìm thấy.

### 8.3 Cache, rate limit, timeout, fallback

- Kết quả search: `IMemoryCache` 24 giờ, khoá = (q đã chuẩn hoá, dataTypes, limit); cache âm 60 giây cho lỗi/timeout; single-flight theo khoá để nhiều người gõ cùng một từ chỉ gọi USDA một lần.
- Chi tiết món (portions, nutrients): cache 7 ngày; món đã import là cache bền (lần sau ra từ catalog local).
- Giới hạn: theo user 10 lượt/phút + trần toàn cục 800 lượt/giờ (key miễn phí ~1.000/giờ); vượt thì `RATE_LIMITED` + `retryAfterSeconds`, không gọi USDA. FE debounce ≥ 400 ms và q ≥ 3 ký tự.
- Timeout: search 2 s, detail 4 s, không retry trong request; USDA lỗi ⇒ `UNAVAILABLE`, local vẫn đủ dùng.
- Key: chỉ `Usda:ApiKey` trong cấu hình BE (header `X-Api-Key` từ server), không gửi qua FE/Next.js, không ghi log, không đưa vào chat. Thiếu key ⇒ `DISABLED` (không dùng `DEMO_KEY` ở production).

### 8.4 Chính sách import

- Chỉ khi user chọn một dòng (hoặc CLI bulk có chủ ý); không tự import khi tìm.
- Idempotent theo (ProviderCode `usda`, ExternalFoodId = FDC ID); không ghi đè/refresh món có sẵn; món cùng tên khác FDC ID là món khác.
- Dòng import: `Source=USDA`, `QualityStatus=Unverified`, `IsVerified=false` trừ khi mapping chắc chắn (FDC ID nằm trong danh sách đã đối chiếu); gom vào canonical bằng `CanonicalLinker` với `MatchMethod` ghi rõ và KHÔNG làm đại diện.
- Lịch sử log bất biến: mỗi `FoodLogEntry` lưu snapshot macro + `ServingSnapshotJson` lúc log; dữ liệu USDA đổi sau này không đổi entry cũ. Món import không tự cập nhật theo USDA; refresh = hành động có phê duyệt, tạo phiên bản mới.
- Trạng thái chế biến (raw/cooked/boiled/fried) lấy từ mô tả USDA làm gợi ý, tách thành các dòng riêng; egg white / whole egg / yolk là các canonical khác nhau.

### 8.5 Cột data type (đề xuất, KHÔNG thêm migration)

Hiện FDC ID lưu ở `FoodItem.ExternalFoodId` + `ProviderCode = usda` (dùng trong nhiều truy vấn, không nên đổi nghĩa). `UsdaFood.DataType` được parse nhưng **chưa lưu** vào đâu. Đề xuất cho user duyệt: thêm cột nullable `FoodItem.SourceDataType nvarchar(32)` (Foundation / SR Legacy / Survey (FNDDS) / Branded) và (tuỳ chọn) `SourcePublishedAt`; additive, backfill được từ file FDC cục bộ. Trước khi được duyệt: data type chỉ trả trong response ngoài (8.2) và suy lại được từ FDC ID; không ghi vào cột nào cả.

### 8.6 Cần user duyệt
(1) Contract 8.2 (2 endpoint) và việc thêm `Survey (FNDDS)` vào tìm kiếm online. (2) Cache/giới hạn 8.3. (3) Chính sách import 8.4. (4) Cột `SourceDataType` (migration riêng). (5) Alias riêng `boiled egg white` → FDC 2707170 (8.1). (6) Có đưa key `Usda:ApiKey` thật vào cấu hình deploy (user tự đặt, không gửi qua chat).
