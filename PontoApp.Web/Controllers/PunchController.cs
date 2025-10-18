using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Services;
using PontoApp.Domain.Entities;
using PontoApp.Domain.Interfaces;
using PontoApp.Web.ViewModels;

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

    private IEnumerable<SelectListItem> BuildEmployeeSelect()
    {
        return _empRepo.Query()
                       .Where(e => e.Ativo && !e.IsDeleted)
                       .OrderBy(e => e.Nome)
                       .Select(e => new SelectListItem
                       {
                           Value = e.Id.ToString(),
                           Text = $"{e.Pin} - {e.Nome}"
                       })
                       .ToList();
    }

    private List<SelectListItem> BuildEmployeeSelectWithAll(int? selectedId)
    {
        var items = new List<SelectListItem> { new() { Value = "", Text = "Todos", Selected = selectedId == null } };
        items.AddRange(
            _empRepo.Query()
                    .Where(e => e.Ativo && !e.IsDeleted)
                    .OrderBy(e => e.Nome)
                    .Select(e => new SelectListItem
                    {
                        Value = e.Id.ToString(),
                        Text = $"{e.Pin} - {e.Nome}",
                        Selected = selectedId.HasValue && selectedId.Value == e.Id
                    })
        );
        return items;
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
    [Authorize(Policy = "RequireAdmin")]
    public IActionResult Index(string? msg = null)
    {
        var vm = new PunchMarkViewModel
        {
            Mensagem = msg,
            Employees = BuildEmployeeSelect()
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Mark(PunchMarkViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.Employees = BuildEmployeeSelect();
            return View("Index", vm);
        }

        var emp = await _empRepo.GetByIdAsync(vm.EmployeeId!.Value, ct);
        if (emp is null || !emp.Ativo || emp.IsDeleted)
        {
            ModelState.AddModelError(nameof(vm.EmployeeId), "Colaborador inválido ou inativo.");
            vm.Employees = BuildEmployeeSelect();
            return View("Index", vm);
        }

        try
        {
            var res = await _svc.MarcarAsync(
                nome: emp.Nome,
                pin: emp.Pin,
                tipo: vm.Tipo,
                ip: null,
                ct: ct
            );

            TempData["SuccessMessage"] = $"{vm.Tipo} registrada às {res.DataHora:HH:mm}.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            vm.Employees = BuildEmployeeSelect();
            return View("Index", vm);
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Ocorreu um erro inesperado ao registrar o ponto.");
            vm.Employees = BuildEmployeeSelect();
            return View("Index", vm);
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

        if (IsAdmin)
        {
            ViewBag.Employees = new SelectList(
                _empRepo.Query()
                        .Where(e => e.Ativo && !e.IsDeleted)
                        .OrderBy(e => e.Nome)
                        .Select(e => new { e.Id, Nome = $"{e.Pin} - {e.Nome}" }),
                "Id", "Nome");
            return View();
        }
        else
        {
            var empId = CurrentEmployeeId;
            if (empId is null) return Forbid();

            var dados = await _svc.ListarPeriodoAsync(ini, fim, empId, ct);
            return View("MesResultado", dados);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Mes(int ano, int mes, int? employeeId, CancellationToken ct)
    {
        if (!IsAdmin)
            employeeId = CurrentEmployeeId;

        var inicio = new DateOnly(ano, mes, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);

        var dados = await _svc.ListarPeriodoAsync(inicio, fim, employeeId, ct);
        return View("MesResultado", dados);
    }

    [HttpGet]
    public async Task<IActionResult> Dia(DateOnly? dia, int? employeeId, CancellationToken ct)
    {
        var d = dia ?? DateOnly.FromDateTime(DateTime.Today);

        if (IsAdmin)
        {
            ViewBag.Employees = BuildEmployeeSelectWithAll(employeeId);

            if (employeeId.HasValue)
            {
                var ok = await _empRepo.Query()
                    .AnyAsync(e => e.Id == employeeId.Value && e.Ativo && !e.IsDeleted, ct);

                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, "Colaborador inválido ou inativo.");
                    var allList = await _svc.GetAllDoDiaAsync(d, ct);
                    return View(allList);
                }

                var listByEmp = await _svc.ListarDoDiaAsync(d, employeeId, ct);
                return View(listByEmp);
            }
            else
            {
                var listAll = await _svc.GetAllDoDiaAsync(d, ct);
                return View(listAll);
            }
        }
        else
        {
            var myId = CurrentEmployeeId;
            if (myId is null) return Forbid();

            var list = await _svc.ListarDoDiaAsync(d, myId, ct);
            return View(list);
        }
    }
}
