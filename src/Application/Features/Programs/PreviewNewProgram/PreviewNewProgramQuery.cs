namespace Application;

/// <summary>Xem trước program từ prefs của wizard (cùng body với <see cref="CreateProgramCommand"/>), không ghi DB. Số khớp đúng số sẽ lưu khi tạo.</summary>
public sealed record PreviewNewProgramQuery(CreateProgramCommand Draft) : IRequest<ProgramDraftPreviewResponse>;
