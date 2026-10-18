using System.Globalization;
using System.Text;

namespace Application;

/// <summary>
/// Tự gán khoá icon (bộ food-icons/v1) cho món theo tên, không dùng AI.
/// Khớp theo TỪ (không khớp chuỗi con): một cụm khớp khi mọi từ của cụm đều có trong tên, thứ tự bất kỳ (tên USDA "Oil, olive" khớp cụm "olive oil").
/// Thắng: cụm nhiều từ hơn, hoà thì cụm xuất hiện sớm hơn trong tên, hoà nữa thì cụm đứng trước trong bảng. Không khớp thì fallback theo nhóm món (chỉ vài nhóm chắc chắn), không thì null (FE dùng emoji).
/// Bảng "Plain" so với tên đã bỏ dấu, số ít; bảng "Strict" so với tên còn dấu để tách các từ mà bỏ dấu sẽ nhầm (bò/bơ, ngô/ngò, nho/nhỏ, táo/tạo).
/// </summary>
public static class IconKeyResolver
{
    private sealed record Entry(string Phrase, string Key, string[] Tokens, bool Strict);

    // Cụm (đã bỏ dấu, chữ thường) → khoá. Bộ icon không có bread, tofu, oats, noodle chung nên các món đó để null.
    private static readonly (string Phrase, string Key)[] Plain =
    [
        // Thịt
        ("ground beef", "ground_beef"), ("hamburger", "ground_beef"), ("beef rib", "beef_ribs"), ("beef steak", "steak_beef"), ("steak", "steak_beef"), ("beef", "beef"),
        ("ground pork", "ground_pork"), ("pork rib", "pork_ribs"), ("suon", "pork_ribs"), ("pork", "pork"), ("heo", "pork"), ("ba chi", "pork"),
        ("bacon", "bacon"), ("ham", "ham"), ("sausage", "sausage"), ("salami", "salami"), ("chorizo", "chorizo"), ("prosciutto", "prosciutto"), ("pancetta", "pancetta"),
        ("ground lamb", "ground_lamb"), ("lamb", "lamb"), ("goat", "goat_meat"), ("rabbit", "rabbit"), ("venison", "venison"), ("veal", "veal"),
        ("chicken", "chicken"), ("duck", "duck"), ("turkey", "turkey"), ("goose", "goose"),
        // Hải sản
        ("sea bass", "sea_bass"), ("salmon", "salmon"), ("ca hoi", "salmon"), ("tuna", "tuna"), ("ca ngu", "tuna"), ("cod", "cod"),
        ("shrimp", "shrimp"), ("tom", "shrimp"), ("prawn", "prawns"), ("crab", "crab"), ("cua", "crab"), ("lobster", "lobster"),
        ("squid", "squid"), ("calamari", "squid"), ("muc", "squid"), ("octopus", "octopus"), ("oyster", "oysters"), ("clam", "clams"), ("mussel", "mussels"), ("scallop", "scallops"),
        ("anchovy", "anchovy"), ("sardine", "sardine"), ("trout", "trout"), ("mackerel", "mackerel"), ("tilapia", "tilapia"), ("halibut", "halibut"), ("haddock", "haddock"),
        ("eel", "eel"), ("herring", "herring"), ("catfish", "catfish"), ("basa", "catfish"), ("ca basa", "catfish"), ("ca tra", "catfish"),
        ("flounder", "flounder"), ("swordfish", "swordfish"), ("yellowtail", "yellowtail"), ("carp", "carp"), ("rockfish", "rockfish"), ("monkfish", "monkfish"),
        // Sữa, trứng
        ("soy milk", "soy_milk"), ("sua dau nanh", "soy_milk"), ("coconut milk", "coconut_milk"), ("nuoc cot dua", "coconut_milk"), ("buttermilk", "buttermilk"),
        ("milk", "milk"), ("sua dac", "milk"), ("sua tuoi", "milk"), ("sua", "milk"),
        ("heavy cream", "heavy_cream"), ("sour cream", "sour_cream"), ("cream cheese", "cream_cheese"), ("cream", "cream"),
        ("greek yogurt", "greek_yogurt"), ("yogurt", "yogurt"), ("yoghurt", "yogurt"), ("sua chua", "yogurt"), ("kefir", "kefir"),
        ("egg yolk", "egg_yolk"), ("egg white", "egg_white"), ("egg", "eggs"), ("trung", "eggs"),
        ("ghee", "ghee"), ("peanut butter", "peanuts"), ("butter", "butter"),
        ("cheddar", "cheddar"), ("swiss cheese", "swiss_cheese"), ("mozzarella", "mozzarella"), ("parmesan", "parmesan"), ("brie", "brie"), ("cottage cheese", "cottage_cheese"),
        ("feta", "feta"), ("goat cheese", "goat_cheese"), ("ricotta", "ricotta"), ("gouda", "gouda"), ("blue cheese", "blue_cheese"), ("camembert", "camembert"),
        ("burrata", "burrata"), ("provolone", "provolone"), ("paneer", "paneer_indian"), ("pho mai", "cheddar"), ("cheese", "cheddar"),
        // Ngũ cốc, đậu
        ("jasmine rice", "jasmine_rice"), ("basmati rice", "basmati_rice"), ("brown rice", "brown_rice"), ("gao lut", "brown_rice"), ("wild rice", "wild_rice"), ("arborio rice", "arborio_rice"),
        ("rice noodle", "rice_noodles"), ("glass noodle", "rice_noodles"), ("banh pho", "rice_noodles"), ("rice paper", "rice"), ("banh trang", "rice"), ("rice porridge", "rice"),
        ("rice", "rice"), ("com", "rice"), ("gao", "rice"), ("xoi", "rice"),
        ("ramen", "ramen_noodles"), ("soba", "soba"), ("udon", "udon"), ("pasta", "pasta"), ("spaghetti", "pasta"), ("macaroni", "pasta"), ("penne", "pasta"), ("fettuccine", "pasta"), ("lasagna", "lasagna_sheet"),
        ("couscous", "couscous"), ("quinoa", "quinoa"), ("bulgur", "bulgur"), ("barley", "barley"), ("buckwheat", "buckwheat"), ("farro", "farro"), ("millet", "millet"), ("polenta", "polenta"), ("cornmeal", "cornmeal"),
        ("bread flour", "bread_flour"), ("whole wheat flour", "whole_wheat_flour"), ("almond flour", "almond_flour"), ("flour", "flour"), ("amaranth", "amaranth"),
        ("chickpea", "chickpeas"), ("garbanzo", "chickpeas"), ("lentil", "lentils"), ("black bean", "black_beans"), ("kidney bean", "kidney_beans"), ("pinto bean", "pinto_beans"),
        ("mung bean", "mung_beans"), ("gia do", "mung_beans"), ("adzuki", "adzuki_beans"), ("edamame", "edamame"), ("fava bean", "fava_beans"), ("broad bean", "fava_beans"),
        // Hạt
        ("almond", "almonds"), ("hanh nhan", "almonds"), ("cashew", "cashews"), ("hat dieu", "cashews"), ("walnut", "walnuts"), ("peanut", "peanuts"), ("dau phong", "peanuts"),
        ("pecan", "pecans"), ("pistachio", "pistachios"), ("hazelnut", "hazelnuts"), ("macadamia", "macadamia"), ("brazil nut", "brazil_nuts"), ("pine nut", "pine_nuts"),
        ("chia", "chia_seeds"), ("flax", "flax_seeds"), ("flaxseed", "flax_seeds"), ("sesame", "sesame_seeds"), ("pumpkin seed", "pumpkin_seeds"), ("hemp seed", "hemp_seeds"),
        ("poppy", "poppy_seed"), ("sunflower oil", "sunflower_oil"),
        // Rau củ
        ("avocado", "avocado"), ("broccoli", "broccoli"), ("bong cai", "broccoli"), ("carrot", "carrot"), ("ca rot", "carrot"),
        ("cherry tomato", "cherry_tomato"), ("tomato juice", "tomato_juice"), ("tomato", "tomato"), ("ca chua", "tomato"),
        ("bell pepper", "bell_pepper"), ("red pepper", "bell_pepper"), ("green pepper", "bell_pepper"), ("sweet pepper", "bell_pepper"),
        ("chili sauce", "hot_sauce"), ("tuong ot", "hot_sauce"), ("chili powder", "chili_powder"), ("chili pepper", "chili_pepper"), ("chili", "chili_pepper"), ("chilli", "chili_pepper"),
        ("jalapeno", "jalapeno"), ("habanero", "habanero"), ("poblano", "poblano"), ("cucumber", "cucumber"), ("dua leo", "cucumber"), ("zucchini", "zucchini"),
        ("yellow squash", "yellow_squash"), ("butternut", "butternut_squash"), ("pumpkin", "pumpkin"), ("acorn squash", "acorn_squash"),
        ("red cabbage", "red_cabbage"), ("napa", "napa_cabbage"), ("cai thao", "napa_cabbage"), ("bok choy", "bok_choy"), ("pak choi", "bok_choy"), ("choy sum", "bok_choy"),
        ("cai ngot", "bok_choy"), ("cai xanh", "bok_choy"), ("cabbage", "cabbage"), ("bap cai", "cabbage"), ("kale", "kale"),
        ("water spinach", "spinach"), ("malabar spinach", "spinach"), ("spinach", "spinach"), ("rau muong", "spinach"), ("cai bo xoi", "spinach"),
        ("lettuce", "lettuce"), ("arugula", "arugula"), ("collard", "collard_greens"), ("endive", "endive"), ("radicchio", "radicchio"), ("asparagus", "asparagus"),
        ("artichoke", "artichoke"), ("cauliflower", "cauliflower"), ("brussels sprout", "brussels_sprouts"), ("celeriac", "celeriac"), ("celery", "celery"),
        ("beet", "beet"), ("radish", "radish"), ("daikon", "daikon"), ("turnip", "turnip"), ("parsnip", "parsnip"),
        ("sweet potato", "sweet_potato"), ("khoai lang", "sweet_potato"), ("potato", "potato"), ("khoai tay", "potato"),
        ("red onion", "red_onion"), ("green onion", "scallion"), ("spring onion", "scallion"), ("scallion", "scallion"), ("hanh la", "scallion"), ("hanh tay", "onion"), ("onion", "onion"), ("hanh", "onion"),
        ("leek", "leek"), ("garlic", "garlic"), ("shiitake", "shiitake"), ("oyster mushroom", "oyster_mushroom"), ("enoki", "enoki"), ("morel", "morel"), ("mushroom", "mushroom"),
        ("baby corn", "baby_corn"), ("corn", "corn"), ("eggplant", "eggplant"), ("aubergine", "eggplant"), ("ca tim", "eggplant"),
        ("green bean", "green_beans"), ("dau que", "green_beans"), ("snap pea", "snap_peas"), ("snow pea", "snow_peas"), ("okra", "okra"), ("fennel", "fennel"), ("kohlrabi", "kohlrabi"),
        ("cassava", "cassava"), ("water chestnut", "water_chestnuts"), ("bamboo shoot", "bamboo_shoots"), ("jackfruit", "jackfruit"), ("nopales", "nopales"),
        ("ginger", "ginger"), ("gung", "ginger"), ("turmeric", "turmeric_root"),
        // Rau thơm, gia vị, nước chấm
        ("basil", "basil"), ("mint", "mint"), ("parsley", "parsley"), ("cilantro", "cilantro"), ("coriander", "cilantro"), ("dill", "dill"), ("rosemary", "rosemary"), ("thyme", "thyme"),
        ("oregano", "oregano"), ("sage", "sage"), ("tarragon", "tarragon"), ("bay leaf", "bay_leaf"), ("lemongrass", "lemongrass"), ("chive", "chives"), ("marjoram", "marjoram"),
        ("shiso", "shiso"), ("kaffir lime", "kaffir_lime_leaf"), ("curry leaf", "curry_leaf"), ("black pepper", "black_pepper"), ("pepper", "black_pepper"), ("tieu", "black_pepper"),
        ("paprika", "paprika"), ("cinnamon", "cinnamon"), ("clove", "cloves_spice"), ("star anise", "star_anise"), ("saffron", "saffron"), ("vanilla", "vanilla_bean"),
        ("cardamom", "cardamom"), ("cumin", "cumin_seed"), ("mustard", "mustard"), ("salt", "salt"), ("muoi", "salt"),
        ("honey", "honey"), ("mat ong", "honey"), ("maple syrup", "maple_syrup"), ("brown sugar", "brown_sugar"), ("sugar", "sugar"), ("duong", "sugar"),
        ("chocolate chip", "chocolate_chips"), ("ketchup", "ketchup"), ("mayonnaise", "mayonnaise"), ("mayo", "mayonnaise"),
        ("olive oil", "olive_oil"), ("dau an", "olive_oil"), ("oil", "olive_oil"),
        ("soy sauce", "soy_sauce"), ("oyster sauce", "soy_sauce"), ("nuoc tuong", "soy_sauce"), ("dau hao", "soy_sauce"), ("fish sauce", "fish_sauce"), ("nuoc mam", "fish_sauce"),
        ("hot sauce", "hot_sauce"), ("bbq sauce", "bbq_sauce"), ("barbecue sauce", "bbq_sauce"), ("sriracha", "sriracha"), ("pesto", "pesto"),
        ("tamari", "soy_sauce"), ("hoisin", "soy_sauce"), ("worcestershire", "soy_sauce"), ("teriyaki", "soy_sauce"), ("tuong ban", "soy_sauce"), ("tuong hot", "soy_sauce"),
        ("mam ruoc", "fish_sauce"), ("mam nem", "fish_sauce"), ("sa te", "hot_sauce"), ("tuong ca", "ketchup"), ("margarine", "butter"),
        ("molasses", "maple_syrup"), ("cane syrup", "maple_syrup"), ("caramel syrup", "maple_syrup"), ("nuoc hang", "maple_syrup"), ("mat mia", "maple_syrup"),
        ("mirin", "sake"), ("rice wine", "sake"), ("cooking wine", "wine_white"), ("five spice", "star_anise"),
        ("monosodium glutamate", "salt"), ("msg", "salt"), ("bot ngot", "salt"), ("hat nem", "salt"), ("bot canh", "salt"),
        ("cornstarch", "flour"), ("corn starch", "flour"), ("tapioca starch", "flour"), ("bot nang", "flour"),
        // Đồ uống
        ("coffee", "coffee"), ("ca phe", "coffee"), ("espresso", "espresso"), ("milk tea", "tea"), ("bubble tea", "tea"), ("tra sua", "tea"), ("tea", "tea"), ("water", "water"),
        ("red wine", "wine_red"), ("white wine", "wine_white"), ("wine", "wine_red"), ("beer", "beer"), ("bia", "beer"), ("sake", "sake"),
        ("cocoa", "cocoa_powder"), ("cacao", "cocoa_powder"), ("chocolate", "cocoa_powder"), ("socola", "cocoa_powder"),
        // Trái cây
        ("apple", "apple"), ("banana", "banana"), ("chuoi", "banana"), ("strawberry", "strawberry"), ("dau tay", "strawberry"),
        ("orange", "orange"), ("nuoc cam", "orange"), ("cam", "orange"), ("lemon", "lemon"), ("lime", "lime"), ("chanh", "lime"), ("grapefruit", "grapefruit"), ("pomelo", "grapefruit"), ("buoi", "grapefruit"),
        ("grape", "grape"), ("cherry", "cherry"), ("blueberry", "blueberry"), ("blackberry", "blackberry"), ("raspberry", "raspberry"), ("cranberry", "cranberry"),
        ("currant", "currant"), ("elderberry", "elderberry"), ("gooseberry", "gooseberry"), ("peach", "peach"), ("apricot", "apricot"), ("plum", "plum"), ("pear", "pear"),
        ("pineapple", "pineapple"), ("mango", "mango"), ("xoai", "mango"), ("papaya", "papaya"), ("du du", "papaya"), ("kiwi", "kiwi"), ("cantaloupe", "cantaloupe"), ("honeydew", "honeydew"),
        ("watermelon", "watermelon"), ("dua hau", "watermelon"), ("dragon fruit", "dragon_fruit"), ("thanh long", "dragon_fruit"), ("lychee", "lychee"), ("longan", "lychee"),
        ("passion fruit", "passion_fruit"), ("chanh day", "passion_fruit"), ("guava", "guava"), ("coconut water", "coconut"), ("nuoc dua", "coconut"), ("coconut", "coconut"),
        ("fig", "fig"), ("date", "date"), ("persimmon", "persimmon"), ("pomegranate", "pomegranate"), ("star fruit", "star_fruit"), ("yuzu", "yuzu"), ("calamansi", "calamansi"),
        ("durian", "durian"), ("sau rieng", "durian"), ("mangosteen", "mangosteen"), ("mang cut", "mangosteen"), ("rambutan", "rambutan"), ("chom chom", "rambutan"),
    ];

