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
    // Manual Nutrition (S29) và ngày nhịn ăn: lỗi rõ ràng, không đổi gì
    public const string FastingWithManualNutrition = "This day is marked as fasting, so it can't also have manual nutrition above zero. Remove the fasting mark or lower the values. Nothing was changed.";
    public const string NutritionDataNothingToSave = "There is nothing to save. To clear manual values use Remove Manual Override.";
    public const string NutritionDataUseRemoveOverride = "Clearing every field does not remove the manual values. Use Remove Manual Override to do that. Nothing was changed.";
    public const string FastingNeedsEmptyDay = "This day has logged entries. Remove them (or the manual entry) before marking it as a fasting day. Nothing was changed.";
    public const string IncompleteNeedsLoggedDay = "Only a day with logged data can be marked as incomplete.";
    public const string NoActiveGoal = "Chưa có goal đang hoạt động.";
    public const string ManualProgramNoCheckIn = "Program Manual không có check-in.";
    public const string ProgramHasNoWeekTarget = "Program chưa có target tuần nào.";
    public const string NoExpenditureEstimate = "Chưa có ước lượng expenditure.";
public static string InitialEstimateNeedsAnswers(string missing) => $"Chưa đủ câu trả lời để ước tính expenditure ban đầu: {missing}. Hoàn tất các câu hỏi hoặc nhập TDEE tay.";
public const string ConcurrentWriteConflict = "Dữ liệu của ngày này vừa được cập nhật ở nơi khác, vui lòng thử lại.";
    /// <summary>409 khi một IdempotencyKey đã dùng cho một lô món KHÁC (key phải mới cho mỗi lần log); trước đây lô thứ hai bị coi là replay, trả 200 và không lưu món nào.</summary>
    public const string IdempotencyKeyReused = "Khoá chống trùng này đã được dùng cho một lần log khác: các món lần này chưa được lưu. Hãy tạo khoá mới cho mỗi lần log.";
public const string ExpenditureSettingsBusy ="Cài đặt expenditure đang được cập nhật ở nơi khác, vui lòng thử lại.";
public const string ManualTdeeNeedsManualMode = "Chỉ nhập TDEE tay khi chọn Manual Initial Estimate.";
public const string ManualTdeeRequired = "Manual Initial Estimate cần một giá trị TDEE.";
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

    public const string CheckInDerivedDataNotReady = "Expenditure đang được tính lại từ dữ liệu mới nhất, chưa thể chấp nhận check-in. Vui lòng thử lại khi hoàn tất.";

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

    // --- Chuyển từ chuỗi viết cứng ở handler/validator (CLAUDE.md: message không hardcode) ---
    public const string MacroNutrientNotPinnable = "Calories/Protein/Carbs/Fat đã có tile riêng, không pin ở đây.";
    public const string InvalidTokenAlgorithm = "Invalid token security algorithm.";
    public const string OnlyDeleteOwnFood = "Chỉ xoá được thực phẩm do bạn tạo.";
    public const string NutritionDataNeedsEnergy = "Energy is required to save a new day (or mark it as fasting).";
    public const string OnboardingNeedsBirthdayAndHeight = "Cần ngày sinh và chiều cao trước khi ước tính.";
    public const string OnboardingNeedsWeight = "Cần có cân nặng hiện tại trước khi ước tính.";
    public const string MacroNutrientTargetViaProgram = "Calories/Protein/Carbs/Fat đặt qua program, không đặt ở đây.";
    public const string EmailVerificationLinkInvalid = "Link xác thực không hợp lệ hoặc đã hết hạn. Hãy gửi lại email xác thực.";
    public const string FileEmpty = "File cannot be empty";
    public const string OnboardingNeedsProgram = "Cần tạo program trước khi hoàn tất thiết lập.";
    public const string EmailNotVerified = "Tài khoản chưa xác thực email. Mở email xác thực hoặc gửi lại email mới.";
    public const string OnlyEditOwnFood = "Chỉ sửa được thực phẩm do bạn tạo.";
    public const string AccessTokenRequired = "Access Token is required";
    public const string InvalidCredentials = "Email hoặc mật khẩu không đúng.";
    public static string FoodHasNoServingAsIngredient(string name)
        => $"Món '{name}' chưa có khẩu phần nên không dùng được làm nguyên liệu.";
    public static string InvalidNutrientCode(string code)
        => $"Mã chất không hợp lệ: {code}.";
}
