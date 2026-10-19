namespace Infrastructure;

/// <summary>
/// Nhóm gia vị / dầu mỡ / đường / nước chấm. Mỗi biến thể (Kosher Salt, Sea Salt, Olive Oil…) là một FoodItem riêng, cùng một thực phẩm chuẩn (canonical) với món đại diện,
/// không phải alias. Số liệu /100 g theo USDA FoodData Central (SR Legacy): <see cref="CondimentVerifiedKeys"/> là các dòng đã đối chiếu trực tiếp với API USDA (QualityStatus Verified);
/// các dòng còn lại ghi từ bảng USDA/nhãn theo trí nhớ nên Unverified, và <see cref="CondimentUncertainKeys"/> là dòng khối lượng khẩu phần, hỗn hợp hoặc món Việt tự ước/không chắc nguồn.
/// Sodium (FoodNutrient sodium_mg) chỉ ghi khi có số đáng tin; thiếu = Unknown (không ghi dòng), KHÔNG để 0. kcal = 0 của muối/bột ngọt/baking soda là dữ liệu đúng.
/// Khẩu phần: "tên=gram|tên=gram", mục đầu là mặc định. Pinch = 1/16 tsp, dash = 1/8 tsp (quy ước Mỹ) nên gram tính theo gram của tsp.
/// </summary>
public static partial class CanonicalCatalogData
{
    public const string CondimentProvider = "cond";

    /// <param name="Canonical">Tên Anh của thực phẩm chuẩn (có thể là canonical đã có: Salt, Sugar, Oil…).</param>
    /// <param name="Vi">Tên Việt của canonical (chỉ cần ở dòng đại diện của canonical mới).</param>
    /// <param name="Rep">Dòng đại diện của canonical; canonical mới mà không có dòng nào Rep thì dòng đầu tiên làm đại diện.</param>
    public sealed record CondimentFood(
        string Key, string Canonical, string Name, string? Vi, FoodCategory Cat,
        decimal Kcal, decimal P, decimal C, decimal F, decimal? SodiumMg,
        string Servings, string Aliases, bool Rep = false);

    private static CondimentFood K(string key, string canonical, string name, string? vi, FoodCategory cat,
        decimal kcal, decimal p, decimal c, decimal f, decimal? na, string servings, string aliases, bool rep = false)
        => new(key, canonical, name, vi, cat, kcal, p, c, f, na, servings, aliases, rep);

    /// <summary>Canonical đã có ở seed cũ (tên Anh → key món seed) mà biến thể mới gắn vào; không tạo canonical mới.</summary>
    public static readonly IReadOnlyDictionary<string, string> ExistingCanonicalKeys = new Dictionary<string, string>
    {
        ["Salt"] = "vn-salt", ["Sugar"] = "vn-sugar", ["Oil"] = "olive-oil", ["Fish sauce"] = "vn-fish-sauce", ["Soy sauce"] = "vn-soy-sauce",
        ["Oyster sauce"] = "vn-oyster-sauce", ["Chili sauce"] = "vn-chili-sauce", ["Mayonnaise"] = "vn-mayonnaise", ["Black pepper"] = "vn-pepper", ["Lard"] = "vn-lard",
    };

    /// <summary>Natri (mg/100 g) bổ sung cho món seed cũ đã có số USDA đã đối chiếu (key món seed → sodium).</summary>
    public static readonly IReadOnlyDictionary<string, decimal> ExistingSodium = new Dictionary<string, decimal>
    {
        ["vn-salt"] = 38800m, ["vn-fish-sauce"] = 7850m, ["vn-soy-sauce"] = 5490m, ["vn-sugar"] = 1m,
    };

    private const string Tsp1 = "1 tsp=6|1/4 tsp=1.5|1 pinch=0.4|1 dash=0.75|1 g=1";

    private static readonly FoodCategory Sp = FoodCategory.Spices, Cd = FoodCategory.Condiments, Ft = FoodCategory.Fats, Vg = FoodCategory.Vegetables;

