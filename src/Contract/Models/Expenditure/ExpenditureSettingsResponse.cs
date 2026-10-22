namespace Contract;

/// <summary>
/// Cài đặt Expenditure của user (đọc/ghi qua /api/expenditure/settings). Giá trị null = chưa cấu hình/chưa trả lời, KHÔNG có giá trị mặc định thay thế.
/// Chỉ những trường này ảnh hưởng ước tính ban đầu; đổi chúng không bao giờ reset hay đổi expenditure đã học.
/// </summary>
/// <param name="InitialEstimateMode">Formula | Manual | null (chưa cấu hình ⇒ hành vi cũ: lấy từ program mới nhất).</param>
/// <param name="ManualInitialTdeeKcal">TDEE nhập tay (chỉ khi Manual); khi chưa cấu hình có thể là giá trị cũ của program, xem <paramref name="ManualInitialSource"/>.</param>
/// <param name="ManualInitialSource">Settings | LegacyProgram | null — nguồn của <paramref name="ManualInitialTdeeKcal"/> (chỉ đọc).</param>
/// <param name="BmrEquation">MacroFactor | Cunningham.</param>
/// <param name="BmrSex">Male | Female | null: tham số sinh học user tự xác nhận, chỉ dùng cho Equation 1; không suy ra từ Gender.</param>
/// <param name="AthleticPursuits">ExerciseRegularly | Athlete | ProfessionalAthlete | NoneOfTheAbove | null (chưa trả lời, không có fallback). Chỉ lưu đáp án, không tự bật Equation 3.</param>
/// <param name="WeighedMoreThanBefore">Yes | No | NotSure | null (Prior Weight, đáp án định tính).</param>
/// <param name="WeighedMoreThanBeforeAtWeightKg">Cân nặng LÚC user trả lời Prior Weight (ngưỡng đã hỏi = giá trị này / 0.9); null với đáp án cũ chưa có ngữ cảnh.</param>
/// <param name="CalculationStartDate">Ngày bắt đầu tính expenditure hiện hiệu lực.</param>
/// <param name="CalculationStartDateSource">Settings | LegacyProgram | null.</param>
/// <param name="CalculationRevision">Tăng mỗi lần Start Date đổi và được dựng lại.</param>
/// <param name="Mode">Dynamic | Static (hôm nay, theo ngày local của user).</param>
/// <param name="StaticTdeeKcal">Giá trị Static đang hiệu lực (null khi Dynamic).</param>
/// <param name="WeighedMoreThanBeforeThresholdKg">Ngưỡng CHÍNH XÁC (kg) của câu hỏi Prior Weight đã LƯU lúc trả lời; không tính lại khi cân/mục tiêu/công thức đổi; null cho đáp án cũ chưa có ngữ cảnh.</param>
/// <param name="WeighedMoreThanBeforeThresholdShown">Số user ĐÃ THẤY (đã làm tròn để hiển thị) theo đơn vị <paramref name="WeighedMoreThanBeforeThresholdUnit"/>.</param>
/// <param name="WeighedMoreThanBeforeThresholdUnit">kg | lb.</param>
/// <param name="MetabolicAdaptationEligibility">NotApplicable | Unknown | Established: điều kiện ">10% dưới đỉnh" có được chứng minh từ đáp án hay không (quy tắc bảo thủ MacroOS, chỉ hiển thị; không đổi expenditure đã học).</param>
/// <param name="DefaultCalculationStartDate">Ngày bắt đầu mặc định ĐÃ LƯU (ngày local expenditure tracking thực sự khởi tạo; ổn định, không đổi theo program/check-in/mục tiêu). null = chưa có nguồn gốc đáng tin (dữ liệu cũ): Reset to Default không khả dụng, không bịa ngày.</param>
/// <param name="AlgorithmName">Luôn là "MacroOS Adaptive Expenditure" (không phải tên/phiên bản MacroFactor).</param>
public sealed record ExpenditureSettingsResponse(
    string? InitialEstimateMode,
    int? ManualInitialTdeeKcal,
    string? ManualInitialSource,
    string BmrEquation,
    string? BmrSex,
    string? AthleticPursuits,
    string? WeighedMoreThanBefore,
    decimal? WeighedMoreThanBeforeAtWeightKg,
    DateOnly? CalculationStartDate,
    string? CalculationStartDateSource,
    int CalculationRevision,
    string Mode,
    int? StaticTdeeKcal,
    decimal? WeighedMoreThanBeforeThresholdKg,
    decimal? WeighedMoreThanBeforeThresholdShown,
    string? WeighedMoreThanBeforeThresholdUnit,
    string MetabolicAdaptationEligibility,
    DateOnly? DefaultCalculationStartDate,
    string AlgorithmName = ExpenditureLabels.AlgorithmName
);
