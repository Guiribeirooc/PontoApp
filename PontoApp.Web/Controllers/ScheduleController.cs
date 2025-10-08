using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoApp.Application.Contracts;

[Authorize(Policy = "RequireAdmin")]
public class ScheduleController : Controller
{
    private readonly IScheduleService _svc;
    public ScheduleController(IScheduleService svc) => _svc = svc;

    public async Task<IActionResult> Folgas(int id, DateOnly? de, DateOnly? ate, CancellationToken ct)
    {
        var start = de ?? DateOnly.FromDateTime(DateTime.Today);
        var end = ate ?? start.AddMonths(1);
        var list = await _svc.ListDayOffsAsync(id, start, end, ct);
        ViewBag.Id = id; ViewBag.De = start; ViewBag.Ate = end;
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFolga(int id, DateOnly date, string? reason, CancellationToken ct)
    {
        await _svc.AddDayOffAsync(id, date, reason, ct);
        return RedirectToAction(nameof(Folgas), new { id });
    }
}
