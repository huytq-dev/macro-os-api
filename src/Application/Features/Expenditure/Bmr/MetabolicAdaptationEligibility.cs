namespace Application;

/// <summary>Kết quả xét điều kiện "cân hiện tại thấp hơn đỉnh &gt; 10%" từ đáp án Prior Weight (MF-VERIFIED điều kiện 10%; cách chứng minh từ đáp án là quy tắc bảo thủ của MacroOS).</summary>
public enum MetabolicAdaptationEligibility
{
    /// <summary>Không áp dụng: đáp án No / Not sure / chưa trả lời (không điều chỉnh theo đỉnh, không bao giờ coi Not sure là Yes hay No).</summary>
    NotApplicable,
    /// <summary>Yes nhưng không chứng minh được > 10% (ngưỡng đã hiển thị thấp hơn cân/0.9, hoặc thiếu ngưỡng/cân lúc trả lời): KHÔNG áp ×0.97/×0.92, không dựng lại số liệu.</summary>
    Unknown,
    /// <summary>Yes và ngưỡng đã hiển thị ≥ cân lúc trả lời / 0.9 (hoặc có bằng chứng cân đỉnh thật): điều kiện 10% đã được chứng minh.</summary>
    Established,
}
