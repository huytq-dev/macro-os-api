namespace Contract;

/// <summary>Một khẩu phần của món đã khớp (chip small/medium/large/g trong AmountSheet): Label không có tiền tố "1 " ("large"), GramsPerUnit để FE tính lại macro khi đổi cỡ.</summary>
public sealed record AiServingModel(Guid Id, string Label, decimal GramsPerUnit, bool IsDefault);
