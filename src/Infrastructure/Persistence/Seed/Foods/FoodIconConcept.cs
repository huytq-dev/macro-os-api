using System.Diagnostics;
using System.Text;

namespace Infrastructure;

/// <param name="Kind">"none" = không khớp cụm nào và không có icon chung; "generic" = chỉ được icon chung theo nhóm (đáng vẽ icon riêng).</param>
public sealed record FoodIconConcept(string Concept, string Kind, int Count, IReadOnlyList<string> Examples);
