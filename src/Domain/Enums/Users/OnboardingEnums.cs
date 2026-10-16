namespace Domain;

// Đáp án màn "Basics" của wizard thiết lập (luồng MacroFactor). Lưu dạng chuỗi enum, dùng để ước tính TDEE khởi điểm.

public enum WeightHistoryAnswer { Yes, No, NotSure }

public enum WeightTrendAnswer { Losing, Gaining, Stable, NotSure }

/// <summary>Khoảng % mỡ cơ thể ước lượng bằng mắt (9 mức).</summary>
public enum BodyFatBand
{
    Bf10To13, Bf14To17, Bf18To23, Bf24To28, Bf29To33, Bf34To37, Bf38To42, Bf43To49, Bf50Plus
}

public enum ExerciseSessionsBand { None, OneToThree, FourToSix, SevenPlus }

/// <summary>Mức vận động ngoài tập luyện (theo số bước/ngày).</summary>
public enum DailyActivityBand { MostlySedentary, ModeratelyActive, VeryActive }

public enum ExperienceLevel { None, Beginner, Intermediate, Advanced }
