namespace Infrastructure;

public class AppData
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        //if (!await context.Roles.AnyAsync())
        //    await context.Roles.AddRangeAsync(RoleData.GetRoles());

        //await EventData.SeedAsync(context);

        await context.SaveChangesAsync();
    }
}
