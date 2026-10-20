namespace Contract;

public static class BusinessMessages
{
    // Success Messages
    public static string CreatedSuccessfully(string entityName) => $"Created {entityName} successfully";
    public static string UpdatedSuccessfully(string entityName) => $"Updated {entityName} successfully";
    public static string DeletedSuccessfully(string entityName) => $"Deleted {entityName} successfully";
    public static string FoundSuccessfully(string entityName) => $"Found {entityName} successfully";

    // Failure Messages
    public static string CreateFailure(string entityName) => $"Failed to create {entityName}";
    public static string UpdateFailure(string entityName) => $"Failed to update {entityName}";
    public static string DeleteFailure(string entityName) => $"Failed to delete {entityName}";
    public static string GetFailure(string entityName) => $"Failed to get {entityName}";
}

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

public static class ValidationMessages
{
    // Basic
    public const string Required = "{PropertyName} is required";
    public const string InvalidFormat = "{PropertyName} has an invalid format";
    public const string InvalidEnumValue = "{PropertyName} has an invalid enum value";
    public const string InvalidIconKey = "{PropertyName} is not a valid food icon key";

    // Format specific
    public const string InvalidEmail = "{PropertyName} is not a valid email format";
    public const string InvalidPhoneNumber = "{PropertyName} is not a valid phone number";
    public const string InvalidDate = "{PropertyName} is not a valid date";
    public const string InvalidPlateNumberFormat = "{PropertyName} can only contain letters, numbers, and hyphens";
    public const string Expired = "{PropertyName} has expired";

    // Length & Range
    public const string MinLength = "{PropertyName} must be at least {MinLength} characters";
    public const string MaxLength = "{PropertyName} must not exceed {MaxLength} characters";
    public const string ExactLength = "{PropertyName} must be exactly {ExactLength} characters";
    public const string Range = "{PropertyName} must be between {MinLength} and {MaxLength} characters";

    // Comparison
    public const string GreaterThan = "{PropertyName} must be greater than {ComparisonValue}";
    public const string GreaterThanOrEqual = "{PropertyName} must be greater than or equal to {ComparisonValue}";
    public const string LessThan = "{PropertyName} must be less than {ComparisonValue}";
    public const string LessThanOrEqual = "{PropertyName} must be less than or equal to {ComparisonValue}";

    // Content restriction
    public const string OnlyLetters = "{PropertyName} can only contain letters";
    public const string OnlyNumbers = "{PropertyName} can only contain numbers";
    public const string OnlyAlphanumeric = "{PropertyName} can only contain letters and numbers";
    public const string NotContainSpaces = "{PropertyName} must not contain spaces";
    public const string ProhibitedContent = "Your comment contains prohibited words";

    // List/Collection
    public const string ListNotEmpty = "{PropertyName} must not be empty";
    public const string ListMinItems = "{PropertyName} must contain at least {MinItems} items";
    public const string ListMaxItems = "{PropertyName} must contain no more than {MaxItems} items";

    // Boolean
    public const string MustBeTrue = "{PropertyName} must be true";
    public const string MustBeFalse = "{PropertyName} must be false";

    // Matching
    public const string MustMatch = "{PropertyName} must match {ComparisonProperty}";
    public const string MustNotMatch = "{PropertyName} must not be the same as {ComparisonProperty}";

    // MacroOS
    public const string NotInFuture = "{PropertyName} không được ở tương lai.";
    public const string MustBeZeroForMaintenance = "{PropertyName} phải bằng 0 khi mục tiêu là Maintenance";
    public const string RefeedDaysRange = "Chọn từ 1 đến 6 ngày khác nhau để ăn thêm.";
    public const string ManualProgramNeedsSevenDayTargets = "Program Manual cần target đủ 7 ngày.";
    public const string CoachedProgramNoDayTargets = "Program Coached không có target theo ngày do người dùng đặt.";
    public const string DayTargetsNeedSevenDays = "Target theo ngày phải đủ 7 ngày.";
    public const string DayTargetsDistinctWeekdays = "Mỗi thứ trong tuần chỉ được đặt một lần.";
    public const string InvalidPhotoAngle = "Góc ảnh phải là Front, Side hoặc Back.";
    public const string InvalidShortcutKey = "Khoá shortcut không hợp lệ.";
    public const string ShortcutDuplicated = "Mỗi shortcut chỉ xuất hiện một lần.";
    public const string InvalidShortcutColor = "Màu shortcut không hợp lệ.";
    public const string InvalidNoteColor = "Color must be Gray, Red, Blue, Green, Yellow, Purple or Orange.";
    public const string AiMealNeedsInput = "Provide at least one photo or a description";
    public const string AiMealPhotoType = "Photos must be JPEG, PNG or WebP";
    public const string AiConfirmMaxIngredients = "Tối đa 50 nguyên liệu.";
    public const string AiConfirmNeedsFood = "Cần FoodItemId hoặc CanonicalFoodId.";

    public static string TapeMeasurementRange(string name)
        => $"{name} phải trong khoảng 5–300 cm.";

    public static string PhotoMaxMb(int mb)
        => $"Ảnh tối đa {mb} MB.";

    public static string ShortcutMinEnabled(int count)
        => $"Cần bật ít nhất {count} shortcut cho kiểu toolbar này.";

    public static string AiMealMaxPhotos(int count)
        => $"At most {count} photos";

    public static string AiMealPhotoMaxMb(int mb)
        => $"Each photo must be at most {mb} MB";
}
