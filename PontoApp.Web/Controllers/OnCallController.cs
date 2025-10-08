using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoApp.Application.Contracts;

[Authorize(Policy = "RequireAdmin")]
public class OnCallController : Controller
{
    private readonly IOnCallService _svc;
    public OnCallController(IOnCallService svc) => _svc = svc;

    public async Task<IActionResult> Index(int id, DateTime? de, DateTime? ate, CancellationToken ct)
    {
        var from = de ?? DateTime.Today.AddDays(-7);
        var to = ate ?? DateTime.Today.AddDays(7);
        var list = await _svc.ListAsync(id, from, to, ct);
        ViewBag.Id = id; ViewBag.De = from; ViewBag.Ate = to;
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, DateTime start, DateTime end, string? notes, CancellationToken ct)
    {
        await _svc.AddAsync(id, start, end, notes, ct);
        return RedirectToAction(nameof(Index), new { id });
    }
}
