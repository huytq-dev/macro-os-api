namespace Domain;

// Verified > Label > UserEntered > AiEstimated > Unverified khi chọn nguồn hiển thị
public enum FoodQualityStatus { Unverified, UserEntered, Label, Verified, AiEstimated }

public enum ServingUnitType { Mass, Volume, Piece, Household }

public enum EntrySource { Search, Barcode, Label, QuickAdd, Describe, AiPhoto, Recipe, Template, Copy, Planner }


// Partial: chạy xong nhưng có món bị lỗi/bỏ qua do thiếu dữ liệu
public enum ImportRunStatus { Running, Succeeded, Partial, Failed }

public enum CanonicalFoodStatus { Active, Deprecated }
