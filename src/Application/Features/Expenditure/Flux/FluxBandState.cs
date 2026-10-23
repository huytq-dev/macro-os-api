namespace Application;

/// <summary>
/// Trạng thái của dải Flux một ngày. Calibrating và NoObservations KHÔNG có dải (null). Holding: không có quan sát mới nên không ứng viên nào cập nhật, dải đứng yên ở mức cuối (vẫn có dải).
/// </summary>
public enum FluxBandState { Calibrating, NoObservations, Updating, Holding }
