namespace API;

/// <summary>Recipe của người dùng (tab Library): tạo/sửa/xoá, dinh dưỡng tính từ nguyên liệu; log recipe qua POST /food-logs/entries (recipeId).</summary>
[Route("api/recipes")]
public sealed class RecipesController(ISender sender) : BaseApiController(sender)
{
    /// <summary>Danh sách recipe của tôi (theo tên), tìm kiếm và phân trang top/skip.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<QueryResult<RecipeSummaryModel>>>> List(
        [FromQuery] GetRecipesQuery query, CancellationToken ct)
        => Ok(ApiResponse<QueryResult<RecipeSummaryModel>>.Ok(await Sender.Send(query, ct)));

    /// <summary>Chi tiết recipe: nguyên liệu, tổng và theo khẩu phần.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RecipeResponse>>> GetById(Guid id, CancellationToken ct)
        => Ok(ApiResponse<RecipeResponse>.Ok(await Sender.Send(new GetRecipeQuery(id), ct)));

    /// <summary>Tạo recipe từ danh sách nguyên liệu (món + khẩu phần + số lượng).</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreateRecipeCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Guid>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Create Recipe step 1: dinh dưỡng của recipe đang soạn (chưa lưu), tổng và theo khẩu phần, kèm % target.</summary>
    [HttpPost("nutrients")]
    public async Task<ActionResult<ApiResponse<RecipeNutrientsResponse>>> GetNutrients(
        [FromBody] GetRecipeNutrientsQuery query, CancellationToken ct)
        => Ok(ApiResponse<RecipeNutrientsResponse>.Ok(await Sender.Send(query, ct)));

    /// <summary>"Create &amp; Add": tạo recipe rồi log nó (ngày/bữa/giờ như POST /food-logs/entries, mặc định hôm nay + giờ hiện tại, 1 khẩu phần).</summary>
    [HttpPost("create-and-log")]
    public async Task<ActionResult<ApiResponse<RecipeCreatedResponse>>> CreateAndLog(
        [FromBody] CreateAndLogRecipeCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<RecipeCreatedResponse>.Ok(await Sender.Send(cmd, ct)));

    /// <summary>Sửa recipe (gửi đủ object; nguyên liệu gửi lên = thay toàn bộ). Entry đã log không đổi.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Update(
        Guid id, [FromBody] UpdateRecipeCommand cmd, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(cmd with { Id = id }, ct)));

    /// <summary>Xoá (soft) recipe; entry đã log giữ snapshot.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Unit>>> Delete(Guid id, CancellationToken ct)
        => Ok(ApiResponse<Unit>.Ok(await Sender.Send(new DeleteRecipeCommand(id), ct)));
}
