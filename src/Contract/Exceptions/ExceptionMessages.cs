namespace Contract;

public static class ExceptionMessages
{
    // Không có tham số
    public const string Forbidden = "You do not have permission to access this resource";
    public const string DatabaseOperationFailed = "Database operation failed";
    public const string InternalError = "Unexpected error occurred";

    // --- NotFound ---
    public static string NotFound(string entityName, object id)
        => $"{entityName} with ID {id} was not found";

    public static string NotFoundField(string entityName, string fieldName, object value)
        => $"{entityName} with {fieldName} '{value}' was not found";

    public static string NotExists(string entityName, object value)
        => $"{entityName} with value '{value}' does not exist";

    // --- Conflict / Validation Logic ---
    public static string AlreadyExists(string entityName, object value)
        => $"{entityName} with value '{value}' already exists";

    public static string Invalid(string entityName)
        => $"{entityName} is invalid";

    // --- MacroOS: program / goal / check-in (tiếng Việt, hiển thị cho người dùng) ---
    public const string NoActiveProgram = "Chưa có program đang hoạt động.";
    public const string DayLoggedInFoodLog = "This day is logged in Food Log; edit foods there.";
    public const string DayLoggedInFoodLogDelete = "This day is logged in Food Log; delete foods there.";
    public const string NoActiveGoal = "Chưa có goal đang hoạt động.";
    public const string ManualProgramNoCheckIn = "Program Manual không có check-in.";
    public const string ProgramHasNoWeekTarget = "Program chưa có target tuần nào.";
    public const string NoExpenditureEstimate = "Chưa có ước lượng expenditure.";
    public const string WeightRequired = "Cần ít nhất một lần cân nặng.";
    public const string WeightRequiredForProgram = "Cần ít nhất một lần cân nặng trước khi tạo program.";
    public const string ProfileRequiredForProgram = "Cần ngày sinh và chiều cao trong hồ sơ trước khi tạo program.";
    public const string ManualProgramNeedsSevenDays = "Program Manual cần target 7 ngày.";
    public const string CheckInDecisionConflict = "Hôm nay đã check-in với quyết định khác.";
    public const string CheckInAlreadyDone = "Hôm nay đã check-in.";
    public const string IncompleteDaysOutOfRange = "Chỉ đánh dấu được trong 7 ngày gần nhất.";
    public const string GoalTargetTooFar = "Cân nặng mục tiêu quá xa cân nặng hiện tại.";
    public const string GoalRateRequired = "Tốc độ phải lớn hơn 0 khi đích khác cân nặng hiện tại.";

    public static string DateInFuture(string field, DateOnly today) => $"{field} cannot be after today ({today:yyyy-MM-dd}, your local date).";

    public static string NoteLimitReached(int max) => $"Up to {max} notes per day.";

    public static string CheckInNotDue(DateOnly nextDate)
        => $"Chưa tới ngày check-in ({nextDate:yyyy-MM-dd}).";

    public static string GoalRateTooHigh(decimal maxPercent)
        => $"Tốc độ tối đa {maxPercent}% cân nặng mỗi tuần.";

    public static string DayKcalMax(DayOfWeek day, int max)
        => $"{day}: kcal tối đa {max}.";

    public static string DayProteinMax(DayOfWeek day, decimal maxPerKg)
        => $"{day}: protein tối đa {maxPerKg} g/kg cân nặng.";

    // --- MacroOS: ảnh / AI ---
    public const string ImageTypeNotSupported = "Chỉ nhận ảnh JPEG, PNG hoặc WebP.";
    public const string PhotoConsentRequired = "Cần đồng ý lưu ảnh tiến độ riêng tư trước khi tải ảnh đầu tiên.";
    public const string ImageSaveFailed = "Không lưu được ảnh lúc này, thử lại sau.";
    public const string ImageLoadFailed = "Không tải được ảnh lúc này, thử lại sau.";
    public const string ImageDeleteFailed = "Không xoá được ảnh lúc này, thử lại sau.";
    public const string AiNotConfigured = "AI chưa được cấu hình";
    public const string AiBusy = "AI đang bận hoặc không phân tích được lúc này, thử lại sau.";
    public const string AiInvalidResult = "AI không trả kết quả hợp lệ, thử lại sau.";
    public const string AiTimeout = "AI đang bận hoặc quá thời gian, thử lại sau.";
    public const string AiScanNotConfirmable = "Phân tích này không còn dùng được để xác nhận.";
    public const string AiScanExpired = "Phân tích đã hết hạn, hãy phân tích lại.";
}
