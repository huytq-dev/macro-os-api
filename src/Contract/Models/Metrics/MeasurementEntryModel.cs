namespace Contract;

/// <summary>Một lần đo của một số đo vòng, đơn vị cm (FE đổi sang in khi hiển thị).</summary>
public sealed record MeasurementEntryModel(DateOnly Date, decimal ValueCm);
