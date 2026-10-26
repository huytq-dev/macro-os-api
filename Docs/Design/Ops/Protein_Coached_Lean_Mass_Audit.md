# Protein target: audit hiện tại + đề xuất chuyển Coached sang lean mass (MF Article 222)

Yêu cầu user 2026-10-26 (Decisions_2026-10-26_E1_E2_Protein_Flux.md §3): Coached dùng Lean Mass theo MF 222; không tạo body fat % giả; Article 142 không phải công thức Coached; Manual/Collaborative giữ quyền user; không hardcode hệ số g/kg lean mass MF chưa công bố; kiểm tra sau Check-in / đổi goal / đổi cân; không phá Macro Budget Feasibility; không đổi target đã chấp nhận trong lịch sử. **Tài liệu này chỉ AUDIT + ĐỀ XUẤT, chưa đổi business rule.**

## 1. Công thức hiện tại (code đọc ngày 2026-10-26)

| Chỗ | Hành vi | Nguồn của số |
|---|---|---|
| `MacroTargetCalculator.ProteinGPerKg(level, training)` | g/kg **TỔNG cân nặng**: Low 1.6 / Moderate 1.8 / High 2.0 / ExtraHigh 2.4; tập tạ (Lifting/Both) +0.2 | Hằng số MacroOS, **không có nguồn MF** (MF 222: g/kg từng mức = UNKNOWN) |
| `SplitMacros(..., fixedProteinG, ...)` | protein = `FixedProteinGrams` nếu có, nếu không `round(gPerKg × weightKg, 1)`; rồi fat sàn max(0.5 g/kg, 20% kcal), carb phần còn lại | MacroOS |
| `ProgramDraftCalculator` | lưu `ProteinLevel`, `ProteinGPerKgBodyweight`; từ chối khi protein ngày > `MaxProteinGPerKg` (4) × cân | MacroOS |
| `CheckInCalculator.Propose` | protein **giữ nguyên** tuần trước trừ khi cân (suy từ protein tuần trước ÷ g/kg) lệch > 2 kg so với trend hiện tại thì tính lại `gPerKg × trend`; `FixedProteinGrams` thắng tất cả | ADR-005 (quy tắc MacroOS, không phải MF) |
| `UpdateProgramGoal` | đổi `ProteinLevel` ⇒ tính lại `ProteinGPerKgBodyweight` bằng cùng hàm | MacroOS |
| Preview onboarding | cùng hàm (`ProgramPreviewCalculator`) | MacroOS |

⇒ Hiện tại protein theo **body mass**, không có lean mass, và Coached/Collaborative/Manual dùng chung một hàm; chỉ `FixedProteinGrams` phân biệt "user tự đặt".

## 2. MF nói gì (đã verify trong MF_Formulas_Research_2026-10-26.md, mục 3)

- Article 222: protein Coached theo lean mass ước lượng (cân nặng + nhóm thể trạng/body composition category), protein preference và exercise type. VERIFIED.
- Article 142: protein tổng quát theo body mass (1.2–2.2 g/kg, RDA 0.8) — dành cho Collaborative/Manual, KHÔNG phải công thức Coached.
- **UNKNOWN (MF không công bố)**: g/kg lean mass của từng mức Low/Moderate/High, cách quy nhóm thể trạng ra lean mass, và hệ số cộng cho exercise type. ⇒ không được tự hardcode "g/kg lean mass".

## 3. Dữ liệu sẵn có để ước lượng lean mass (không tạo số giả)

- `UserProfile.BodyFat` (BodyFatBand, user tự chọn lúc onboarding) và `BodyMetric.BodyFatPercent` (số user nhập/đo thật). `OnboardingCalculator.BodyFatPercent(band)` đã có điểm giữa dải (11.5 / 15.5 / 20.5 / 26 / 31 / 35.5 / 40 / 46) và đang dùng cho nhánh BMR FFM (theo MF BMR blog). Lean mass = cân × (1 − BF%/100) dùng **số đo thật nếu có, nếu không điểm giữa dải user đã chọn**, không bịa phần trăm.
- Thiếu cả hai (user bỏ qua body composition) ⇒ **không ước lượng được lean mass**: phải rơi về body mass (nhãn rõ "chưa có body composition") hoặc yêu cầu user chọn nhóm, không đoán.

## 4. Đề xuất (CHƯA làm, cần user duyệt từng mục)

1. **Tách theo ProgramType.** Coached: protein = lean mass ước lượng × hệ số g/kg-lean của (ProteinLevel, TrainingType). Collaborative/Manual: giữ nguyên hành vi user đặt (g/kg body mass hoặc `FixedProteinGrams`), không đổi.
2. **Hệ số g/kg-lean**: MF không công bố ⇒ hai lựa chọn: (a) chờ nguồn MF; (b) user duyệt một bảng hệ số **gắn nhãn "MacroOS approximation"**, nêu nguồn khoa học công khai làm cơ sở. Đề xuất: (b) chỉ khi user duyệt bảng cụ thể; mặc định không tự đặt.
3. **Lean mass tại thời điểm tính**: dùng `BodyMetric.BodyFatPercent` mới nhất nếu có, không thì điểm giữa `BodyFatBand`; thiếu ⇒ giữ công thức body mass hiện tại và trả `reasonCode` "PROTEIN_LEAN_MASS_UNAVAILABLE" (cộng thêm, tương thích ngược).
4. **Check-in**: thay quy tắc ADR-005 "cân lệch > 2 kg" bằng "lean mass ước lượng lệch > X" chỉ khi user duyệt X; trước đó giữ nguyên protein tuần trước (không đổi target đã chấp nhận). Đổi goal/ProteinLevel: tính lại theo công thức mới cho **tuần chưa chấp nhận**, không đụng `MacroProgramWeek` đã Accept.
5. **Lưu trữ**: `ProteinGPerKgBodyweight` hiện nghĩa "g/kg cân". Dùng lại cột cho g/kg-lean sẽ đổi nghĩa ⇒ cần cột/tag mới (migration) hoặc suy từ `ProgramType` khi đọc. Đề xuất không đụng schema ở bước này; báo user trước khi cần migration.
6. **Feasibility**: giữ `MaxProteinGPerKg` (tính theo cân) và kiểm fat sàn + kcal ≥ 4×protein + 9×fatMin trước khi chấp nhận; thêm test: Coached với lean mass nhỏ không bao giờ vượt protein ngày cũ khi cân/ProteinLevel không đổi (không "nhảy" target).

