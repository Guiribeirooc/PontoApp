using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PontoApp.Application.DTOs;
using PontoApp.Application.Services;
using PontoApp.Domain.Interfaces;
using PontoApp.Infrastructure.Repositories;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
public class ReportController(IReportService report, IEmployeeRepository empRepo, IPunchRepository punchRepo) : Controller
{
    private readonly IReportService _report = report;
    private readonly IEmployeeRepository _empRepo = empRepo;
    private readonly IPunchRepository _punchRepo = punchRepo;

    [HttpGet]
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Periodo(DateOnly inicio, DateOnly fim, int? employeeId)
    {
        if (inicio > fim)
        {
            ModelState.AddModelError(string.Empty, "A data inicial não pode ser maior que a final.");
            return await RecarregarPeriodoComErro(inicio, fim);
        }

        var resumo = await _report.ResumoAsync(inicio, fim, employeeId);
        return View("PeriodoResultado", resumo);
    }

    [HttpGet]
    public async Task<IActionResult> Excel(DateOnly inicio, DateOnly fim, int? employeeId)
    {
        var resumo = await _report.ResumoAsync(inicio, fim, employeeId);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Relatório");

        ws.Cell(1, 1).SetValue("Dia");
        ws.Cell(1, 2).SetValue("Colaborador");
        ws.Cell(1, 3).SetValue("Entrada");
        ws.Cell(1, 4).SetValue("Saída");
        ws.Cell(1, 5).SetValue("Saída Almoço");
        ws.Cell(1, 6).SetValue("Volta Almoço");
        ws.Cell(1, 7).SetValue("Duração (min)");

        int row = 2;

        IEnumerable<WorkDayDto> dias = resumo.Dias ?? Enumerable.Empty<WorkDayDto>();

        foreach (WorkDayDto d in dias.OrderBy(d => d.Dia).ThenBy(d => d.EmployeeId))
        {
            bool first = true;

            foreach (WorkIntervalDto i in d.Intervalos.OrderBy(ii => ii.In))
            {
                ws.Cell(row, 1).SetValue(d.Dia.ToString("yyyy-MM-dd"));
                ws.Cell(row, 2).SetValue(d.EmployeeNome);
                ws.Cell(row, 3).SetValue(i.In.ToLocalTime().ToString("HH:mm"));
                ws.Cell(row, 4).SetValue(i.Out?.ToLocalTime().ToString("HH:mm") ?? "");
                ws.Cell(row, 5).SetValue(first && d.SaidaAlmoco != null ? d.SaidaAlmoco.Value.ToLocalTime().ToString("HH:mm") : "");
                ws.Cell(row, 6).SetValue(first && d.VoltaAlmoco != null ? d.VoltaAlmoco.Value.ToLocalTime().ToString("HH:mm") : "");
                ws.Cell(row, 7).SetValue(i.Out == null ? "" : ((int)i.Duration.TotalMinutes).ToString());

                row++;
                first = false;
            }
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var content = ms.ToArray();

        return File(content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"relatorio_{inicio:yyyyMMdd}_{fim:yyyyMMdd}.xlsx");
    }

    private async Task<ViewResult> RecarregarPeriodoComErro(DateOnly inicio, DateOnly fim)
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
    public async Task<IActionResult> Espelho(int id, DateOnly? de, DateOnly? ate, CancellationToken ct)
    {
        var start = de ?? new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        var end = ate ?? start.AddMonths(1).AddDays(-1);

        var fromDt = start.ToDateTime(TimeOnly.MinValue);
        var toDt = end.ToDateTime(TimeOnly.MaxValue);

        var punches = await _punchRepo.Query()
            .Where(p => p.EmployeeId == id
                     && p.DataHora.LocalDateTime >= fromDt
                     && p.DataHora.LocalDateTime <= toDt)
            .OrderBy(p => p.DataHora)
            .ToListAsync(ct);

        var dias = punches
            .GroupBy(p => DateOnly.FromDateTime(p.DataHora.LocalDateTime))
            .OrderBy(g => g.Key)
            .Select(g => new EspelhoDiaViewModel
            {
                Dia = g.Key,
                Marcas = g.Select(m => new MarcaVm
                {
                    Tipo = m.Tipo,
                    Hora = m.DataHora.LocalDateTime
                }).ToList()
            })
            .ToList();

        return View(dias);
    }
}
