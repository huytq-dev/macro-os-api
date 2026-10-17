namespace Domain;

// Cài đặt trang Food Log (MacroFactor): lưu dạng chuỗi, JSON trả đúng tên enum

// NoOverages: vượt target không hiện số vượt; ShowOverages: hiện
public enum OverageDisplay { NoOverages, ShowOverages }

public enum TimelineAlignment { Left, Right }

// Cách thêm món vào một giờ trên timeline
public enum AddFoodTrigger { PlusIcon, LongPress }

public enum WeekBannerMode { Show, ShowAndPin, Hide }

public enum CalendarScrollMode { Daily, Weekly }

public enum SearchOptimization { Speed, Accuracy }

// Mật độ ô món trên timeline
public enum TileDensity { Comfort, Standard, Compact }

// Nhấn mạnh ô món: Edit = nút sửa, Energy = làm nổi theo năng lượng, None = không
public enum TileHighlight { Edit, Energy, None }

// Banner đầu bảng Thêm món: chỉ đĩa đang soạn / cả ngày + đĩa / còn lại sau đĩa
public enum BannerDisplay { PlateOnly, AllDayAndPlate, RemainingAfterPlate }

// Nhấn mạnh dòng kết quả tìm món: MultiAdd = nút + thêm thẳng vào plate, Energy = làm nổi theo năng lượng, None = không
public enum SearchRowHighlight { MultiAdd, Energy, None }
