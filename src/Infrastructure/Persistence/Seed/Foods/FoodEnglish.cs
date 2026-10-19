using System.Text.RegularExpressions;

namespace Infrastructure;

/// <summary>
/// Tên và khẩu phần tiếng Anh cho danh mục seed (60 món FoodData, 49 món VN, gia vị). Tên Việt vẫn nằm trong dữ liệu gốc làm alias tìm kiếm
/// nhưng KHÔNG hiển thị: <see cref="CanonicalCatalogData.IncludeVietnamese"/> = false bỏ luôn alias/tên Việt khỏi seed.
/// </summary>
public static partial class FoodEnglish
{
    /// <summary>Tên hiển thị tiếng Anh của món seed (key = ExternalFoodId của FoodData).</summary>
    public static readonly IReadOnlyDictionary<string, string> SeedNames = new Dictionary<string, string>
    {
        ["egg-boiled"] = "Egg, hard-boiled", ["chicken-breast"] = "Chicken breast, cooked", ["chicken-thigh"] = "Chicken thigh, skinless, cooked",
        ["beef-lean"] = "Beef, lean, cooked", ["pork-lean"] = "Pork, lean, cooked", ["salmon-grilled"] = "Salmon, grilled",
        ["shrimp-boiled"] = "Shrimp, boiled", ["tofu"] = "Tofu", ["tuna-canned"] = "Tuna, canned", ["milk-fresh"] = "Milk, unsweetened",
        ["yogurt-plain"] = "Yogurt, plain", ["yogurt-sweet"] = "Yogurt, sweetened", ["cheddar"] = "Cheddar cheese", ["whey"] = "Whey protein powder",
        ["rice-white"] = "White rice, cooked", ["rice-brown"] = "Brown rice, cooked", ["sticky-rice"] = "Sticky rice, cooked",
        ["bun"] = "Rice vermicelli, fresh", ["pho-noodle"] = "Pho noodles, rice noodles", ["bread-roll"] = "Bread roll, plain",
        ["bread-slice"] = "Sandwich bread", ["oats"] = "Oats, dry", ["sweet-potato"] = "Sweet potato, boiled", ["potato"] = "Potato, boiled",
        ["corn"] = "Corn, boiled", ["tomato"] = "Tomato", ["carrot"] = "Carrot", ["cucumber"] = "Cucumber", ["broccoli"] = "Broccoli, boiled",
        ["spinach"] = "Spinach", ["cabbage"] = "Cabbage", ["water-spinach"] = "Water spinach", ["banana"] = "Banana", ["apple"] = "Apple",
        ["orange"] = "Orange", ["mango"] = "Mango", ["watermelon"] = "Watermelon", ["avocado"] = "Avocado", ["dragon-fruit"] = "Dragon fruit",
        ["papaya"] = "Papaya", ["pineapple"] = "Pineapple", ["olive-oil"] = "Cooking oil", ["peanut-butter"] = "Peanut butter",
        ["almond"] = "Almonds", ["cashew"] = "Cashews", ["coffee-black"] = "Black coffee", ["coffee-milk"] = "Iced milk coffee",
        ["bubble-tea"] = "Bubble tea", ["orange-juice"] = "Orange juice", ["cola"] = "Cola", ["dark-chocolate"] = "Dark chocolate",
        ["potato-chips"] = "Potato chips", ["pho-beef"] = "Beef pho", ["bun-bo"] = "Bun bo Hue, spicy beef noodle soup", ["banh-mi-thit"] = "Banh mi, pork",
        ["com-tam"] = "Broken rice with grilled pork chop", ["goi-cuon"] = "Fresh spring roll", ["pizza"] = "Pizza", ["hamburger"] = "Hamburger",
        ["french-fries"] = "French fries",
    };

