using System.Security.Claims;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Application.DTOs;
using PontoApp.Application.Services;
using PontoApp.Domain.Interfaces;
using PontoApp.Web.Pdf;
using PontoApp.Web.Printing;
using PontoApp.Web.ViewModels;
using QuestPDF.Fluent;

namespace PontoApp.Web.Controllers;

[Authorize]
public class ReportController(
    IReportService report,
    IEmployeeRepository empRepo,
    IPunchRepository punchRepo,
    IReportQueries reports) : Controller
{
    private readonly IReportService _report = report;
    private readonly IEmployeeRepository _empRepo = empRepo;
    private readonly IPunchRepository _punchRepo = punchRepo;
    private readonly IReportQueries _reports = reports;

    [HttpGet, Authorize(Policy = "RequireAdmin")]
    public IActionResult Periodo()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        ViewBag.Inicio = new DateOnly(hoje.Year, hoje.Month, 1);
        ViewBag.Fim = hoje;

        var itens = new List<SelectListItem> { new() { Value = "", Text = "Todos" } };
        itens.AddRange(
            _empRepo.Query()
                .Where(e => e.Ativo && !e.IsDeleted)
                .OrderBy(e => e.Nome)
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = $"{e.Pin} - {e.Nome}"
                })
                .ToList()
        );
        ViewBag.Employees = itens;

        return View();
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Periodo(DateOnly inicio, DateOnly fim, int? employeeId)
    {
        if (inicio > fim)
        {
            ModelState.AddModelError(string.Empty, "A data inicial não pode ser maior que a final.");
            return RecarregarPeriodoComErro(inicio, fim);
        }

        var resumo = await _report.ResumoAsync(inicio, fim, employeeId);
        return View("PeriodoResultado", resumo);
    }

    private ViewResult RecarregarPeriodoComErro(DateOnly inicio, DateOnly fim)
    {
        var itens = new List<SelectListItem> { new() { Value = "", Text = "Todos" } };
        itens.AddRange(
            _empRepo.Query()
                .Where(e => e.Ativo && !e.IsDeleted)
                .OrderBy(e => e.Nome)
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = $"{e.Pin} - {e.Nome}"
                })
                .ToList()
        );
        ViewBag.Employees = itens;
        ViewBag.Inicio = inicio;
        ViewBag.Fim = fim;

        return View("Periodo");
    }

    [HttpGet]
    public async Task<IActionResult> TimesheetPdf(DateOnly? inicio, DateOnly? fim, int? employeeId, CancellationToken ct)
    {
        inicio ??= new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        fim ??= DateOnly.FromDateTime(DateTime.Today);

        var isAdmin = User.IsInRole("Admin") ||
                      string.Equals(User.FindFirstValue(ClaimTypes.Role), "Admin", StringComparison.OrdinalIgnoreCase);

        if (!isAdmin)
        {
            var empClaim = User.FindFirstValue("EmployeeId") ?? User.FindFirstValue("employee_id");
            if (!int.TryParse(empClaim, out var ownId))
                return Forbid();

            employeeId = ownId;
        }

        var resumo = await _report.ResumoAsync(inicio.Value, fim.Value, employeeId);

        var model = new TimesheetPdfModel
        {
            Inicio = inicio.Value,
            Fim = fim.Value,
            Funcionario = string.IsNullOrWhiteSpace(resumo.EmployeeNome) ? "Colaborador" : resumo.EmployeeNome,
            Dias = (resumo.Dias ?? Enumerable.Empty<WorkDayDto>())
                   .OrderBy(d => d.Dia)
                   .Select(d =>
                   {
                       var entrada = d.Intervalos?.FirstOrDefault()?.In;
                       var saida = d.Intervalos?.LastOrDefault()?.Out;

                       var total = d.TotalDia != default ? d.TotalDia : (entrada.HasValue && saida.HasValue ? saida.Value - entrada.Value : (TimeSpan?)null);
                       var extras = d.HorasExtras;
                       var atraso = d.MinutosAtraso;

                       return new TimesheetPdfDay
                       {
                           Data = d.Dia,
                           Entrada = entrada,
                           Saida = saida,
                           SaidaAlmoco = d.SaidaAlmoco,
                           VoltaAlmoco = d.VoltaAlmoco,
                           Total = total,
                           Extras = extras,
                           Atraso = atraso
                       };
                   }).ToList(),
            BancoDeHoras = resumo.BancoDeHoras,

        };

        var pdfBytes = Document.Create(container => new TimesheetDocument(model).Compose(container))
                              .GeneratePdf();
        var safeName = model.Funcionario.Replace(' ', '_');
        var fileName = $"espelho_{safeName}_{model.Inicio:yyyyMMdd}_{model.Fim:yyyyMMdd}.pdf";

        return File(pdfBytes, "application/pdf",
            $"espelho_{model.Funcionario.Replace(' ', '_')}_{model.Inicio:yyyyMMdd}_{model.Fim:yyyyMMdd}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> Espelho(int? employeeId, DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        inicio ??= new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        fim ??= DateOnly.FromDateTime(DateTime.Today);

        var isAdmin = User.IsInRole("Admin") ||
                      string.Equals(User.FindFirstValue(ClaimTypes.Role), "Admin", StringComparison.OrdinalIgnoreCase);

        if (!isAdmin)
        {
            var empClaim = User.FindFirstValue("employee_id") ?? User.FindFirstValue("EmployeeId");
            if (int.TryParse(empClaim, out var eid))
                employeeId = eid;
        }

        var dto = await _reports.GetEspelhoAsync(inicio.Value, fim.Value, employeeId, ct);

        var vm = dto.Select(d => new EspelhoDiaViewModel
        {
            Dia = d.Dia,
            Marcas = d.Marcas
                .OrderBy(m => m.Hora)
                .Select(m => new EspelhoMarcacaoViewModel
                {
                    Tipo = m.Tipo,
                    Hora = m.Hora
                }).ToList()
        }).ToList();

        return View("Espelho", vm);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Timesheet(CancellationToken ct)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var ini = new DateOnly(hoje.Year, hoje.Month, 1);
        var fim = hoje;

        var isAdmin = User.IsInRole("Admin") ||
                      string.Equals(User.FindFirstValue(ClaimTypes.Role), "Admin",
                                    StringComparison.OrdinalIgnoreCase);

        var vm = new TimesheetFiltroViewModel
        {
            Inicio = ini,
            Fim = fim,
            IsAdmin = isAdmin
        };

        if (isAdmin)
        {
            vm.Employees.Add(new SelectListItem { Value = "", Text = "Selecione..." });
            vm.Employees.AddRange(
                _empRepo.Query()
                    .Where(e => e.Ativo && !e.IsDeleted)
                    .OrderBy(e => e.Nome)
                    .Select(e => new SelectListItem
                    {
                        Value = e.Id.ToString(),
                        Text = $"{e.Pin} - {e.Nome}"
                    })
                    .ToList()
            );
        }
        else
        {
            // Força o colaborador logado
            var empClaim = User.FindFirstValue("EmployeeId") ?? User.FindFirstValue("employee_id");
            if (int.TryParse(empClaim, out var eid))
            {
                vm.EmployeeId = eid;
                var nome = await _empRepo.Query()
                            .Where(e => e.Id == eid)
                            .Select(e => e.Nome)
                            .FirstOrDefaultAsync(ct);
                vm.EmployeeName = nome ?? "Colaborador";
                // opcional: mostra o nome no dropdown desabilitado
                vm.Employees.Add(new SelectListItem { Value = eid.ToString(), Text = vm.EmployeeName!, Selected = true });
            }
            else
            {
                return Forbid();
            }
        }

        return View("TimesheetFiltro", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Timesheet(TimesheetFiltroViewModel vm, CancellationToken ct)
    {
        if (vm.Inicio > vm.Fim)
        {
            ModelState.AddModelError(string.Empty, "A data inicial não pode ser maior que a final.");
            return await Timesheet(ct); // recarrega com dropdowns
        }

        var isAdmin = User.IsInRole("Admin") ||
                      string.Equals(User.FindFirstValue(ClaimTypes.Role), "Admin",
                                    StringComparison.OrdinalIgnoreCase);

        vm.IsAdmin = isAdmin;

        // Recarrega lista (para o caso de erro/volta)
        if (isAdmin)
        {
            vm.Employees.Clear();
            vm.Employees.Add(new SelectListItem { Value = "", Text = "Selecione..." });
            vm.Employees.AddRange(
                _empRepo.Query()
                    .Where(e => e.Ativo && !e.IsDeleted)
                    .OrderBy(e => e.Nome)
                    .Select(e => new SelectListItem
                    {
                        Value = e.Id.ToString(),
                        Text = $"{e.Pin} - {e.Nome}",
                        Selected = vm.EmployeeId.HasValue && vm.EmployeeId.Value.ToString() == e.Id.ToString()
                    })
                    .ToList()
            );
        }
        else
        {
            var empClaim = User.FindFirstValue("EmployeeId") ?? User.FindFirstValue("employee_id");
            if (int.TryParse(empClaim, out var eid))
            {
                vm.EmployeeId = eid;
                var nome = await _empRepo.Query()
                            .Where(e => e.Id == eid)
                            .Select(e => e.Nome)
                            .FirstOrDefaultAsync(ct);
                vm.EmployeeName = nome ?? "Colaborador";
                vm.Employees.Clear();
                vm.Employees.Add(new SelectListItem { Value = eid.ToString(), Text = vm.EmployeeName!, Selected = true });
            }
            else
            {
                return Forbid();
            }
        }

        // Busca nome do colaborador para mostrar no resultado (ADMIN)
        if (isAdmin && vm.EmployeeId.HasValue)
        {
            vm.EmployeeName = await _empRepo.Query()
                .Where(e => e.Id == vm.EmployeeId.Value)
                .Select(e => e.Nome)
                .FirstOrDefaultAsync(ct) ?? "Colaborador";
        }

        // Carrega as marcações (reutiliza seu IReportQueries)
        var dto = await _reports.GetEspelhoAsync(vm.Inicio, vm.Fim, vm.EmployeeId, ct);

        vm.Dias = dto.Select(d => new EspelhoDiaViewModel
        {
            Dia = d.Dia,
            Marcas = d.Marcas.Select(m => new EspelhoMarcacaoViewModel
            {
                Tipo = m.Tipo,
                Hora = m.Hora
            }).ToList()
        }).ToList();

        return View("TimesheetFiltro", vm);
    }
}
