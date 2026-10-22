namespace Domain;

/// <summary>Tham số sinh học user tự xác nhận cho phương trình BMR (Equation 1 có số hạng giới). Tách khỏi <see cref="Gender"/> (giới tính hiển thị): không bao giờ suy ra từ Gender = Other, không có giá trị mặc định.</summary>
public enum BiologicalSex { Male, Female }
