using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoApp.Application.Contracts;

[Authorize(Policy = "RequireAdmin")]
public class BankController : Controller
{
    private readonly ITimeBankService _bank;
    public BankController(ITimeBankService bank) => _bank = bank;

    public async Task<IActionResult> Index(int id, DateOnly? de, DateOnly? ate, CancellationToken ct)
    {
        // id = EmployeeId; em produção, coloque um seletor
        var start = de ?? new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        var end = ate ?? start.AddMonths(1).AddDays(-1);
        var saldo = await _bank.GetBalanceMinutesAsync(id, start, end, ct);
        var extrato = await _bank.GetStatementAsync(id, start, end, ct);
        ViewBag.Id = id; ViewBag.De = start; ViewBag.Ate = end; ViewBag.Saldo = saldo;
        return View(extrato);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ajuste(int id, int minutes, string reason, CancellationToken ct)
    {
        await _bank.AddAdjustmentAsync(id, minutes, reason, ct);
        return RedirectToAction(nameof(Index), new { id });
    }
}
