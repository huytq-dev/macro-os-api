namespace Application;

/// <param name="Name">Tên khẩu phần đã dựng sẵn, vd "1 cup, sliced".</param>
public sealed record UsdaPortion(string Name, decimal Grams);