    /// <summary>Thực phẩm chuẩn (canonical) từng mang tên Việt trong DB cũ → tên Anh mới; dùng khi chuyển DB đã seed sang tiếng Anh tại chỗ.</summary>
    public static readonly IReadOnlyDictionary<string, string> CanonicalRenames = new Dictionary<string, string>
    {
        ["Mắm ruốc"] = "Mam ruoc (fermented shrimp paste)", ["Mắm nêm"] = "Mam nem (fermented anchovy sauce)", ["Tương bần"] = "Tuong ban (fermented bean paste)",
        ["Tương hột"] = "Tuong hot (fermented soybean sauce)", ["Sa tế"] = "Sa te (chili lemongrass sauce)", ["Hạt nêm"] = "Seasoning Granules",
        ["Bột canh"] = "Bot canh (seasoned salt)", ["Caramel Syrup (Nước hàng)"] = "Caramel Syrup",
    };

    // Đơn vị khẩu phần tiếng Việt → tiếng Anh (phần chữ sau số lượng)
    private static readonly Dictionary<string, string> Units = new()
    {
        ["chén"] = "cup", ["chén múi"] = "cup segments", ["chùm nhỏ"] = "small bunch", ["con"] = "piece", ["con vừa"] = "medium piece",
        ["cây"] = "stalk", ["cọng"] = "stalk", ["củ"] = "piece", ["củ vừa"] = "medium", ["lon"] = "can", ["ly"] = "glass", ["lát"] = "slice",
        ["miếng"] = "piece", ["muỗng"] = "scoop", ["muỗng canh"] = "tbsp", ["muỗng cà phê"] = "tsp", ["múi"] = "segment", ["nhánh"] = "sprig",
        ["nắm"] = "handful", ["quả"] = "piece", ["trái"] = "piece", ["tép"] = "clove", ["tô"] = "bowl", ["tờ"] = "sheet", ["đĩa"] = "plate",
        ["dĩa"] = "plate", ["bắp"] = "ear", ["cuốn"] = "roll", ["cái"] = "piece", ["gói"] = "pack", ["gói nhỏ"] = "small pack", ["hộp"] = "container",
        ["phần"] = "serving", ["phần vừa"] = "medium serving", ["thanh"] = "bar", ["ổ"] = "loaf",
    };

    [GeneratedRegex(@"^(\d+(?:/\d+)?)\s+(.+)$")]
    private static partial Regex ServingPattern();

    /// <summary>"1/2 chén" → "1/2 cup", "1 muỗng canh" → "1 tbsp". Tên đã là tiếng Anh hoặc đơn vị lạ thì giữ nguyên (<see cref="HasVietnamese"/> báo còn sót).</summary>
    public static string ServingName(string name)
    {
        var m = ServingPattern().Match(name.Trim());
        return m.Success && Units.TryGetValue(m.Groups[2].Value.Trim(), out var en) ? $"{m.Groups[1].Value} {en}" : name;
    }

    /// <summary>Còn chữ có dấu/đ tiếng Việt.</summary>
    public static bool HasVietnamese(string? s) => FoodNameNormalizer.DetectLocale(s) == "vi-VN";

    /// <summary>
    /// Lọc alias còn lại là tiếng Anh: bỏ alias có dấu, và dạng không dấu của alias/tên Việt đã biết ("nuoc mam" đi kèm "nước mắm").
    /// <paramref name="knownVietnamese"/> là tên/alias Việt đã biết của cùng món (kể cả khi không có trong <paramref name="tokens"/>).
    /// </summary>
    public static List<string> EnglishOnly(IEnumerable<string> tokens, IEnumerable<string>? knownVietnamese = null, IEnumerable<string>? explicitEnglish = null)
    {
        var list = tokens.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        var keep = (explicitEnglish ?? []).Select(FoodNameNormalizer.Normalize).ToHashSet();   // tên Anh liệt kê tường minh ("Pho") luôn giữ dù trùng dạng không dấu của "phở"
        var vi = list.Where(HasVietnamese).Concat(knownVietnamese ?? []).Select(FoodNameNormalizer.Normalize).ToHashSet();
        return list.Where(t => !HasVietnamese(t) && (keep.Contains(FoodNameNormalizer.Normalize(t)) || !vi.Contains(FoodNameNormalizer.Normalize(t)))).ToList();
    }
}
