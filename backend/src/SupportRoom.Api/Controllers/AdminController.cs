using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SupportRoom.Application.Services;

namespace SupportRoom.Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminService _service;

    public AdminController(IServiceProvider serviceProvider)
    {
        _service = serviceProvider.GetRequiredService<IAdminService>();
    }

    [HttpPost("reset")]
    public ActionResult Reset()
    {
        _service.ResetDemoData();
        return Ok(new { status = "reset" });
    }

    /// <summary>Rebuilds the whole RAG index from source - see IAdminService.ReindexAllAsync.
    /// Long-running (re-embeds every lesson deck + document), so callers should expect seconds.</summary>
    [HttpPost("reindex")]
    public async Task<ActionResult> Reindex()
    {
        var result = await _service.ReindexAllAsync();
        return Ok(new { status = "reindexed", result });
    }
}
