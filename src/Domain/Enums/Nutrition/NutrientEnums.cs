namespace Domain;

public enum NutrientGroup { Energy, Macro, Fat, Carb, AminoAcid, Vitamin, Mineral, Other }

// Unknown/NotProvided khác 0: giá trị 0 thật là Measured với Amount = 0
public enum NutrientAmountStatus { Unknown, NotProvided, Estimated, Measured }

// Unmarked = user chưa đánh dấu (UI hiển thị Unlogged nếu chưa có entry); Fasting chỉ khi user xác nhận
public enum FoodLogStatus { Unmarked, Complete, Partial, Fasting }

public enum DayStatusSource { User, System }

public enum ScanMode { Barcode, Label, Photo, Describe }

// Completed = AI/OCR trả kết quả; Confirmed = user đã duyệt/sửa; chỉ Confirmed mới được dùng để log
public enum ScanStatus { Pending, Processing, Completed, Failed, Confirmed, Rejected }