## 5. Kiểm thử đề xuất khi được duyệt (pure trước, DB test riêng sau)

- Cùng cân + khác BodyFatBand ⇒ protein Coached khác; Collaborative/Manual không đổi.
- Sau Check-in / đổi goal / đổi cân: protein tuần đã Accept bất biến; tuần mới theo quy tắc đã duyệt.
- Không có body composition ⇒ fallback body mass + reasonCode, không NaN/0.
- Feasibility: mọi ngày kcal ≥ 4P + 9·fatMin.

## 5b. Audit round 2 (2026-10-26): body composition, protein preference, exercise multiplier — MF verified vs MacroOS assumption

**Hệ số protein hiện tại tính theo TỔNG CÂN NẶNG (total body weight), KHÔNG theo lean mass** — `MacroTargetCalculator.ProteinGPerKg(level, training)` × `weightKg` (cân hiện tại hoặc trend), không đọc `BodyFat`/lean mass ở bất kỳ nhánh protein nào. Lean mass chỉ xuất hiện ở nhánh TDEE khởi điểm (Cunningham, `OnboardingCalculator`/`InitialExpenditureCalculator`), không ở protein.

| Thành phần | Trong code | Nguồn / trạng thái |
|---|---|---|
| **Bảng body composition** | `BodyFatBand` 9 mức nhìn bằng mắt (`Bf10To13` … `Bf50Plus`) lưu ở `UserProfile.BodyFat`; điểm giữa dùng khi cần số: 11.5 / 15.5 / 20.5 / 26 / 31 / 35.5 / 40 / 46 / 52 (`OnboardingCalculator.BodyFatPercent`) | Tên 9 mức: theo wizard MF của ta (ảnh/tài liệu onboarding). **Điểm giữa dải = lựa chọn của MacroOS** (MF 222 nói "body composition category" nhưng KHÔNG công bố con số % mỡ hay lean mass cho từng nhóm ⇒ UNKNOWN). Mức cuối `50+` lấy 52 là giả định của MacroOS. Hiện chỉ dùng cho BMR/Cunningham: `lean = cân × (1 − %mỡ)` (công thức Cunningham 500 + 22×lean khớp MF BMR blog) |
| **Protein preference** | `ProteinLevel` Low / Moderate / High / ExtraHigh → g/kg tổng cân 1.6 / 1.8 / 2.0 / 2.4 | MF 222/34 (VERIFIED): Coached có low / moderate / high = đáy / giữa / đỉnh khoảng tối ưu; **g/kg của từng mức UNKNOWN**. `ExtraHigh` và 4 con số 1.6–2.4 là giả định MacroOS. MF 142 (1.2–2.2 g/kg body mass, RDA 0.8) là khuyến nghị CHUNG, không phải công thức Coached |
| **Exercise multiplier** | `TrainingType` None / Lifting / Cardio / Both (suy từ mức kinh nghiệm tập tạ/cardio); Lifting hoặc Both ⇒ **+0.2 g/kg**, Cardio và None ⇒ +0 | MF 222 (VERIFIED): protein Coached phụ thuộc exercise type; **hệ số cụ thể UNKNOWN**. "+0.2 cho tập tạ" và "cardio = 0" là giả định MacroOS |
| **Fixed protein (Manual/Collaborative)** | `FixedProteinGrams` thắng mọi công thức (`SplitMacros`, `CheckInCalculator`) | user giữ quyền (MF 142 cho Collaborative/Manual). Giữ nguyên |
| **Rule đổi cân** | check-in giữ protein tuần trước trừ khi cân lệch > 2 kg so với cân suy ra (ADR-005); `UpdateProgramGoal` đổi level ⇒ tính lại | quy tắc MacroOS, không có nguồn MF |

### Fallback khi thiếu body composition (đề xuất, cần duyệt)
1. Có số đo thật gần nhất (`BodyMetric.BodyFatPercent`) ⇒ dùng số đó làm nguồn lean mass (không bịa).
2. Không có số đo nhưng có `BodyFat` band ⇒ điểm giữa dải (đúng như BMR đang làm), gắn cờ "ước lượng từ nhóm thể trạng".
3. Thiếu cả hai ⇒ KHÔNG ước lượng lean mass: giữ công thức body mass hiện tại (cùng kết quả như hôm nay) + `reasonCode` cộng thêm báo "thiếu body composition", và FE mời user chọn nhóm thể trạng. Không đoán %mỡ, không dừng chương trình.

Không đổi công thức, không sửa target lịch sử trong bước này.

## 6. Cần user quyết

(1) Duyệt tách Coached/Manual-Collaborative như §4.1. (2) Hệ số g/kg-lean: chờ MF hay duyệt bảng MacroOS approximation (nêu số cụ thể). (3) Khi thiếu body composition: fallback body mass hay bắt chọn nhóm. (4) Có chấp nhận migration (cột phân biệt nghĩa của g/kg) hay suy theo ProgramType.
