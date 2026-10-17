namespace Application;

/// <summary>Giờ mặc định theo bữa và suy bữa từ giờ ăn — một nguồn cho BE và FE (timeline theo giờ).</summary>
public static class MealTimes
{
    public static TimeOnly Default(MealType meal) => meal switch
    {
        MealType.Breakfast => new TimeOnly(7, 0),
        MealType.MorningSnack => new TimeOnly(10, 0),
        MealType.Lunch => new TimeOnly(12, 0),
        MealType.AfternoonSnack => new TimeOnly(15, 0),
        MealType.Dinner => new TimeOnly(18, 30),
        _ => new TimeOnly(21, 0),
    };

    /// <summary>&lt; 10:00 Breakfast, &lt; 11:30 MorningSnack, &lt; 14:00 Lunch, &lt; 17:00 AfternoonSnack, &lt; 21:00 Dinner, còn lại EveningSnack.</summary>
    public static MealType ForTime(TimeOnly t)
        => t < new TimeOnly(10, 0) ? MealType.Breakfast
         : t < new TimeOnly(11, 30) ? MealType.MorningSnack
         : t < new TimeOnly(14, 0) ? MealType.Lunch
         : t < new TimeOnly(17, 0) ? MealType.AfternoonSnack
         : t < new TimeOnly(21, 0) ? MealType.Dinner
         : MealType.EveningSnack;
}