    public static readonly CondimentFood[] Condiments =
    [
        // --- Muối (canonical Salt; đại diện là muối ăn thường, đều 0 kcal) ---
        K("cond-salt-table", "Salt", "Salt", "Muối ăn", Sp, 0, 0, 0, 0, 38800, Tsp1, "table salt|muối ăn|muối tinh|muối bàn|muối", rep: true),
        K("cond-salt-iodized", "Salt", "Iodized Table Salt", null, Sp, 0, 0, 0, 0, 38800, Tsp1, "iodized salt|iodized table salt|muối i-ốt|muối iot"),
        K("cond-salt-sea", "Salt", "Sea Salt Non-Iodized", null, Sp, 0, 0, 0, 0, 38800, "1 tsp=5.5|1/4 tsp=1.4|1 pinch=0.35|1 dash=0.7|1 g=1", "sea salt|non-iodized sea salt|muối biển"),
        K("cond-salt-kosher", "Salt", "Kosher Salt", null, Sp, 0, 0, 0, 0, 38800,
            "1 tsp (Diamond Crystal)=2.8|1 tsp (Morton)=4.8|1/4 tsp (Diamond Crystal)=0.7|1/4 tsp (Morton)=1.2|1 pinch (Diamond Crystal)=0.2|1 pinch (Morton)=0.3|1 g=1", "kosher salt|muối kosher"),
        K("cond-salt-himalayan", "Salt", "Himalayan Pink Salt", null, Sp, 0, 0, 0, 0, null, "1 tsp=5.5|1/4 tsp=1.4|1 pinch=0.35|1 dash=0.7|1 g=1", "himalayan salt|himalayan pink salt|pink salt|muối hồng|muối himalaya"),
        K("cond-salt-sea-fine", "Salt", "Fine Sea Salt", null, Sp, 0, 0, 0, 0, 38800, "1 tsp=5.9|1/4 tsp=1.5|1 pinch=0.37|1 dash=0.74|1 g=1", "fine sea salt|muối biển mịn"),
        K("cond-salt-flaky", "Salt", "Flaky Sea Salt", null, Sp, 0, 0, 0, 0, 38800, "1 tsp=2.5|1/4 tsp=0.6|1 pinch=0.15|1 g=1", "flaky sea salt|fleur de sel|maldon salt|muối vảy|muối bông"),
        K("cond-salt-black", "Salt", "Black Salt (Kala Namak)", null, Sp, 0, 0, 0, 0, null, "1 tsp=5.5|1/4 tsp=1.4|1 pinch=0.35|1 g=1", "black salt|kala namak|muối đen"),
        K("cond-salt-garlic", "Salt", "Garlic Salt", null, Sp, 83, 4.2m, 18.2m, 0.2m, null, "1 tsp=5.5|1/4 tsp=1.4|1 pinch=0.35|1 dash=0.7|1 g=1", "garlic salt|muối tỏi"),
        K("cond-salt-celery", "Salt", "Celery Salt", null, Sp, 98, 4.5m, 10.4m, 6.3m, null, "1 tsp=5.5|1/4 tsp=1.4|1 pinch=0.35|1 dash=0.7|1 g=1", "celery salt|muối cần tây"),
        K("cond-salt-onion", "Salt", "Onion Salt", null, Sp, 85, 2.6m, 19.8m, 0.3m, null, "1 tsp=5.5|1/4 tsp=1.4|1 pinch=0.35|1 dash=0.7|1 g=1", "onion salt|muối hành"),
        K("cond-salt-seasoned", "Salt", "Seasoned Salt", null, Sp, 70, 3.5m, 13.5m, 3.2m, null, "1 tsp=5.5|1/4 tsp=1.4|1 pinch=0.35|1 dash=0.7|1 g=1", "seasoned salt|muối gia vị|muối tẩm gia vị"),
        K("cond-salt-lite", "Salt", "Lite Salt (Salt Substitute)", null, Sp, 0, 0, 0, 0, 19400, "1 tsp=6|1/4 tsp=1.5|1 pinch=0.4|1 dash=0.75|1 g=1", "lite salt|salt substitute|muối ăn kiêng|muối ít natri"),
        K("cond-salt-chili", "Salt", "Vietnamese Chili Salt", null, Sp, 40, 1.2m, 8, 1.3m, null, "1 tsp=6|1/4 tsp=1.5|1 pinch=0.4|1 g=1", "muối ớt xanh|muối ớt tây ninh|chili salt"),
        K("cond-salt-lime-pepper", "Salt", "Vietnamese Lime-Pepper Salt", null, Sp, 10, 0.4m, 2.5m, 0.1m, null, "1 tsp=6|1/4 tsp=1.5|1 pinch=0.4|1 g=1", "muối tiêu chanh|lemon pepper salt|lime pepper salt"),
        K("cond-salt-shrimp", "Salt", "Vietnamese Shrimp Salt", null, Sp, 60, 4, 8, 1.5m, null, "1 tsp=6|1/4 tsp=1.5|1 pinch=0.4|1 g=1", "muối tôm tây ninh|shrimp salt"),
        K("cond-salt-coarse", "Salt", "Coarse Salt", null, Sp, 0, 0, 0, 0, 38800, "1 tsp=5|1/4 tsp=1.25|1 pinch=0.3|1 g=1", "muối hột|coarse salt|rock salt"),

        // --- Dầu và mỡ nấu ăn ---
        K("cond-oil-vegetable", "Vegetable Oil", "Vegetable Oil", "Dầu thực vật", Ft, 884, 0, 0, 100, 0, "1 tbsp=13.6|1 tsp=4.5|1 g=1", "soybean oil|dầu thực vật|dầu đậu nành|dầu ăn thực vật", rep: true),
        K("cond-oil-canola", "Canola Oil", "Canola Oil", "Dầu canola", Ft, 884, 0, 0, 100, 0, "1 tbsp=13.6|1 tsp=4.5|1 g=1", "rapeseed oil|dầu canola|dầu hạt cải", rep: true),
        K("cond-oil-olive", "Oil", "Olive Oil", null, Ft, 884, 0, 0, 100, 2, "1 tbsp=13.5|1 tsp=4.5|1 g=1", "olive oil|dầu ô-liu"),
        K("cond-oil-evoo", "Oil", "Extra Virgin Olive Oil", null, Ft, 884, 0, 0, 100, 2, "1 tbsp=13.5|1 tsp=4.5|1 g=1", "extra virgin olive oil|evoo|dầu ô liu nguyên chất"),
        K("cond-oil-sesame", "Sesame Oil", "Sesame Oil", "Dầu mè", Ft, 884, 0, 0, 100, 0, "1 tbsp=13.6|1 tsp=4.5|1 g=1", "dầu mè|dầu vừng|dầu mè rang", rep: true),
        K("cond-oil-coconut", "Coconut Oil", "Coconut Oil", "Dầu dừa", Ft, 892, 0, 0, 99.1m, 0, "1 tbsp=13.6|1 tsp=4.5|1 g=1", "dầu dừa", rep: true),
        K("cond-oil-sunflower", "Sunflower Oil", "Sunflower Oil", "Dầu hướng dương", Ft, 884, 0, 0, 100, 0, "1 tbsp=13.6|1 tsp=4.5|1 g=1", "dầu hướng dương", rep: true),
        K("cond-oil-peanut", "Peanut Oil", "Peanut Oil", "Dầu đậu phộng", Ft, 884, 0, 0, 100, 0, "1 tbsp=13.5|1 tsp=4.5|1 g=1", "groundnut oil|dầu lạc|dầu đậu phộng", rep: true),
        K("cond-fat-chicken", "Chicken Fat", "Chicken Fat", "Mỡ gà", Ft, 898, 0, 0, 99.8m, null, "1 tbsp=12.8|1 tsp=4.3|1 g=1", "schmaltz|mỡ gà", rep: true),
        K("cond-butter-salted", "Butter", "Butter, Salted", "Bơ sữa", Ft, 717, 0.85m, 0.06m, 81.1m, 643, "1 tbsp=14.2|1 tsp=4.7|1 pat=5|1 stick=113|1 g=1", "butter|salted butter|bơ sữa|bơ mặn", rep: true),
        K("cond-butter-unsalted", "Butter", "Butter, Unsalted", null, Ft, 717, 0.85m, 0.06m, 81.1m, 11, "1 tbsp=14.2|1 tsp=4.7|1 pat=5|1 stick=113|1 g=1", "unsalted butter|sweet butter|bơ lạt|bơ không muối"),
        K("cond-margarine", "Margarine", "Margarine", "Bơ thực vật", Ft, 717, 0.2m, 0.7m, 80.5m, 751, "1 tbsp=14.2|1 tsp=4.7|1 g=1", "margarine|bơ thực vật|bơ nhân tạo|magarin", rep: true),
        K("cond-ghee", "Ghee", "Ghee", "Bơ ghee", Ft, 876, 0.3m, 0, 99.5m, 2, "1 tbsp=12.8|1 tsp=4.3|1 g=1", "clarified butter|bơ tinh luyện|bơ ghee", rep: true),
        K("cond-shortening", "Shortening", "Vegetable Shortening", "Mỡ thực vật", Ft, 884, 0, 0, 100, 0, "1 tbsp=12.8|1 tsp=4.3|1 g=1", "shortening|mỡ thực vật", rep: true),

        // --- Đường và chất ngọt (canonical Sugar có sẵn) ---
        K("cond-sugar-granulated", "Sugar", "Granulated Sugar", null, Cd, 387, 0, 100, 0, 1, "1 tsp=4.2|1 tbsp=12.6|1 packet=4|1 g=1", "granulated sugar|white granulated sugar|đường kính"),
        K("cond-sugar-brown", "Sugar", "Brown Sugar", null, Cd, 380, 0.1m, 98.1m, 0, 28, "1 tsp=4.6|1 tbsp=13.8|1 g=1", "brown sugar|đường nâu|đường vàng"),
        K("cond-sugar-powdered", "Sugar", "Powdered Sugar", null, Cd, 389, 0, 99.8m, 0, 2, "1 tsp=2.5|1 tbsp=7.5|1 g=1", "powdered sugar|icing sugar|confectioners sugar|đường bột|đường xay"),
        K("cond-sugar-palm", "Palm Sugar", "Palm Sugar", "Đường thốt nốt", Cd, 375, 0, 93, 0, null, "1 tsp=4|1 tbsp=12|1 g=1", "coconut palm sugar|đường thốt nốt", rep: true),
        K("cond-sugar-rock", "Rock Sugar", "Rock Sugar", "Đường phèn", Cd, 387, 0, 100, 0, null, "1 tsp=4.2|1 tbsp=12.6|1 g=1", "crystal sugar|đường phèn", rep: true),
        K("cond-honey", "Honey", "Honey", "Mật ong", Cd, 304, 0.3m, 82.4m, 0, 4, "1 tsp=7|1 tbsp=21|1 g=1", "mật ong|mật ong nguyên chất", rep: true),
        K("cond-maple-syrup", "Maple Syrup", "Maple Syrup", "Siro cây phong", Cd, 260, 0, 67, 0.1m, 12, "1 tsp=6.7|1 tbsp=20|1 g=1", "siro cây phong|xi-rô cây phong", rep: true),
        K("cond-molasses", "Molasses", "Molasses", "Mật rỉ đường", Cd, 290, 0, 74.7m, 0.1m, 37, "1 tsp=6.7|1 tbsp=20|1 g=1", "mật rỉ đường|mật rỉ", rep: true),
        K("cond-corn-syrup", "Corn Syrup", "Corn Syrup", "Siro ngô", Cd, 281, 0, 76.8m, 0, null, "1 tsp=7|1 tbsp=21|1 g=1", "light corn syrup|siro ngô|xi-rô bắp", rep: true),
        K("cond-cane-syrup", "Cane Syrup", "Sugarcane Syrup", "Mật mía", Cd, 290, 0, 74.5m, 0, null, "1 tsp=7|1 tbsp=21|1 g=1", "cane syrup|mật mía|nước mía cô đặc", rep: true),
        K("cond-caramel-color", "Caramel Syrup", "Caramel Syrup", "Nước hàng", Cd, 300, 0, 75, 0, null, "1 tsp=6.7|1 tbsp=20|1 g=1", "nước hàng|nước màu|nước màu dừa|caramel sauce|caramelized sugar syrup", rep: true),
        K("cond-stevia", "Stevia", "Stevia Sweetener", "Cỏ ngọt (stevia)", Cd, 0, 0, 0, 0, null, "1 tsp=0.5|1 packet=1|1 g=1", "stevia|đường stevia|cỏ ngọt", rep: true),
        K("cond-sucralose", "Sucralose", "Sucralose Sweetener", "Chất tạo ngọt sucralose", Cd, 0, 0, 0, 0, null, "1 packet=1|1 tsp=1|1 g=1", "sucralose|splenda|chất tạo ngọt", rep: true),

        // --- Nước chấm và sốt ---
        K("cond-tamari", "Tamari", "Tamari", "Nước tương tamari", Cd, 60, 10.5m, 5.5m, 0.1m, 5586, "1 tbsp=18|1 tsp=6|1 g=1", "tamari|nước tương tamari", rep: true),
        K("cond-hoisin", "Hoisin Sauce", "Hoisin Sauce", "Tương hoisin", Cd, 220, 3.3m, 44.1m, 3.4m, 1881, "1 tbsp=16|1 tsp=5.3|1 g=1", "hoisin|tương hoisin|tương đen", rep: true),
        K("cond-sriracha", "Chili sauce", "Sriracha Sauce", null, Cd, 93, 1.9m, 19.2m, 0.9m, 2124, "1 tbsp=17|1 tsp=5.7|1 g=1", "sriracha sauce"),
        K("cond-sweet-chili", "Chili sauce", "Sweet Chili Sauce", null, Cd, 190, 0.5m, 46, 0.2m, null, "1 tbsp=18|1 tsp=6|1 g=1", "sweet chili sauce|tương ớt chua ngọt|sốt ớt chua ngọt"),
        K("cond-ketchup", "Ketchup", "Ketchup", "Tương cà", Cd, 101, 1.04m, 27.4m, 0.1m, 907, "1 tbsp=17|1 tsp=5.7|1 packet=9|1 g=1", "catsup|tương cà chua|sốt cà chua|xốt cà chua", rep: true),
        K("cond-mustard", "Mustard", "Mustard, Yellow", "Mù tạt", Cd, 60, 3.7m, 5.8m, 3.3m, 1135, "1 tsp=5|1 tbsp=15|1 g=1", "mustard|yellow mustard|mù tạt|mù tạt vàng", rep: true),
        K("cond-vinegar-white", "White Vinegar", "White Vinegar", "Giấm trắng", Cd, 18, 0, 0.04m, 0, 2, "1 tbsp=15|1 tsp=5|1 g=1", "vinegar|distilled vinegar|giấm|dấm|giấm ăn|giấm trắng", rep: true),
        K("cond-vinegar-rice", "Rice Vinegar", "Rice Vinegar", "Giấm gạo", Cd, 18, 0, 0.4m, 0, null, "1 tbsp=15|1 tsp=5|1 g=1", "rice vinegar|giấm gạo|dấm gạo", rep: true),
        K("cond-vinegar-balsamic", "Balsamic Vinegar", "Balsamic Vinegar", "Giấm balsamic", Cd, 88, 0.5m, 17, 0, 23, "1 tbsp=16|1 tsp=5.3|1 g=1", "balsamic vinegar|giấm balsamic|dấm balsamic", rep: true),
        K("cond-worcestershire", "Worcestershire Sauce", "Worcestershire Sauce", "Sốt Worcestershire", Cd, 78, 0, 19.5m, 0, 980, "1 tbsp=17|1 tsp=5.7|1 g=1", "worcestershire|nước sốt worcestershire", rep: true),
        K("cond-teriyaki", "Teriyaki Sauce", "Teriyaki Sauce", "Sốt teriyaki", Cd, 89, 5.9m, 15.6m, 0, 3833, "1 tbsp=18|1 tsp=6|1 g=1", "teriyaki|sốt teriyaki|xốt teriyaki", rep: true),
        K("cond-bbq", "Barbecue Sauce", "Barbecue Sauce", "Sốt BBQ", Cd, 172, 0.8m, 40.8m, 0.6m, null, "1 tbsp=17|1 tsp=5.7|1 g=1", "bbq sauce|barbecue sauce|sốt bbq|sốt thịt nướng|xốt bbq", rep: true),
        K("cond-ranch", "Ranch Dressing", "Ranch Dressing", "Sốt ranch", Cd, 430, 1.2m, 6, 45, null, "1 tbsp=15|1 tsp=5|1 g=1", "ranch|sốt ranch|xốt ranch", rep: true),
        K("cond-mirin", "Mirin", "Mirin", "Rượu mirin", Cd, 241, 0.2m, 43.2m, 0, null, "1 tbsp=18|1 tsp=6|1 g=1", "rượu mirin|mirin", rep: true),
        K("cond-cooking-wine", "Cooking Wine", "Cooking Wine", "Rượu nấu ăn", Cd, 85, 0.5m, 4, 0, null, "1 tbsp=15|1 tsp=5|1 g=1", "rượu nấu ăn|rượu gạo nấu|shaoxing wine|rượu hoa điêu", rep: true),
        K("cond-shrimp-paste", "Shrimp Paste", "Shrimp Paste (mam tom)", "Mắm tôm", Cd, 80, 13, 3, 1.5m, null, "1 tsp=5|1 tbsp=15|1 g=1", "shrimp paste|belacan|mắm tôm", rep: true),
        K("cond-mam-ruoc", "Mam ruoc (fermented shrimp paste)", "Mam ruoc (fermented shrimp paste)", "Mắm ruốc", Cd, 70, 12, 3, 1, null, "1 tsp=5|1 tbsp=15|1 g=1", "mắm ruốc huế|mam ruoc", rep: true),
        K("cond-mam-nem", "Mam nem (fermented anchovy sauce)", "Mam nem (fermented anchovy sauce)", "Mắm nêm", Cd, 90, 6, 12, 2, null, "1 tbsp=15|1 tsp=5|1 g=1", "mắm nêm pha|mam nem", rep: true),
        K("cond-tuong-ban", "Tuong ban (fermented bean paste)", "Tuong ban (fermented bean paste)", "Tương bần", Cd, 70, 3, 12, 1, null, "1 tbsp=17|1 tsp=5.7|1 g=1", "tuong ban|tương bần hưng yên", rep: true),
        K("cond-tuong-hot", "Tuong hot (fermented soybean sauce)", "Tuong hot (fermented soybean sauce)", "Tương hột", Cd, 100, 7, 10, 4, null, "1 tbsp=17|1 tsp=5.7|1 g=1", "tuong hot|tương hạt|fermented soybean paste", rep: true),
        K("cond-sa-te", "Sa te (chili lemongrass sauce)", "Sa te (chili lemongrass sauce)", "Sa tế", Cd, 400, 4, 15, 38, null, "1 tbsp=15|1 tsp=5|1 g=1", "sate sauce|sa te|satay chili|sa tế ớt", rep: true),

        // --- Gia vị khô và bột ---
        K("cond-pepper-white", "White Pepper", "White Pepper", "Tiêu trắng", Sp, 296, 10.4m, 68.6m, 2.1m, 5, "1 tsp=2.4|1 pinch=0.15|1 dash=0.3|1 g=1", "tiêu trắng|bột tiêu trắng", rep: true),
        K("cond-chili-flakes", "Chili Flakes", "Red Pepper Flakes (Chili Flakes)", "Ớt bột", Sp, 318, 12, 56.6m, 17.3m, 30, "1 tsp=1.8|1 pinch=0.1|1 dash=0.2|1 g=1", "red pepper flakes|crushed red pepper|ớt bột|ớt khô|ớt xay|bột ớt|cayenne|cayenne pepper", rep: true),
        K("cond-paprika", "Paprika", "Paprika", "Bột paprika", Sp, 282, 14.1m, 54, 12.9m, 68, "1 tsp=2.3|1 pinch=0.15|1 dash=0.3|1 g=1", "bột paprika|ớt paprika", rep: true),
        K("cond-cumin", "Cumin", "Cumin, Ground", "Bột cumin", Sp, 375, 17.8m, 44.2m, 22.3m, 168, "1 tsp=2.1|1 pinch=0.13|1 dash=0.26|1 g=1", "cumin|ground cumin|bột cumin|hạt cumin", rep: true),
        K("cond-curry-powder", "Curry Powder", "Curry Powder", "Bột cà ri", Sp, 325, 14.3m, 55.8m, 14, 52, "1 tsp=2|1 pinch=0.12|1 dash=0.25|1 g=1", "bột cà ri|bột cà-ri|cà ri", rep: true),
        K("cond-turmeric", "Turmeric", "Turmeric, Ground", "Nghệ", Sp, 312, 9.7m, 67.1m, 3.3m, 27, "1 tsp=3|1 pinch=0.19|1 dash=0.38|1 g=1", "turmeric|ground turmeric|turmeric powder|nghệ|bột nghệ|nghệ bột", rep: true),
        K("cond-cinnamon", "Cinnamon", "Cinnamon, Ground", "Quế", Sp, 247, 4, 80.6m, 1.2m, 10, "1 tsp=2.6|1 pinch=0.16|1 dash=0.33|1 g=1", "cinnamon|ground cinnamon|quế|bột quế", rep: true),
        K("cond-five-spice", "Five-Spice Powder", "Five-Spice Powder", "Ngũ vị hương", Sp, 330, 10, 60, 8, null, "1 tsp=2.2|1 pinch=0.14|1 g=1", "five spice|five-spice powder|chinese five spice|ngũ vị hương|bột ngũ vị hương", rep: true),
        K("cond-star-anise", "Star Anise", "Star Anise", "Hoa hồi", Sp, 337, 17.6m, 50, 15.9m, null, "1 tsp=2|1 pod=0.5|1 g=1", "star anise|hoa hồi|đại hồi", rep: true),
        K("cond-cloves", "Cloves", "Cloves, Ground", "Đinh hương", Sp, 274, 6, 65.5m, 13, 277, "1 tsp=2.1|1 pinch=0.13|1 g=1", "cloves|ground cloves|đinh hương", rep: true),
        K("cond-nutmeg", "Nutmeg", "Nutmeg, Ground", "Nhục đậu khấu", Sp, 525, 5.8m, 49.3m, 36.3m, 16, "1 tsp=2.2|1 pinch=0.14|1 g=1", "nutmeg|ground nutmeg|nhục đậu khấu", rep: true),
        K("cond-garlic-powder", "Garlic Powder", "Garlic Powder", "Bột tỏi", Sp, 331, 16.6m, 72.7m, 0.7m, 60, "1 tsp=2.8|1 pinch=0.18|1 dash=0.35|1 g=1", "bột tỏi", rep: true),
        K("cond-onion-powder", "Onion Powder", "Onion Powder", "Bột hành", Sp, 341, 10.4m, 79.1m, 1, 73, "1 tsp=2.4|1 pinch=0.15|1 dash=0.3|1 g=1", "bột hành", rep: true),
        K("cond-ginger-powder", "Ginger Powder", "Ginger, Ground", "Bột gừng", Sp, 335, 9, 71.6m, 4.2m, 27, "1 tsp=1.8|1 pinch=0.11|1 dash=0.22|1 g=1", "ground ginger|ginger powder|bột gừng", rep: true),
        K("cond-msg", "MSG", "Monosodium Glutamate (MSG)", "Bột ngọt", Sp, 0, 0, 0, 0, 12280, "1 tsp=4.8|1/4 tsp=1.2|1 pinch=0.3|1 g=1", "monosodium glutamate|bột ngọt|mì chính|msg", rep: true),
        K("cond-hat-nem", "Seasoning Granules", "Seasoning Granules", "Hạt nêm", Sp, 230, 12, 35, 4, null, "1 tsp=4|1 tbsp=12|1 g=1", "seasoning granules|chicken seasoning|bột nêm|hạt nêm thịt", rep: true),
        K("cond-bot-canh", "Bot canh (seasoned salt)", "Bot canh (seasoned salt)", "Bột canh", Sp, 35, 3, 4, 0.5m, null, "1 tsp=5|1 tbsp=15|1 g=1", "bột canh hải châu|bot canh", rep: true),
        K("cond-baking-soda", "Baking Soda", "Baking Soda", "Muối nở", Sp, 0, 0, 0, 0, 27360, "1 tsp=4.6|1/4 tsp=1.15|1 pinch=0.3|1 g=1", "baking soda|sodium bicarbonate|bicarbonate of soda|muối nở|bột baking soda", rep: true),
        K("cond-baking-powder", "Baking Powder", "Baking Powder", "Bột nở", Sp, 53, 0.1m, 27.7m, 0, 10600, "1 tsp=4.6|1/4 tsp=1.15|1 pinch=0.3|1 g=1", "baking powder|bột nở|bột nổi|bột nở baking powder", rep: true),
        K("cond-tapioca-starch", "Tapioca Starch", "Tapioca Starch", "Bột năng", Sp, 358, 0.2m, 88.7m, 0, 1, "1 tbsp=9|1 tsp=3|1 g=1", "tapioca starch|tapioca flour|bột năng|bột năng tapioca", rep: true),
        K("cond-cornstarch", "Cornstarch", "Cornstarch", "Bột bắp", Sp, 381, 0.3m, 91.3m, 0.1m, 9, "1 tbsp=8|1 tsp=2.7|1 g=1", "corn starch|cornflour|bột bắp|bột ngô", rep: true),

        // --- Rau thơm / củ gia vị (chỉ món chưa có ở seed cũ: tỏi, gừng, hành lá, ớt, sả, ngò đã có) ---
        K("cond-shallot", "Shallot", "Shallot", "Hành tím", Vg, 72, 2.5m, 16.8m, 0.1m, 12, "1 tbsp chopped=10|1 shallot=25|1 g=1", "hành tím|hành khô", rep: true),
        K("cond-basil", "Basil", "Basil, Fresh", "Húng quế", Vg, 23, 3.2m, 2.7m, 0.6m, 4, "1 tbsp=2.7|5 leaves=2.5|1 g=1", "basil|fresh basil|húng quế|rau húng quế", rep: true),
        K("cond-mint", "Mint", "Mint, Fresh", "Húng lủi", Vg, 70, 3.8m, 14.9m, 0.9m, 31, "1 tbsp=1.7|5 leaves=1|1 g=1", "mint|spearmint|húng lủi|rau húng lủi|bạc hà", rep: true),
    ];

