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
    /// <summary>Độ dài chuỗi trong khoảng (rule <c>Length(min, max)</c>).</summary>
    public const string LengthRange = "{PropertyName} must be between {MinLength} and {MaxLength} characters";
    /// <summary>Giá trị số trong khoảng (rule <c>InclusiveBetween</c>/<c>ExclusiveBetween</c>: placeholder {From}/{To}).</summary>
    public const string NumberRange = "{PropertyName} must be between {From} and {To}";

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
    public const string RefeedDaysRange = "Select between 1 and 6 different days.";
    public static string CalorieShiftNonPositive(DayOfWeek day, int calories)
        => $"{day}: only {calories} kcal left after the shift (must be greater than 0).";
    public static string CalorieShiftBelowFloor(DayOfWeek day, int calories, int floorKcal)
        => $"{day}: {calories} kcal after the shift, below the calorie floor of {floorKcal}.";
    public static string CalorieShiftCarbsBelowZero(DayOfWeek day, decimal carbsG)
        => $"{day}: carbs would be {carbsG} g after the shift (must be >= 0, otherwise macros no longer match calories).";
    // Target feasibility (INVALID_CALORIE_TARGET | MACRO_BUDGET_EXCEEDED | INFEASIBLE_PROGRAM): actionable, never "silently fixed"
    public const string TargetInfeasible = "This plan produces targets that cannot be followed. Slow the weekly rate of change, raise the calorie target or choose different macro preferences; protein and fat are never lowered automatically and nothing was saved.";
    private static string Where(string? day) => day is null ? "Daily target" : day;
    public static string TargetNonPositive(string? day, int calories)
        => $"{Where(day)}: the calorie target would be {calories} kcal (must be greater than 0). Slow the weekly rate of weight change, raise the calorie target or change the goal.";
    public static string TargetBelowFloor(string? day, int calories, int floorKcal)
        => $"{Where(day)}: {calories} kcal is below the calorie floor of {floorKcal}. Adjust the goal rate or the calorie floor.";
    public static string TargetMacroExceeds(string? day, int calories, int macroKcal, int minCalories)
        => $"{Where(day)}: protein, carbs and fat need {macroKcal} kcal but the target is {calories} kcal (at least {minCalories} kcal required). Raise the calorie target, slow the weekly rate or lower the protein level / diet style yourself; macros are never lowered automatically.";
    public static string TargetNegativeMacro(string? day)
        => $"{Where(day)}: a macro target would be negative. Raise the calorie target, slow the weekly rate or change the protein level / diet style.";
    public static string WeeklyBudgetNotConserved(int budgetKcal, int plannedKcal)
        => $"Locked or fasting days leave no room to fit the weekly budget ({plannedKcal} kcal planned vs {budgetKcal} kcal). Unlock a day or change the budget.";
    public const string ManualProgramNeedsSevenDayTargets ="Program Manual cần target đủ 7 ngày.";
    public static string MacroKcalMismatch(string? day, int calories, int macroKcal, int differenceKcal)
        => $"{Where(day)}: your protein, carbs and fat add up to {macroKcal} kcal but the calorie target is {calories} kcal ({(differenceKcal > 0 ? "+" : "")}{differenceKcal} kcal). You can still save; nothing was changed. Adjust a macro or the calorie target if you want them to match.";
    public static string ShiftReduceSuggestion(string days, decimal suggestedPercent)
        => $"The calorie shift makes {days} invalid. Reduce the shift to {suggestedPercent}% or use an even distribution; nothing was changed until you confirm.";
    public static string ShiftEvenSuggestion(string days)
        => $"No calorie shift within the allowed range keeps {days} valid. Use an even distribution (confirm to apply) or adjust the goal rate; nothing was changed.";
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

    // --- Chuyển từ chuỗi viết cứng ở handler/validator (CLAUDE.md: message không hardcode) ---
    public const string OneDefaultServing = "Chỉ được một serving mặc định.";
    public const string HourRangeLastAfterFirst = "HourRangeLast phải lớn hơn HourRangeFirst ít nhất 1 giờ.";
    public const string MacroInNutrientInput = "4 macro (kcal, protein, carbs, fat) nhập ở Nutrition, không nhập ở Nutrients.";
    public const string MaxFdcIds50 = "Tối đa 50 fdcId dương.";
    public const string NeedToDateOrToDates = "Cần ToDate hoặc ToDates.";
    public const string GoalOrder = "Phải đúng thứ tự Floor ≤ Target ≤ Ceiling.";
    public const string InvalidBarcode = "Mã vạch gồm 8–14 chữ số.";
    public const string RecipeLineHasNoServing = "Recipe không có khẩu phần riêng.";
    public const string MaxNutrients200 = "Tối đa 200 chất.";
    public const string FromAfterTo = "From must not be after To.";
    public const string NeedQueryOrFdcIds = "Cần Query hoặc FdcIds.";
    public const string FastingDayNoEnergy = "A fasting day has no energy or macros: leave them empty or 0.";
    public const string MustBeAdult = "Bạn cần đủ 18 tuổi để tiếp tục.";
    public const string MaxPins8 = "Tối đa 8 nutrient được pin.";
    public const string MustAcceptHealthDisclaimer = "Cần đồng ý Health Disclaimer để tiếp tục.";
    public const string DuplicateWidgetKey = "WidgetKey bị trùng.";
    public const string EntryLineFoodOrRecipeExactlyOne = "Mỗi dòng cần đúng một trong FoodItemId hoặc RecipeId.";
    public const string EnergyRequiredWithMacros = "Energy is required when protein, carbs or fat are given.";
    public const string InvalidId = "Id không hợp lệ.";
    public const string SourceEqualsTarget = "Nguồn và đích trùng nhau.";
    public const string NeedOneMoveTarget = "Cần ít nhất một trong ToDate, ToTime, ToMealType.";
    public const string DuplicateNutrientCode = "Mã chất bị trùng.";
    public const string InvalidList = "Danh sách không hợp lệ.";
    public const string FoodOrRecipeExactlyOne = "Cần đúng một trong FoodItemId hoặc RecipeId.";
    public const string InvalidBirthDate = "Ngày sinh không hợp lệ.";
    public const string EndDateBeforeStart = "EndDate phải sau hoặc bằng StartDate.";
    public const string RecipeHasNoOwnServing = "Recipe không có khẩu phần riêng (FoodServingId phải để trống).";
    public const string EntryServingUnverifiable = "Entry này được log trước khi lưu snapshot khẩu phần nên không xác minh được khối lượng gốc: không thể đổi khẩu phần. Hãy xoá và log lại món, hoặc chỉ đổi số lượng.";
    public const string InvalidDateRange730 ="Khoảng ngày không hợp lệ (tối đa 730 ngày).";
    public const string DuplicateCode = "Code bị trùng.";
    public const string MaxIngredients100 = "Tối đa 100 nguyên liệu.";
    public const string MustAcceptPrivacyNotice = "Cần đồng ý Consumer Health Privacy Notice để tiếp tục.";
    public static string MaxItemsPerBatch(int max)
        => $"Tối đa {max} món mỗi lô.";
    public static string MaxDatesPerCall(int max)
        => $"Tối đa {max} ngày mỗi lần.";
    public static string MaxEntriesPerCall(int max)
        => $"Tối đa {max} món mỗi lần.";
    public static string DateRangeInvalid(int maxDays)
        => $"Range must be valid (From ≤ To) and at most {maxDays} days.";
    public static string RangeTooLong(int maxDays)
        => $"Range must not exceed {maxDays} days.";
    public static string MaxCopiesPerCall(int max)
        => $"Tối đa {max} entry được tạo mỗi lần (số món × số ngày).";
    public static string MaxPlateLines(int max)
        => $"At most {max} items";
}
