namespace Application;

/// <summary>Yêu cầu chứa dữ liệu nhạy cảm (mật khẩu, token, email): không được ghi nội dung vào nhật ký, kể cả khi chạy chậm.</summary>
public interface ISensitiveRequest;