    // Cụm còn dấu (chữ thường): các từ mà bỏ dấu sẽ nhầm sang nghĩa khác hoặc từ thường dùng.
    private static readonly (string Phrase, string Key)[] Strict =
    [
        ("bò", "beef"), ("bơ", "butter"), ("gà", "chicken"), ("vịt", "duck"), ("tỏi", "garlic"),
        ("ngô", "corn"), ("bắp", "corn"), ("ngò", "cilantro"), ("sả", "lemongrass"), ("ớt", "chili_pepper"), ("nấm", "mushroom"),
        ("trà", "tea"), ("cháo", "rice"), ("bún", "rice_noodles"), ("phở", "rice_noodles"), ("miến", "rice_noodles"),
        ("dừa", "coconut"), ("dứa", "pineapple"), ("mít", "jackfruit"), ("nhãn", "lychee"), ("vải", "lychee"), ("ổi", "guava"), ("nho", "grape"), ("táo", "apple"), ("lê", "pear"),
    ];

    // Fallback khi không khớp cụm nào: chỉ nhóm mà icon đại diện không sai về loại. Protein/Grains/Fats/Beverages/Snacks/FastFood/Supplements/Other → null.
    private static readonly Dictionary<FoodCategory, string> CategoryFallback = new()
    {
        [FoodCategory.Fruits] = "apple",
        [FoodCategory.Vegetables] = "cabbage",
        [FoodCategory.Dairy] = "milk",
    };

