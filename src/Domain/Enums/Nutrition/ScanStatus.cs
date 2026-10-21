namespace Domain;

// Completed = AI/OCR trả kết quả; Confirmed = user đã duyệt/sửa; chỉ Confirmed mới được dùng để log
public enum ScanStatus { Pending, Processing, Completed, Failed, Confirmed, Rejected }
