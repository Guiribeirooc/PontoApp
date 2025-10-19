// PontoApp.Web/Controllers/OnCallController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoApp.Application.Contracts;

namespace PontoApp.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
public class OnCallController : Controller
{
    private readonly IOnCallService _svc;
    public OnCallController(IOnCallService svc) => _svc = svc;

    public async Task<IActionResult> Index(int id, DateTime? de, DateTime? ate, CancellationToken ct)
    {
        // datas padrão: -7d / +7d
        var deDt = (de ?? DateTime.Today.AddDays(-7)).Date;
        var ateDt = (ate ?? DateTime.Today.AddDays(+7)).Date;

        var from = DateOnly.FromDateTime(deDt);
        var to = DateOnly.FromDateTime(ateDt);

        var list = await _svc.ListAsync(id, from, to, ct);

        ViewBag.Id = id;
        ViewBag.De = from;
        ViewBag.Ate = to;
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, DateTime start, DateTime end, string? notes, CancellationToken ct)
    {
        await _svc.AddAsync(
            id,
            DateOnly.FromDateTime(start.Date),
            DateOnly.FromDateTime(end.Date),
            notes,
            ct
        );

        return RedirectToAction(nameof(Index), new { id });
    }
}
