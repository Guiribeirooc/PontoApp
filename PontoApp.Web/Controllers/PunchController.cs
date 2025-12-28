using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoApp.Application.Services;
using PontoApp.Domain.Enums;
using PontoApp.Domain.Interfaces;

namespace PontoApp.Web.Controllers;

[Authorize]
public class PunchController(IPunchService svc, IEmployeeRepository empRepo) : Controller
{
    private readonly IPunchService _svc = svc;
    private readonly IEmployeeRepository _empRepo = empRepo;

    private bool IsAdmin =>
        User.IsInRole("Admin") ||
        string.Equals(User.FindFirstValue(ClaimTypes.Role), "Admin", StringComparison.OrdinalIgnoreCase);

    private int? CurrentEmployeeId
    {
        get
        {
            var s = User.FindFirstValue("EmployeeId");
            return int.TryParse(s, out var id) ? id : (int?)null;
        }
    }

    private static (DateOnly Inicio, DateOnly Fim) GetPeriodoPadrao()
    {
        var hoje = DateTime.Today;
        var inicio = new DateOnly(hoje.Year, hoje.Month, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);
        return (inicio, fim);
    }

    [HttpGet]
    public IActionResult Bater()
    {
        if (IsAdmin)
            return RedirectToAction("Create", "AdminPunch");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bater(PunchType tipo, CancellationToken ct)
    {
        if (IsAdmin)
            return RedirectToAction("Create", "AdminPunch");

        var empId = CurrentEmployeeId;
        if (empId is null) return Forbid();

        var emp = await _empRepo.GetByIdAsync(empId.Value, ct);
        if (emp is null || !emp.Ativo || emp.IsDeleted) return Forbid();

        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var res = await _svc.MarcarAsync(emp.Nome, emp.Pin, tipo, ip, ct);

            TempData["ok"] = $"{tipo} registrada às {res.DataHora:HH:mm}.";
            return RedirectToAction(nameof(Meu));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View();
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Erro inesperado ao registrar o ponto.");
            return View();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Meu(DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        var empId = CurrentEmployeeId;
        if (empId is null) return Forbid();

        var (defInicio, defFim) = GetPeriodoPadrao();
        var ini = inicio ?? defInicio;
        var end = fim ?? defFim;

        var dados = await _svc.ListarPeriodoAsync(ini, end, empId, ct);
        return View("MesResultado", dados);
    }

    [HttpGet]
    public async Task<IActionResult> Mes(CancellationToken ct)
    {
        var (ini, fim) = GetPeriodoPadrao();
        ViewBag.Ano = ini.Year;
        ViewBag.Mes = ini.Month;

        var empId = CurrentEmployeeId;
        if (empId is null) return Forbid();

        var dados = await _svc.ListarPeriodoAsync(ini, fim, empId, ct);
        return View("MesResultado", dados);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Mes(int ano, int mes, CancellationToken ct)
    {
        var empId = CurrentEmployeeId;
        if (empId is null) return Forbid();

        var inicio = new DateOnly(ano, mes, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);

        var dados = await _svc.ListarPeriodoAsync(inicio, fim, empId, ct);
        return View("MesResultado", dados);
    }

    [HttpGet]
    public async Task<IActionResult> Dia(DateOnly? dia, CancellationToken ct)
    {
        var d = dia ?? DateOnly.FromDateTime(DateTime.Today);

        var myId = CurrentEmployeeId;
        if (myId is null) return Forbid();

        var list = await _svc.ListarDoDiaAsync(d, myId, ct);
        return View(list);
    }
}
