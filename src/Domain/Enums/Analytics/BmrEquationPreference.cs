namespace Domain;

/// <summary>Phương trình BMR user chọn cho ước tính khởi điểm: MacroFactor (MacroOS tự chọn Eq1/Eq2/Eq3 theo dữ liệu có) hoặc Cunningham (cần lean mass).</summary>
public enum BmrEquationPreference { MacroFactor, Cunningham }