    /// <summary>Dòng đã đối chiếu trực tiếp với API USDA FoodData Central (SR Legacy) lúc soạn: giá trị khớp, QualityStatus Verified.</summary>
    public static readonly IReadOnlySet<string> CondimentVerifiedKeys = new HashSet<string>
    {
        "cond-salt-table", "cond-salt-iodized", "cond-oil-vegetable", "cond-oil-olive", "cond-butter-salted", "cond-butter-unsalted",
        "cond-honey", "cond-sugar-granulated", "cond-ketchup",
    };

    /// <summary>
    /// Dòng khối lượng khẩu phần, hỗn hợp hoặc món Việt tự ước/không chắc nguồn: vẫn Unverified, cần đối chiếu nhãn/USDA trước khi tin.
    /// Hỗn hợp muối (tỏi, cần tây, hành, gia vị, muối ớt/tiêu chanh/tôm, hạt nêm, bột canh) lấy theo tỉ lệ pha ước tính; sodium để Unknown.
    /// Muối biển/kosher/hạt dùng sodium của muối ăn (≈ NaCl); Lite Salt và MSG dùng sodium tính theo thành phần hoá học (Unverified).
    /// </summary>
    public static readonly string[] CondimentUncertainKeys =
    [
        "cond-salt-sea", "cond-salt-kosher", "cond-salt-himalayan", "cond-salt-sea-fine", "cond-salt-flaky", "cond-salt-black", "cond-salt-garlic", "cond-salt-celery",
        "cond-salt-onion", "cond-salt-seasoned", "cond-salt-lite", "cond-salt-chili", "cond-salt-lime-pepper", "cond-salt-shrimp", "cond-salt-coarse",
        "cond-fat-chicken", "cond-sugar-palm", "cond-sugar-rock", "cond-corn-syrup", "cond-cane-syrup", "cond-caramel-color", "cond-stevia", "cond-sucralose",
        "cond-sweet-chili", "cond-bbq", "cond-ranch", "cond-mirin", "cond-cooking-wine", "cond-shrimp-paste", "cond-mam-ruoc", "cond-mam-nem", "cond-tuong-ban",
        "cond-tuong-hot", "cond-sa-te", "cond-vinegar-rice", "cond-five-spice", "cond-star-anise", "cond-msg", "cond-hat-nem", "cond-bot-canh", "cond-shallot",
        "cond-basil", "cond-mint",
    ];
}