    private static readonly Entry[] Entries = Plain.Select(e => Build(e, strict: false)).Concat(Strict.Select(e => Build(e, strict: true))).ToArray();

    /// <summary>Mọi khoá mà bảng có thể trả ra (self-check đối chiếu với <see cref="FoodIconKeys"/>).</summary>
    public static IEnumerable<string> AllKeys => Entries.Select(e => e.Key).Concat(CategoryFallback.Values).Append("avocado").Distinct();

    /// <returns>Khoá icon hoặc null nếu không chắc (tên trống, không khớp cụm nào và nhóm món không có icon đại diện).</returns>
    public static string? Resolve(string? name, FoodCategory? category = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var plain = PlainTokens(name);
        var strict = StrictTokens(name);

        Entry? best = null;
        var bestPosition = int.MaxValue;
        foreach (var e in Entries)
        {
            var tokens = e.Strict ? strict : plain;
            var position = int.MaxValue;
            var all = true;
            foreach (var t in e.Tokens)
            {
                var i = Array.IndexOf(tokens, t);
                if (i < 0) { all = false; break; }
                position = Math.Min(position, i);
            }
            if (!all) continue;
            if (best is null || e.Tokens.Length > best.Tokens.Length || (e.Tokens.Length == best.Tokens.Length && position < bestPosition))
            {
                best = e;
                bestPosition = position;
            }
        }

        // "Bơ" là bơ sữa ở nhóm khác, nhưng là trái bơ khi món thuộc nhóm trái cây
        if (best is { Phrase: "bơ" } && category == FoodCategory.Fruits) return "avocado";
        if (best is not null) return best.Key;
        return category is { } c && CategoryFallback.TryGetValue(c, out var fallback) ? fallback : null;
    }

