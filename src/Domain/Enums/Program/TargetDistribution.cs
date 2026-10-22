namespace Domain;

/// <summary>Cách tuần được chia thành 7 ngày tại thời điểm chụp snapshot (D2): Flat = mọi ngày bằng target tuần; DayRows = dòng MacroProgramDayTarget theo ngày; TrainingDays/Custom = dịch kcal của program.</summary>
public enum TargetDistribution { Flat, DayRows, TrainingDays, Custom }
