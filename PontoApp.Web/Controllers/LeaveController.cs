using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoApp.Application.Contracts;
using PontoApp.Domain.Entities;
using PontoApp.Domain.Enums;

[Authorize(Policy = "RequireAdmin")]
public class LeaveController : Controller
{
    private readonly ILeaveService _svc;
    public LeaveController(ILeaveService svc) => _svc = svc;

    public async Task<IActionResult> Index(int? id, LeaveType? type, DateOnly? de, DateOnly? ate, CancellationToken ct)
    {
        var list = await _svc.ListAsync(id, de, ate, type, ct);
        ViewBag.Type = type;
        return View(list);
    }

    [HttpGet] public IActionResult Create() => View(new Leave { Start = DateOnly.FromDateTime(DateTime.Today), End = DateOnly.FromDateTime(DateTime.Today) });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Leave m, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(m);
        await _svc.CreateAsync(m, ct);
        return RedirectToAction(nameof(Index));
    }
}