    private static Entry Build((string Phrase, string Key) e, bool strict)
        => new(e.Phrase, e.Key, strict ? StrictTokens(e.Phrase) : PlainTokens(e.Phrase), strict);

    // Bỏ dấu, chữ thường, số ít từng từ ("eggs" → "egg")
    private static string[] PlainTokens(string s)
        => FoodNameNormalizer.Normalize(s).Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Singular).ToArray();

    // ponytail: quy tắc số ít cho tên tiếng Anh (cherries → cherry, tomatoes → tomato, peaches → peach, eggs → egg); từ bất quy tắc (leaves) để bảng khai báo cả hai dạng
    private static string Singular(string t)
    {
        if (t.Length > 4 && t.EndsWith("ies")) return t[..^3] + "y";
        if (t.Length > 4 && (t.EndsWith("oes") || t.EndsWith("ches") || t.EndsWith("shes") || t.EndsWith("xes") || t.EndsWith("sses"))) return t[..^2];
        return t.Length > 3 && t.EndsWith('s') && !t.EndsWith("ss") ? t[..^1] : t;
    }

    // Giữ dấu (NFC), chữ thường, tách theo ký tự không phải chữ/số
    private static string[] StrictTokens(string s)
    {
        var sb = new StringBuilder();
        var tokens = new List<string>();
        foreach (var c in s.ToLowerInvariant().Normalize(NormalizationForm.FormC))
        {
            if (char.IsLetterOrDigit(c) || CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) sb.Append(c);
            else if (sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
        }
        if (sb.Length > 0) tokens.Add(sb.ToString());
        return tokens.ToArray();
    }
}
