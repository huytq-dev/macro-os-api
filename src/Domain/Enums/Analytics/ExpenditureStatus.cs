namespace Domain;

// Updating: đang nhận weight log đều, algorithm đang tính (học từ ngày 1 của program, không đóng băng)
// Holding: thiếu log/cân, algorithm tạm dừng, giữ TDEE cũ
// Calibrating: giá trị CŨ giữ để tương thích DB. Chỉ còn là snapshot khởi tạo (TDEE₀ lúc tạo program/Reset/SetManual) và dữ liệu cũ chưa dựng lại;
// nhãn "đang hiệu chỉnh" của UI là IsCalibrating trên response, tách khỏi trạng thái thuật toán
public enum ExpenditureStatus { Calibrating, Updating, Holding }
