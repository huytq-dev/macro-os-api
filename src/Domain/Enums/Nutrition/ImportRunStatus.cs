namespace Domain;

// Partial: chạy xong nhưng có món bị lỗi/bỏ qua do thiếu dữ liệu
public enum ImportRunStatus { Running, Succeeded, Partial, Failed }
