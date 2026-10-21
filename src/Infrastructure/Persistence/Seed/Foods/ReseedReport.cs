namespace Infrastructure;

/// <param name="References">Số bản ghi dữ liệu NGƯỜI DÙNG đang trỏ vào món catalog (tên bảng → số dòng); các món này KHÔNG bị xoá, chỉ đổi tên/khẩu phần/icon tại chỗ.</param>
public sealed record ReseedReport(
    int CatalogFoods, int FoodsRenamed, int CanonicalsRenamed, int LanguageFixed, int ServingsRenamed, int IconsChanged, int IconsStillNull,
    IReadOnlyList<string> NullIconSamples, int ViAliasesRemoved, int CanonicalNameViCleared, IReadOnlyDictionary<string, int> References, bool Applied);
