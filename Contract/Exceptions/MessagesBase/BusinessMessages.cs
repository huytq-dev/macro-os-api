namespace Contract;

public static class BusinessMessages
{
    // Success Messages
    public static string CreatedSuccessfully(string entityName) => $"Created {entityName} successfully";
    public static string UpdatedSuccessfully(string entityName) => $"Updated {entityName} successfully";
    public static string DeletedSuccessfully(string entityName) => $"Deleted {entityName} successfully";
    public static string FoundSuccessfully(string entityName) => $"Found {entityName} successfully";

    // Failure Messages
    public static string CreateFailure(string entityName) => $"Failed to create {entityName}";
    public static string UpdateFailure(string entityName) => $"Failed to update {entityName}";
    public static string DeleteFailure(string entityName) => $"Failed to delete {entityName}";
    public static string GetFailure(string entityName) => $"Failed to get {entityName}";
}