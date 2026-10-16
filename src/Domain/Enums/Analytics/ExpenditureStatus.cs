namespace Domain;

// Calibrating: chưa đủ data (< 2 tuần)
// Updating: đang nhận weight log đều, algorithm đang tính
// Holding: user ngừng log cân, algorithm tạm dừng
public enum ExpenditureStatus { Calibrating, Updating, Holding }
