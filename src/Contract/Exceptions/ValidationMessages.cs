namespace Contract;

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
    public const string UnknownMeasurementKey = "Unknown measurement key.";
    public const string MeasurementSetAndClear = "A metric cannot be set and cleared in the same request.";
    public const string NothingToChange = "Nothing to change: send at least one value or one key to clear.";
    public const string InvalidRecipeLink = "Link must be a valid http(s) URL.";
    public const string RecipeStepsMax = "Up to 50 steps.";
    public const string InvalidNoteColor = "Color must be Gray, Red, Blue, Green, Yellow, Purple or Orange.";
    public const string AiMealNeedsInput = "Provide at least one photo or a description";
    public const string AiMealPhotoType = "Photos must be JPEG, PNG or WebP";
    public const string AiConfirmMaxIngredients = "Tối đa 50 nguyên liệu.";
    public const string AiConfirmNeedsFood = "Cần FoodItemId hoặc CanonicalFoodId.";

    public static string TapeMeasurementRange(string name)
        => $"{name} phải trong khoảng 5–300 cm.";

    public static string TapeMeasurementRange(string name, decimal min, decimal max)
        => $"{name} must be between {min:0.##} and {max:0.##} cm.";

    public static string PhotoMaxMb(int mb)
        => $"Ảnh tối đa {mb} MB.";

    public static string ShortcutMinEnabled(int count)
        => $"Cần bật ít nhất {count} shortcut cho kiểu toolbar này.";

    public static string AiMealMaxPhotos(int count)
        => $"At most {count} photos";

    public static string AiMealPhotoMaxMb(int mb)
        => $"Each photo must be at most {mb} MB";
}
