namespace Domain;

public enum NutrientGroup { Energy, Macro, Fat, Carb, AminoAcid, Vitamin, Mineral, Other }

// Unknown/NotProvided khác 0: giá trị 0 thật là Measured với Amount = 0
public enum NutrientAmountStatus { Unknown, NotProvided, Estimated, Measured }

public enum ScanMode { Barcode, Label, Photo, Describe }

// Completed = AI/OCR trả kết quả; Confirmed = user đã duyệt/sửa; chỉ Confirmed mới được dùng để log
public enum ScanStatus { Pending, Processing, Completed, Failed, Confirmed, Rejected }
