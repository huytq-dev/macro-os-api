namespace Infrastructure;

public static partial class CanonicalCatalogData
{
    public const string VnProvider = "vn-fct";

    /// <summary>
    /// Bộ nhỏ món/nguyên liệu Việt phổ biến, macro /100 g làm tròn theo bảng thành phần thực phẩm Việt Nam (NIN) và USDA SR Legacy, GHI TỪ TRÍ NHỚ chưa đối chiếu lại bản gốc
    /// nên mọi dòng QualityStatus = Unverified, IsVerified = false. Các key trong UncertainKeys là số tự ước (không có nguồn chắc): cần đối chiếu trước khi tin.
    /// Cột: key, tên Việt, tên Anh chuẩn, nhóm, kcal, P, C, F, tên khẩu phần, gram khẩu phần, alias cách nhau bằng dấu gạch đứng.
    /// </summary>
    public static readonly (string Key, string Vi, string En, FoodCategory Cat, decimal Kcal, decimal P, decimal C, decimal F, string Serving, decimal Grams, string Aliases)[] VietnameseFoods =
    [
        ("vn-fish-sauce", "Nước mắm", "Fish sauce", FoodCategory.Other, 35, 5.1m, 3.6m, 0, "1 muỗng canh", 18, "nước mắm|nuoc mam|nước mắm nhĩ|fish sauce"),
        ("vn-soy-sauce", "Nước tương", "Soy sauce", FoodCategory.Other, 53, 8.1m, 4.9m, 0.6m, "1 muỗng canh", 16, "nước tương|xì dầu|tương|soy sauce|soya sauce"),
        ("vn-oyster-sauce", "Dầu hào", "Oyster sauce", FoodCategory.Other, 51, 1.4m, 10.9m, 0.3m, "1 muỗng canh", 16, "dầu hào|oyster sauce"),
        ("vn-chili-sauce", "Tương ớt", "Chili sauce", FoodCategory.Other, 93, 1.9m, 19.2m, 0.9m, "1 muỗng canh", 15, "tương ớt|chili sauce|sriracha|sốt ớt"),
        ("vn-mayonnaise", "Sốt mayonnaise", "Mayonnaise", FoodCategory.Fats, 680, 1, 0.6m, 75, "1 muỗng canh", 14, "mayonnaise|mayo|sốt mayonnaise|xốt mayonnaise"),
        ("vn-sugar", "Đường", "Sugar", FoodCategory.Other, 387, 0, 100, 0, "1 muỗng cà phê", 4, "đường|đường cát|đường trắng|sugar|white sugar"),
        ("vn-salt", "Muối", "Salt", FoodCategory.Other, 0, 0, 0, 0, "1 muỗng cà phê", 6, "muối|muối ăn|salt|table salt"),
        ("vn-pepper", "Tiêu", "Black pepper", FoodCategory.Other, 251, 10.4m, 64, 3.3m, "1 muỗng cà phê", 2, "tiêu|hạt tiêu|bột tiêu|black pepper|pepper"),
        ("vn-lard", "Mỡ heo", "Lard", FoodCategory.Fats, 902, 0, 0, 100, "1 muỗng canh", 13, "mỡ heo|mỡ lợn|mỡ|lard"),
        ("vn-coconut-milk", "Nước cốt dừa", "Coconut milk", FoodCategory.Fats, 230, 2.3m, 5.5m, 23.8m, "1/4 chén", 60, "nước cốt dừa|nước cốt|coconut milk|coconut cream"),
        ("vn-peanut", "Đậu phộng", "Peanut", FoodCategory.Fats, 567, 25.8m, 16.1m, 49.2m, "1 nắm", 30, "đậu phộng|lạc|đậu phộng rang|peanut|peanuts|groundnut"),
        ("vn-garlic", "Tỏi", "Garlic", FoodCategory.Vegetables, 149, 6.4m, 33.1m, 0.5m, "1 tép", 3, "tỏi|tép tỏi|garlic|garlic clove"),
        ("vn-onion", "Hành tây", "Onion", FoodCategory.Vegetables, 40, 1.1m, 9.3m, 0.1m, "1 củ vừa", 110, "hành tây|hành củ|onion|yellow onion"),
        ("vn-scallion", "Hành lá", "Scallion", FoodCategory.Vegetables, 32, 1.8m, 7.3m, 0.2m, "1 cọng", 15, "hành lá|hành xanh|scallion|green onion|spring onion"),
        ("vn-ginger", "Gừng", "Ginger", FoodCategory.Vegetables, 80, 1.8m, 17.8m, 0.8m, "1 lát", 6, "gừng|ginger"),
        ("vn-chili", "Ớt", "Chili pepper", FoodCategory.Vegetables, 40, 1.9m, 8.8m, 0.4m, "1 quả", 10, "ớt|ớt tươi|ớt hiểm|chili|chilli|chili pepper"),
        ("vn-lemongrass", "Sả", "Lemongrass", FoodCategory.Vegetables, 99, 1.8m, 25.3m, 0.5m, "1 cây", 30, "sả|cây sả|lemongrass|lemon grass"),
        ("vn-cilantro", "Ngò rí", "Cilantro", FoodCategory.Vegetables, 23, 2.1m, 3.7m, 0.5m, "1 nhánh", 3, "ngò rí|ngò|rau mùi|cilantro|coriander"),
        ("vn-choy-sum", "Cải ngọt", "Choy sum", FoodCategory.Vegetables, 13, 1.5m, 2.2m, 0.2m, "1 chén", 70, "cải ngọt|choy sum"),
        ("vn-napa", "Cải thảo", "Napa cabbage", FoodCategory.Vegetables, 16, 1.2m, 3.2m, 0.2m, "1 chén", 75, "cải thảo|napa cabbage|chinese cabbage"),
        ("vn-malabar", "Mồng tơi", "Malabar spinach", FoodCategory.Vegetables, 19, 1.8m, 3.4m, 0.3m, "1 chén", 60, "mồng tơi|rau mồng tơi|malabar spinach|ceylon spinach"),
        ("vn-bean-sprouts", "Giá đỗ", "Bean sprouts", FoodCategory.Vegetables, 30, 3, 5.9m, 0.2m, "1 chén", 90, "giá đỗ|giá|giá đậu|bean sprouts|mung bean sprouts"),
        ("vn-green-beans", "Đậu que", "Green beans", FoodCategory.Vegetables, 31, 1.8m, 7, 0.2m, "1 chén", 100, "đậu que|đậu cô ve|green beans|string beans"),
        ("vn-winter-melon", "Bí đao", "Winter melon", FoodCategory.Vegetables, 13, 0.4m, 3, 0.2m, "1 chén", 100, "bí đao|winter melon|wax gourd"),
        ("vn-bitter-melon", "Khổ qua", "Bitter melon", FoodCategory.Vegetables, 17, 1, 3.7m, 0.2m, "1/2 trái", 100, "khổ qua|mướp đắng|bitter melon|bitter gourd"),
        ("vn-eggplant", "Cà tím", "Eggplant", FoodCategory.Vegetables, 25, 1, 5.9m, 0.2m, "1 trái", 200, "cà tím|eggplant|aubergine"),
        ("vn-mushroom", "Nấm", "Mushroom", FoodCategory.Vegetables, 22, 3.1m, 3.3m, 0.3m, "1 chén", 70, "nấm|nấm tươi|nấm rơm|mushroom|mushrooms|button mushroom"),
        ("vn-pomelo", "Bưởi", "Pomelo", FoodCategory.Fruits, 38, 0.8m, 9.6m, 0, "1 chén múi", 190, "bưởi|pomelo"),
        ("vn-guava", "Ổi", "Guava", FoodCategory.Fruits, 68, 2.6m, 14.3m, 1, "1 trái", 100, "ổi|guava"),
        ("vn-longan", "Nhãn", "Longan", FoodCategory.Fruits, 60, 1.3m, 15.1m, 0.1m, "1 chùm nhỏ", 100, "nhãn|longan"),
        ("vn-lychee", "Vải", "Lychee", FoodCategory.Fruits, 66, 0.8m, 16.5m, 0.4m, "5 trái", 50, "vải|lychee|litchi"),
        ("vn-rambutan", "Chôm chôm", "Rambutan", FoodCategory.Fruits, 82, 0.7m, 20.9m, 0.2m, "5 trái", 60, "chôm chôm|rambutan"),
        ("vn-durian", "Sầu riêng", "Durian", FoodCategory.Fruits, 147, 1.5m, 27.1m, 5.3m, "1 múi", 40, "sầu riêng|durian"),
        ("vn-jackfruit", "Mít", "Jackfruit", FoodCategory.Fruits, 95, 1.7m, 23.3m, 0.6m, "1 múi", 20, "mít|jackfruit"),
        ("vn-lime", "Chanh", "Lime", FoodCategory.Fruits, 30, 0.7m, 10.5m, 0.2m, "1 quả", 44, "chanh|chanh xanh|lime"),
        ("vn-pork-belly", "Thịt ba chỉ", "Pork belly", FoodCategory.Protein, 518, 9.3m, 0, 53, "1 miếng", 50, "thịt ba chỉ|ba chỉ|thịt ba rọi|ba rọi|pork belly|streaky pork"),
        ("vn-duck-egg", "Trứng vịt", "Duck egg", FoodCategory.Protein, 185, 12.8m, 1.5m, 13.8m, "1 quả", 70, "trứng vịt|duck egg"),
        ("vn-quail-egg", "Trứng cút", "Quail egg", FoodCategory.Protein, 158, 13, 0.4m, 11.1m, "1 quả", 9, "trứng cút|quail egg"),
        ("vn-basa", "Cá basa", "Basa fish", FoodCategory.Protein, 90, 15.5m, 0, 3, "1 miếng", 120, "cá basa|cá tra|basa|pangasius|swai"),
        ("vn-shrimp-raw", "Tôm sống", "Shrimp, raw", FoodCategory.Protein, 85, 20.1m, 0, 0.5m, "1 con vừa", 15, "tôm sống|tôm tươi|raw shrimp"),
        ("vn-squid", "Mực", "Squid", FoodCategory.Protein, 92, 15.6m, 3.1m, 1.4m, "1 con", 150, "mực|mực ống|mực tươi|squid|calamari"),
        ("vn-beef-egg-stirfry", "Bò xào trứng", "Stir-fried beef with egg", FoodCategory.Protein, 180, 14, 3, 12, "1 đĩa", 200, "bò xào trứng|beef and egg stir fry|beef stir fry with egg"),
        ("vn-condensed-milk", "Sữa đặc", "Sweetened condensed milk", FoodCategory.Dairy, 321, 7.9m, 54.4m, 8.7m, "1 muỗng canh", 20, "sữa đặc|sữa đặc có đường|condensed milk|sweetened condensed milk"),
        ("vn-soy-milk", "Sữa đậu nành", "Soy milk", FoodCategory.Beverages, 33, 2.9m, 1.7m, 1.6m, "1 ly", 240, "sữa đậu nành|sữa đậu|soy milk|soymilk"),
        ("vn-coconut-water", "Nước dừa", "Coconut water", FoodCategory.Beverages, 19, 0.7m, 3.7m, 0.2m, "1 trái", 300, "nước dừa|dừa tươi|coconut water"),
        ("vn-beer", "Bia", "Beer", FoodCategory.Beverages, 43, 0.5m, 3.6m, 0, "1 lon", 330, "bia|beer"),
        ("vn-rice-porridge", "Cháo trắng", "Rice porridge", FoodCategory.Grains, 46, 0.9m, 9.6m, 0.4m, "1 tô", 300, "cháo|cháo trắng|rice porridge|congee"),
        ("vn-rice-paper", "Bánh tráng", "Rice paper", FoodCategory.Grains, 333, 0.7m, 82, 0.3m, "1 tờ", 10, "bánh tráng|bánh đa nem|rice paper|spring roll wrapper"),
        ("vn-glass-noodles", "Miến", "Glass noodles", FoodCategory.Grains, 84, 0.1m, 20.7m, 0, "1 chén", 150, "miến|miến dong|glass noodles|cellophane noodles"),
    ];

    /// <summary>Dòng số liệu tự ước hoặc không chắc nguồn (xem báo cáo): vẫn Unverified như cả bộ, ghi riêng để người rà soát biết.</summary>
    public static readonly string[] UncertainKeys = ["vn-beef-egg-stirfry", "vn-basa", "vn-chili-sauce", "vn-rice-porridge", "vn-rice-paper", "vn-glass-noodles"];
}
