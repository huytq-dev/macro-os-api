namespace Contract;

public static class ExceptionMessages
{
    // Không có tham số
    public const string Forbidden = "You do not have permission to access this resource";
    public const string DatabaseOperationFailed = "Database operation failed";
    public const string InternalError = "Unexpected error occurred";

    // --- NotFound ---
    public static string NotFound(string entityName, object id)
        => $"{entityName} with ID {id} was not found";

    public static string NotFoundField(string entityName, string fieldName, object value)
        => $"{entityName} with {fieldName} '{value}' was not found";

    public static string NotExists(string entityName, object value)
        => $"{entityName} with value '{value}' does not exist";

    // --- Conflict / Validation Logic ---
    public static string AlreadyExists(string entityName, object value)
        => $"{entityName} with value '{value}' already exists";

    public static string Invalid(string entityName)
        => $"{entityName} is invalid";
}