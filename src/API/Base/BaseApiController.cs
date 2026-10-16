namespace API;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class BaseApiController(ISender sender) : ControllerBase
{
    protected readonly ISender Sender = sender;
}
