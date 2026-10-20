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
    private sealed record Entry(string Phrase, string Key, string[] Tokens, bool Strict, bool HeadOnly = false);

    // Cụm (đã bỏ dấu, chữ thường) → khoá. Bộ icon mở rộng (529 khoá) đã có bread, tofu, oats, noodle, pizza… nên các món đó có icon riêng.
    private static readonly (string Phrase, string Key)[] Plain =
    [
        // Thịt
        ("ground beef", "ground_beef"), ("hamburger", "hamburger"), ("beef rib", "beef_ribs"), ("beef steak", "steak_beef"), ("steak", "steak_beef"), ("beef", "beef"),
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
        ("ghee", "ghee"), ("peanut butter", "peanut_butter"), ("bo dau phong", "peanut_butter"), ("butter", "butter"),
        ("cheddar", "cheddar"), ("swiss cheese", "swiss_cheese"), ("mozzarella", "mozzarella"), ("parmesan", "parmesan"), ("brie", "brie"), ("cottage cheese", "cottage_cheese"),
        ("feta", "feta"), ("goat cheese", "goat_cheese"), ("ricotta", "ricotta"), ("gouda", "gouda"), ("blue cheese", "blue_cheese"), ("camembert", "camembert"),
        ("burrata", "burrata"), ("provolone", "provolone"), ("paneer", "paneer_indian"), ("pho mai", "cheddar"), ("cheese", "cheddar"),
        // Ngũ cốc, đậu
        ("jasmine rice", "jasmine_rice"), ("basmati rice", "basmati_rice"), ("brown rice", "brown_rice"), ("gao lut", "brown_rice"), ("wild rice", "wild_rice"), ("arborio rice", "arborio_rice"),
        ("rice noodle", "rice_noodles"), ("glass noodle", "rice_noodles"), ("banh pho", "rice_noodles"), ("rice paper", "rice"), ("banh trang", "rice"), ("rice porridge", "congee"),
        ("rice", "rice"), ("com", "rice"), ("gao", "rice"), ("xoi", "xoi"),
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
        ("molasses", "syrup_bottle"), ("cane syrup", "syrup_bottle"), ("sugarcane syrup", "syrup_bottle"), ("caramel syrup", "syrup_bottle"), ("corn syrup", "syrup_bottle"), ("syrup", "syrup_bottle"), ("nuoc hang", "syrup_bottle"), ("mat mia", "syrup_bottle"),
        ("mirin", "rice_wine"), ("rice wine", "rice_wine"), ("cooking wine", "wine_white"), ("five spice", "star_anise"),
        ("monosodium glutamate", "salt"), ("msg", "salt"), ("bot ngot", "salt"), ("hat nem", "salt"), ("bot canh", "salt"),
        ("cornstarch", "flour"), ("corn starch", "flour"), ("tapioca starch", "flour"), ("bot nang", "flour"),
        // Đồ uống
        ("coffee", "coffee"), ("ca phe", "coffee"), ("espresso", "espresso"), ("milk tea", "bubble_tea"), ("bubble tea", "bubble_tea"), ("boba", "bubble_tea"), ("tra sua", "bubble_tea"), ("tea", "tea"), ("water", "water"),
        ("red wine", "wine_red"), ("white wine", "wine_white"), ("wine", "wine_red"), ("beer", "beer"), ("bia", "beer"), ("sake", "sake"),
        ("cocoa", "cocoa_powder"), ("cacao", "cocoa_powder"), ("chocolate", "cocoa_powder"), ("socola", "cocoa_powder"),
        // Trái cây
        ("apple", "apple"), ("banana", "banana"), ("chuoi", "banana"), ("strawberry", "strawberry"), ("dau tay", "strawberry"),
        ("orange", "orange"), ("nuoc cam", "orange_juice"), ("cam", "orange"), ("lemon", "lemon"), ("lime", "lime"), ("chanh", "lime"), ("grapefruit", "grapefruit"), ("pomelo", "grapefruit"), ("buoi", "grapefruit"),
        ("grape", "grape"), ("cherry", "cherry"), ("blueberry", "blueberry"), ("blackberry", "blackberry"), ("raspberry", "raspberry"), ("cranberry", "cranberry"),
        ("currant", "currant"), ("elderberry", "elderberry"), ("gooseberry", "gooseberry"), ("peach", "peach"), ("apricot", "apricot"), ("plum", "plum"), ("pear", "pear"),
        ("pineapple", "pineapple"), ("mango", "mango"), ("xoai", "mango"), ("papaya", "papaya"), ("du du", "papaya"), ("kiwi", "kiwi"), ("cantaloupe", "cantaloupe"), ("honeydew", "honeydew"),
        ("watermelon", "watermelon"), ("dua hau", "watermelon"), ("dragon fruit", "dragon_fruit"), ("thanh long", "dragon_fruit"), ("lychee", "lychee"), ("longan", "lychee"),
        ("passion fruit", "passion_fruit"), ("chanh day", "passion_fruit"), ("guava", "guava"), ("coconut water", "coconut_water"), ("nuoc dua", "coconut_water"), ("coconut", "coconut"),
        ("fig", "fig"), ("date", "date"), ("persimmon", "persimmon"), ("pomegranate", "pomegranate"), ("star fruit", "star_fruit"), ("yuzu", "yuzu"), ("calamansi", "calamansi"),
        ("durian", "durian"), ("sau rieng", "durian"), ("mangosteen", "mangosteen"), ("mang cut", "mangosteen"), ("rambutan", "rambutan"), ("chom chom", "rambutan"),

        // ===== Bộ icon mở rộng (529 khoá): cụm cụ thể thắng cụm chung nhờ nhiều từ hơn =====
        // Ngũ cốc ăn sáng, món nước, mì
        ("oatmeal", "oatmeal_bowl"), ("oat", "oatmeal_bowl"), ("yen mach", "oatmeal_bowl"), ("cereal", "cereal_bowl"), ("granola bar", "granola_bar"), ("granola", "granola"),
        ("ramen soup", "ramen_bowl"), ("rice vermicelli", "rice_noodles"), ("vermicelli", "rice_noodles"), ("noodle", "ramen_noodles"), ("egg noodle", "ramen_noodles"), ("instant noodle", "ramen_noodles"),
        ("fried noodle", "fried_noodles"), ("mi xao", "fried_noodles"), ("mien xao", "fried_noodles"), ("hu tieu", "hu_tieu"),
        ("pad thai", "pad_thai"), ("bibimbap", "bibimbap"), ("chicken rice", "chicken_rice"), ("com ga", "chicken_rice"), ("rice bowl", "rice_bowl"), ("sushi bowl", "sushi_bowl"),
        ("fried rice", "fried_rice"), ("com chien", "fried_rice"), ("congee", "congee"), ("sticky rice", "xoi"), ("glutinous rice", "xoi"),
        ("pho", "pho"), ("beef pho", "pho"), ("pho bo", "pho"), ("pho ga", "pho"), ("bun bo hue", "bun_bo_hue"), ("hue beef noodle", "bun_bo_hue"), ("bun cha", "bun_cha"),
        ("com tam", "com_tam"), ("broken rice", "com_tam"), ("banh xeo", "banh_xeo"), ("goi cuon", "goi_cuon"), ("fresh spring roll", "goi_cuon"), ("summer roll", "goi_cuon"), ("nem cuon", "goi_cuon"), ("spring roll", "spring_rolls"),
        ("dumpling", "dumplings"), ("sushi", "sushi_roll"), ("nigiri", "nigiri"), ("sashimi", "sashimi"), ("teriyaki chicken", "teriyaki_chicken"), ("mapo tofu", "mapo_tofu"), ("grilled fish", "grilled_fish"),
        ("che", "che_dessert"), ("soup", "soup_bowl"), ("salad", "salad_bowl"), ("caesar salad", "caesar_salad"), ("curry", "curry"), ("stir fried", "stir_fry"), ("stir fry", "stir_fry"), ("kebab", "kebab"),
        // Món Tây
        ("hamburger", "hamburger"), ("burger", "hamburger"), ("cheeseburger", "cheeseburger"), ("pizza", "pizza_slice"), ("hot dog", "hot_dog"), ("sandwich", "sandwich"), ("sub sandwich", "sub_sandwich"), ("club sandwich", "club_sandwich"),
        ("burrito", "burrito"), ("taco", "taco"), ("quesadilla", "quesadilla"), ("nacho", "nachos"), ("french fry", "french_fries"), ("khoai tay chien", "french_fries"), ("khoai chien", "french_fries"),
        ("fried chicken", "fried_chicken"), ("chicken nugget", "chicken_nuggets"), ("chicken wing", "chicken_wings"), ("roast chicken", "roast_chicken"), ("meatball", "meatballs"), ("spaghetti", "spaghetti"),
        ("mac and cheese", "mac_and_cheese"), ("lasagna", "lasagna"), ("lasagna sheet", "lasagna_sheet"), ("omelette", "omelette"), ("omelet", "omelette"), ("fried egg", "fried_egg"), ("scrambled egg", "scrambled_eggs"),
        ("boiled egg", "boiled_egg"), ("trung luoc", "boiled_egg"), ("trung op la", "fried_egg"), ("trung chien", "fried_egg"),
        // Bánh mì, bánh ngọt
        ("bread", "bread_loaf"), ("bread loaf", "bread_loaf"), ("white bread", "white_bread_slice"), ("sandwich bread", "white_bread_slice"), ("bread slice", "white_bread_slice"), ("banh mi sandwich", "white_bread_slice"), ("banh mi lat", "white_bread_slice"),
        ("whole wheat bread", "whole_wheat_bread"), ("wholemeal bread", "whole_wheat_bread"), ("toast", "toast"), ("sourdough", "sourdough"), ("baguette", "baguette"), ("ciabatta", "ciabatta"), ("croissant", "croissant"), ("bagel", "bagel"),
        ("english muffin", "english_muffin"), ("pita", "pita"), ("naan", "naan"), ("tortilla", "tortilla"), ("flatbread", "flatbread"), ("hamburger bun", "hamburger_bun"), ("hot dog bun", "hot_dog_bun"), ("bread roll", "bread_roll"),
        ("rye bread", "rye_bread"), ("cornbread", "cornbread"), ("garlic bread", "garlic_bread"), ("brioche", "brioche"), ("focaccia", "focaccia"), ("pretzel bun", "pretzel_bun"), ("breadstick", "breadsticks"), ("cinnamon roll", "cinnamon_roll"),
        ("banh mi", "banh_mi"), ("banh mi thit", "banh_mi"), ("banh mi kep", "banh_mi"),
        ("muffin", "muffin"), ("scone", "scone"), ("danish", "danish_pastry"), ("donut", "donut"), ("doughnut", "donut"), ("cupcake", "cupcake"), ("cake", "cake_slice"), ("pie", "pie_slice"), ("cookie", "cookie"), ("brownie", "brownie"),
        ("pancake", "pancakes"), ("waffle", "waffle"), ("french toast", "french_toast"),
        // Đồ ngọt
        ("ice cream", "ice_cream_scoop"), ("ice cream cone", "ice_cream_cone"), ("gelato", "fruit_gelato"), ("popsicle", "popsicle"), ("chocolate bar", "chocolate_bar"), ("dark chocolate", "chocolate_bar"),
        ("socola den", "chocolate_bar"), ("chocolate truffle", "chocolate_truffle"), ("candy", "candy"), ("lollipop", "lollipop"), ("gummy bear", "gummy_bears"), ("jelly", "jelly"), ("pudding", "pudding"), ("custard", "custard"),
        ("creme brulee", "creme_brulee"), ("caramel", "caramel"), ("marshmallow", "marshmallow"), ("cotton candy", "cotton_candy"), ("macaron", "macaron"), ("churro", "churro"), ("mochi", "mochi"), ("baklava", "baklava"),
        ("tiramisu", "tiramisu"), ("cheesecake", "cheesecake"), ("fruit tart", "fruit_tart"), ("jam", "jam_jar"), ("chocolate spread", "chocolate_spread_jar"),
        // Đồ uống
        ("orange juice", "orange_juice"), ("apple juice", "apple_juice"), ("juice box", "juice_box"), ("lemonade", "lemonade"), ("smoothie", "smoothie"), ("mango smoothie", "mango_smoothie"), ("strawberry smoothie", "strawberry_smoothie"),
        ("milkshake", "milkshake"), ("protein shake", "protein_shake"), ("soda", "soda_can"), ("soft drink", "soda_can"), ("nuoc ngot", "soda_can"), ("cola", "cola_bottle"), ("energy drink", "energy_drink"), ("sport drink", "sports_drink"),
        ("iced coffee", "iced_coffee"), ("ca phe sua", "vietnamese_coffee_milk"), ("iced milk coffee", "vietnamese_coffee_milk"), ("vietnamese coffee", "vietnamese_coffee_milk"), ("latte", "latte"), ("cappuccino", "cappuccino"), ("americano", "americano"),
        ("hot chocolate", "hot_chocolate"), ("green tea", "green_tea"), ("matcha", "matcha"), ("iced matcha", "iced_matcha"), ("iced tea", "iced_tea"), ("herbal tea", "herbal_tea"), ("fruit tea", "fruit_tea"),
        ("sparkling water", "sparkling_water"), ("sugarcane juice", "sugarcane_juice"), ("nuoc mia", "sugarcane_juice"), ("whiskey", "whiskey"), ("whisky", "whiskey"), ("vodka", "vodka"), ("cocktail", "cocktail"), ("margarita", "margarita"), ("champagne", "champagne"),
        // Snack, thực phẩm bổ sung
        ("potato chip", "potato_chips"), ("snack khoai tay", "potato_chips"), ("bim bim", "potato_chips"), ("popcorn", "popcorn"), ("trail mix", "trail_mix"), ("protein bar", "protein_bar"), ("rice cake", "rice_cake"),
        ("pretzel", "pretzel"), ("cracker", "cracker"), ("cheese cracker", "cracker"), ("pork rind", "pork_rinds"), ("seaweed snack", "seaweed_snack"), ("jerky", "jerky"), ("dried fruit", "dried_fruit"), ("dried mango", "dried_mango"),
        ("fruit snack", "fruit_snack"), ("energy gel", "energy_gel"),
        ("whey", "whey_scoop"), ("whey protein", "whey_scoop"), ("protein powder", "protein_powder"), ("creatine", "creatine"), ("bcaa", "bcaa"), ("multivitamin", "multivitamin"), ("vitamin", "vitamin_pills"), ("vitamin c", "vitamin_c"),
        ("fish oil", "fish_oil"), ("collagen", "collagen"), ("electrolyte", "electrolyte_powder"), ("fiber supplement", "fiber_supplement"), ("magnesium", "magnesium_capsules"), ("pre workout", "pre_workout"), ("shaker", "shaker_cup"),
        // Gia vị, dầu, đồ ăn kèm, đạm thực vật
        ("vinegar", "vinegar"), ("ranch dressing", "ranch_dressing"), ("dressing", "salad_dressing"), ("curry powder", "curry_powder"), ("nutmeg", "nutmeg"), ("baking soda", "baking_soda"), ("baking powder", "baking_powder"),
        ("stevia", "stevia"), ("sucralose", "sucralose"), ("shortening", "shortening"), ("lard", "shortening"), ("coconut oil", "coconut_oil"), ("canola oil", "canola_oil"), ("sesame oil", "sesame_oil"),
        ("tahini", "tahini"), ("hummus", "hummus"), ("salsa", "salsa"), ("guacamole", "guacamole"), ("relish", "relish"), ("wasabi", "wasabi"), ("miso", "miso"), ("tomato sauce", "tomato_sauce"), ("tomato paste", "tomato_sauce"),
        ("broth", "broth"), ("seasoning granule", "salt"),

        // ===== Loại món (xem DishWords): bánh/nước ép/súp/salad thắng nguyên liệu trong tên =====
        ("strudel", "pie_slice"), ("ice cream sandwich", "ice_cream_scoop"), ("tart", "fruit_tart"), ("juice", "juice_box"), ("fruit drink", "juice_box"), ("juice drink", "juice_box"), ("salad dressing", "salad_dressing"), ("egg roll", "spring_rolls"),
        ("roll", "bread_roll"), ("biscuit", "bread_roll"), ("gravy", "soup_bowl"), ("liqueur", "cocktail"), ("pina colada", "cocktail"),
        ("plantain", "banana"), ("snowpea", "snow_peas"), ("daiquiri", "cocktail"), ("tequila", "cocktail"), ("frankfurter", "hot_dog"), ("wiener", "hot_dog"), ("frozen novelty", "popsicle"), ("refried bean", "pinto_beans"),
        // ===== Rau đơn lẻ: khoá cụ thể gần nhất thay cho icon chung (salad_bowl chỉ dành cho món salad) =====
        ("summer squash", "yellow_squash"), ("summer squash zucchini", "zucchini"), ("winter squash", "butternut_squash"), ("pea", "snap_peas"), ("green pea", "snap_peas"),
        ("watercress", "arugula"), ("cress", "arugula"), ("chard", "kale"), ("greens", "collard_greens"), ("taro", "cassava"),
        ("seaweed", "seaweed_salad_veg"), ("kelp", "seaweed_salad_veg"), ("seaweed salad", "seaweed_salad_veg"), ("green jackfruit", "green_jackfruit"),
    ];

    // Cụm còn dấu (chữ thường): các từ mà bỏ dấu sẽ nhầm sang nghĩa khác hoặc từ thường dùng.
    private static readonly (string Phrase, string Key)[] Strict =
    [
        ("bò", "beef"), ("bơ", "butter"), ("gà", "chicken"), ("vịt", "duck"), ("tỏi", "garlic"),
        ("ngô", "corn"), ("bắp", "corn"), ("ngò", "cilantro"), ("sả", "lemongrass"), ("ớt", "chili_pepper"), ("nấm", "mushroom"),
        ("trà", "tea"), ("cháo", "congee"), ("bún", "rice_noodles"), ("phở", "pho"), ("miến", "rice_noodles"), ("mì", "ramen_noodles"),
        ("dừa", "coconut"), ("dứa", "pineapple"), ("mít", "jackfruit"), ("nhãn", "lychee"), ("vải", "lychee"), ("ổi", "guava"), ("nho", "grape"), ("táo", "apple"), ("lê", "pear"),
    ];

    // Fallback khi không khớp cụm nào: chỉ nhóm mà icon đại diện không sai về loại. Trái cây KHÔNG có icon chung (Rhubarb, Quince… từng ra "apple") → null; rau cũng KHÔNG có icon chung (từng ra "cabbage", rồi "salad_bowl": rau đơn lẻ như đậu Hà Lan, rong biển, watercress không phải món salad) → null; salad_bowl chỉ cho món salad.
    private static readonly Dictionary<FoodCategory, string> CategoryFallback = new()
    {
        [FoodCategory.Dairy] = "milk",
    };

    // Fallback theo nhóm của nguồn (USDA SR/Foundation: foodCategory; FNDDS: WWEIA), chỉ khi không cụm nào khớp. Khớp theo chuỗi con (chữ thường), cái đứng trước thắng.
    // Chỉ icon CHUNG đúng loại; nhóm không có icon chung hợp lý (trái cây, hạt, đậu, gia vị, dầu, thịt rừng, chất tạo ngọt…) không có dòng ở đây → null (riêng trái cây có dòng Key = null để "Fruit Juices" không ăn nhầm "juice") (FE dùng emoji), và được báo ở reassign-icons là khái niệm thiếu icon.
    // Không dùng "mixed_nuts"/"tofu"/"tempeh": hình vẽ sai (xem FoodIconKeys.NotAutoAssigned).
    private static readonly (string Needle, string? Key)[] GroupFallback =
    [
        ("fruits and fruit", null), ("alcoholic", "cocktail"), ("finfish", "grilled_fish"), ("shellfish", "shrimp"), ("fish", "grilled_fish"),
        ("salad", "salad_bowl"), ("soup", "soup_bowl"), ("sauces, and gravies", "soup_bowl"),
        ("cakes", "cake_slice"), ("cake", "cake_slice"), ("pie", "pie_slice"), ("cookie", "cookie"), ("brownie", "cookie"), ("candy", "candy"), ("candies", "candy"),
        ("ice cream", "ice_cream_scoop"), ("frozen dessert", "ice_cream_scoop"), ("yogurt", "yogurt"), ("cheese", "cheddar"),
        ("bread", "bread_roll"), ("rolls", "bread_roll"), ("baked products", "bread_roll"), ("pizza", "pizza_slice"), ("sandwich", "sandwich"), ("burger", "hamburger"),
        ("breakfast cereal", "cereal_bowl"), ("cereal grains", "bowl"), ("cereal", "cereal_bowl"), ("pasta", "pasta"), ("noodle", "ramen_noodles"), ("rice", "rice"),
        ("pancake", "pancakes"), ("waffle", "waffle"), ("egg", "eggs"),
        ("juice", "juice_box"), ("soft drink", "soda_can"), ("soda", "soda_can"), ("sports", "sports_drink"), ("energy", "energy_drink"), ("coffee", "coffee"), ("tea", "tea"), ("beer", "beer"), ("wine", "wine_red"),
        ("poultry", "chicken"), ("chicken", "chicken"), ("beef", "beef"), ("pork", "pork"), ("sausages", "sausage"), ("sausage", "sausage"),
        ("fast food", "takeout_box"), ("restaurant", "takeout_box"), ("meals, entrees", "bowl"), ("mixed dishes", "bowl"), ("stew", "bowl"),
    ];

    // Từ chỉ LOẠI món: khi đoạn đầu của tên (trước dấu phẩy đầu tiên) có một từ này thì cụm chứa nó (mọi từ của cụm nằm trong đoạn đầu/đoạn hai) thắng nguyên liệu ("Apple pie" → pie_slice, "Oat flour" → flour, "Chicken noodle soup" → soup_bowl).
    // Không gồm "bread" (USDA viết "Bread, pita": từ sau dấu phẩy mới là món cụ thể).
    private static readonly HashSet<string> DishWords = new(StringComparer.Ordinal)
    {
        "pie", "strudel", "tart", "cake", "cookie", "brownie", "muffin", "cupcake", "pancake", "waffle", "donut", "doughnut", "soup", "salad", "sandwich", "flour", "juice", "pudding",
    };

    // Cụm chỉ có nghĩa khi đứng ở đoạn đầu của tên (loại món): "Grape juice" → juice_box nhưng "Tangerines, canned, juice pack" là quýt
    private static readonly HashSet<string> HeadOnlyPhrases = new(StringComparer.Ordinal) { "juice", "fruit drink", "juice drink" };

    // Từ mở đầu đoạn mô tả cách làm/phục vụ ("on white bread", "with mayonnaise"): đoạn đó không dùng để chọn cụm loại món
    private static readonly HashSet<string> Qualifiers = new(StringComparer.Ordinal) { "on", "with", "without", "in", "made", "from", "for", "and", "or" };

    // Cụm bổ ngữ cách chế biến/đóng gói không nói gì về loại món ("Squash, boiled, drained, without salt" từng ra icon muối): bỏ trước khi so cụm
    private static readonly System.Text.RegularExpressions.Regex Noise = new(
        @"\b(?:(?:with|without|no)\s+(?:added\s+)?salt(?:\s+added)?|salt\s+(?:added|free)(?:\s+in\s+(?:processing|cooking))?|(?:no\s+)?(?:fat|sodium|sugars?)\s+added(?:\s+in\s+(?:processing|cooking))?|(?:no\s+|with\s+)?added\s+(?:fat|sodium|sugars?|salt)|(?:un)?drained(?:\s+solids)?|rinsed|unenriched|enriched)\b",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

    // Khoá có hình sai (FoodIconKeys.NotAutoAssigned) không bao giờ được resolver trả ra, kể cả khi ai đó thêm cụm trỏ tới chúng
    private static readonly Entry[] Entries = Plain.Select(e => Build(e, strict: false)).Concat(Strict.Select(e => Build(e, strict: true)))
        .Where(e => !FoodIconKeys.NotAutoAssigned.Contains(e.Key)).ToArray();

    /// <summary>Mọi khoá mà bảng có thể trả ra (self-check đối chiếu với <see cref="FoodIconKeys"/>).</summary>
    public static IEnumerable<string> AllKeys => Entries.Select(e => e.Key).Concat(CategoryFallback.Values).Concat(GroupFallback.Select(g => g.Key).OfType<string>().Where(k => !FoodIconKeys.NotAutoAssigned.Contains(k))).Append("avocado").Distinct();

    /// <param name="categoryFallback">false = bỏ fallback theo nhóm món (reassign-icons thử tên gốc USDA trước khi dùng nó).</param>
    /// <returns>Khoá icon hoặc null nếu không chắc (tên trống, không khớp cụm nào và nhóm món không có icon đại diện).</returns>
    public static string? Resolve(string? name, FoodCategory? category = null, string? sourceGroup = null, bool categoryFallback = true)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        name = Noise.Replace(name, " ");
        var plain = PlainTokens(name);
        var strict = StrictTokens(name);
        // Từ chỉ loại món trong đoạn đầu tên (trước dấu phẩy đầu tiên): cụm chứa nó thắng cụm nguyên liệu
        var segments = name.Split(',');
        var headTokens = PlainTokens(segments[0]);
        var dish = headTokens.Where(DishWords.Contains).ToArray();
        // Cụm loại món được dùng từ của đoạn đầu và đoạn hai ("Muffins, English" → english muffin) trừ khi đoạn hai là bổ ngữ ("Hot dog sandwich, on white bread" không thành "sandwich bread")
        var dishTokens = segments.Length > 1 && PlainTokens(segments[1]) is { Length: > 0 } second && !Qualifiers.Contains(second[0]) ? headTokens.Concat(second).ToArray() : headTokens;

        Entry? best = null, bestDish = null;
        var bestPosition = int.MaxValue;
        var bestDishPosition = int.MaxValue;
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
            if (!e.HeadOnly && (best is null || e.Tokens.Length > best.Tokens.Length || (e.Tokens.Length == best.Tokens.Length && position < bestPosition)))
            {
                best = e;
                bestPosition = position;
            }
            if (dish.Length > 0 && !e.Strict && e.Tokens.Any(dish.Contains) && e.Tokens.All(dishTokens.Contains)
                && (bestDish is null || e.Tokens.Length > bestDish.Tokens.Length || (e.Tokens.Length == bestDish.Tokens.Length && position < bestDishPosition)))
            {
                bestDish = e;
                bestDishPosition = position;
            }
        }

        // "Bơ" là bơ sữa ở nhóm khác, nhưng là trái bơ khi món thuộc nhóm trái cây
        if (best is { Phrase: "bơ" } && category == FoodCategory.Fruits) return "avocado";
        if (bestDish is not null) return bestDish.Key;
        if (best is not null) return best.Key;
        if (!string.IsNullOrWhiteSpace(sourceGroup))
            foreach (var (needle, key) in GroupFallback)
                if (GroupHas(sourceGroup, needle)) return key is not null && !FoodIconKeys.NotAutoAssigned.Contains(key) ? key : null;
        return categoryFallback && category is { } c && CategoryFallback.TryGetValue(c, out var fallback) ? fallback : null;
    }

    // Khớp đầu từ ("tea" không khớp "steak", "pie" không khớp "spiel")
    private static bool GroupHas(string group, string needle)
    {
        for (var i = group.IndexOf(needle, StringComparison.OrdinalIgnoreCase); i >= 0; i = group.IndexOf(needle, i + 1, StringComparison.OrdinalIgnoreCase))
            if (i == 0 || !char.IsLetter(group[i - 1])) return true;
        return false;
    }

    private static Entry Build((string Phrase, string Key) e, bool strict)
        => new(e.Phrase, e.Key, strict ? StrictTokens(e.Phrase) : PlainTokens(e.Phrase), strict, !strict && HeadOnlyPhrases.Contains(e.Phrase));

    // Bỏ dấu, chữ thường, số ít từng từ ("eggs" → "egg")
    private static string[] PlainTokens(string s)
        => FoodNameNormalizer.Normalize(s).Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Singular).ToArray();

    // ponytail: quy tắc số ít cho tên tiếng Anh (cherries → cherry, tomatoes → tomato, peaches → peach, eggs → egg); từ bất quy tắc (leaves) để bảng khai báo cả hai dạng
    private static string Singular(string t)
    {
        // "cookies" → "cooky" làm "Cookies, …" không khớp "cookie": các từ này số ít là -ie
        if (t is "cookies" or "brownies" or "smoothies" or "veggies" or "pies") return t[..^1];
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
