namespace Application;

/// <summary>Kết quả một lần worker chạy job đã nhận: Done = đã dựng xong và đánh dấu Succeeded; Busy = không lấy được khoá user (instance khác đang dựng), job được trả về Pending.</summary>
public enum JobRunOutcome { Done, Busy }
