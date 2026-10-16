namespace Domain;

public enum NutritionProgramStatus { Draft, Active, Superseded, Archived }

public enum ProgramSource { Manual, Generated, Duplicated }

// Even = chia đều; TrainingDays = ngày tập nhiều hơn (tổng tuần giữ nguyên); Custom = user chọn ngày
public enum CalorieShiftMode { Even, TrainingDays, Custom }

public enum DayType { Standard, Training, Rest }
