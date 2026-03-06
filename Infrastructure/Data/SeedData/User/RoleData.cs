//namespace Infrastructure;

//public class RoleData
//{
//    public static IEnumerable<Role> GetRoles()
//    {
//        var createdAt = new DateTimeOffset(2025, 11, 9, 11, 0, 0, TimeSpan.FromHours(7));

//        var roleTypes = Enum.GetValues<RoleType>();

//        return roleTypes.Select(role => new Role
//        {
//            Id = GuidHelper.From($"Role.{role}"),
//            RoleName = role.ToString(),
//            CreatedAt = createdAt
//        }).ToList();
//    }
//}