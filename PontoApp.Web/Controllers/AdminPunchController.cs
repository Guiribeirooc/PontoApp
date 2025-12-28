using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Services;
using PontoApp.Domain.Enums;
using PontoApp.Domain.Interfaces;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Controllers
{
    [Authorize(Policy = "RequireAdmin")]
    public class AdminPunchController(IPunchService punchService, IEmployeeRepository empRepo) : Controller
    {
        private readonly IPunchService _punchService = punchService;
        private readonly IEmployeeRepository _empRepo = empRepo;

        private IEnumerable<SelectListItem> BuildEmployeeSelect(int? selectedId = null)
        {
            var items = _empRepo.Query()
                .Where(e => e.Ativo && !e.IsDeleted)
                .OrderBy(e => e.Nome)
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = $"{e.Pin} - {e.Nome}",
                    Selected = selectedId.HasValue && selectedId.Value == e.Id
                })
                .ToList();

            return items;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? employeeId = null, CancellationToken ct = default)
        {
            if (employeeId.HasValue)
            {
                var exists = await _empRepo.Query()
                    .AnyAsync(e => e.Id == employeeId.Value && e.Ativo && !e.IsDeleted, ct);
                if (!exists) return NotFound("Colaborador não encontrado ou inativo.");
            }

            var nowLocal = DateTime.Now;
            var vm = new AdminManualPunchViewModel
            {
                EmployeeId = employeeId,
                DataHoraLocal = nowLocal.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
                Tipo = PunchType.Entrada,
                Employees = BuildEmployeeSelect(employeeId)
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminManualPunchViewModel vm, CancellationToken ct)
        {
            if (!vm.EmployeeId.HasValue)
                ModelState.AddModelError(nameof(vm.EmployeeId), "Selecione um colaborador.");

            if (vm.EmployeeId.HasValue)
            {
                var emp = await _empRepo.GetByIdAsync(vm.EmployeeId.Value, ct);
                if (emp is null || !emp.Ativo || emp.IsDeleted)
                    ModelState.AddModelError(nameof(vm.EmployeeId), "Colaborador inválido ou inativo.");
            }

            if (!Enum.IsDefined(typeof(PunchType), vm.Tipo))
                ModelState.AddModelError(nameof(vm.Tipo), "Tipo de batida inválido.");

            DateTime parsedLocal;

            if (string.IsNullOrWhiteSpace(vm.DataHoraLocal) ||
                !DateTime.TryParseExact(
                    vm.DataHoraLocal,
                    "yyyy-MM-dd'T'HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out parsedLocal))
            {
                ModelState.AddModelError(nameof(vm.DataHoraLocal), "Data/hora inválida.");
                vm.Employees = BuildEmployeeSelect(vm.EmployeeId);
                return View(vm);
            }

            if (!ModelState.IsValid)
            {
                vm.Employees = BuildEmployeeSelect(vm.EmployeeId);
                return View(vm);
            }

            var localDt = DateTime.SpecifyKind(parsedLocal, DateTimeKind.Unspecified);

            try
            {
                await _punchService.MarcarManualAsync(
                    employeeId: vm.EmployeeId!.Value,
                    tipo: vm.Tipo,
                    dataHora: localDt,
                    justificativa: vm.Justificativa!,
                    ct: ct);

                TempData["ok"] = "Marcação manual registrada.";

                var diaLocal = DateOnly.FromDateTime(localDt.Date);
                return RedirectToAction(nameof(Periodo),
                    new { employeeId = vm.EmployeeId!.Value, inicio = diaLocal, fim = diaLocal });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (DbUpdateException dbx)
            {
                ModelState.AddModelError(string.Empty, $"Erro de persistência: {dbx.GetBaseException().Message}");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Erro inesperado: {ex.Message}");
            }

            vm.Employees = BuildEmployeeSelect(vm.EmployeeId);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Periodo(int employeeId, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken ct = default)
        {
            var emp = await _empRepo.GetByIdAsync(employeeId, ct);
            if (emp is null || emp.IsDeleted) return NotFound("Colaborador não encontrado.");

            var ini = inicio ?? DateOnly.FromDateTime(DateTime.Today);
            var end = fim ?? ini;

            var dados = await _punchService.ListarPeriodoAsync(ini, end, employeeId, ct);
            ViewBag.EmployeeId = employeeId;
            ViewBag.Inicio = ini;
            ViewBag.Fim = end;
            return View(dados);
        }

        [HttpGet]
        public async Task<IActionResult> Dia(DateOnly? dia, int? employeeId, CancellationToken ct)
        {
            var d = dia ?? DateOnly.FromDateTime(DateTime.Today);

            ViewBag.Employees = BuildEmployeeSelectWithAll(employeeId);

            if (employeeId.HasValue)
            {
                var ok = await _empRepo.Query()
                    .AnyAsync(e => e.Id == employeeId.Value && e.Ativo && !e.IsDeleted, ct);

                if (!ok)
                {
                    ModelState.AddModelError(string.Empty, "Colaborador inválido ou inativo.");
                    var allList = await _punchService.GetAllDoDiaAsync(d, ct);
                    // Reaproveita a view antiga
                    return View("~/Views/Punch/Dia.cshtml", allList);
                }

                var listByEmp = await _punchService.ListarDoDiaAsync(d, employeeId, ct);
                return View("~/Views/Punch/Dia.cshtml", listByEmp);
            }

            var listAll = await _punchService.GetAllDoDiaAsync(d, ct);
            return View("~/Views/Punch/Dia.cshtml", listAll);
        }

        [HttpGet]
        public IActionResult Mes()
        {
            var (ini,_) = GetPeriodoPadrao();
            ViewBag.Ano = ini.Year;
            ViewBag.Mes = ini.Month;

            ViewBag.Employees = new SelectList(
                _empRepo.Query()
                    .Where(e => e.Ativo && !e.IsDeleted)
                    .OrderBy(e => e.Nome)
                    .Select(e => new { e.Id, Nome = $"{e.Pin} - {e.Nome}" }),
                "Id", "Nome"
            );

            // Reaproveita a view antiga (que tinha dropdown)
            return View("~/Views/Punch/Mes.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Mes(int ano, int mes, int? employeeId, CancellationToken ct)
        {
            var inicio = new DateOnly(ano, mes, 1);
            var fim = inicio.AddMonths(1).AddDays(-1);

            if (employeeId.HasValue)
            {
                var ok = await _empRepo.Query()
                    .AnyAsync(e => e.Id == employeeId.Value && e.Ativo && !e.IsDeleted, ct);

                if (!ok)
                {
                    TempData["error"] = "Colaborador inválido ou inativo.";
                    return RedirectToAction(nameof(Mes));
                }
            }

            var dados = await _punchService.ListarPeriodoAsync(inicio, fim, employeeId, ct);
            return View("~/Views/Punch/MesResultado.cshtml", dados);
        }


        private List<SelectListItem> BuildEmployeeSelectWithAll(int? selectedId)
        {
            var items = new List<SelectListItem>
            {
                new() { Value = "", Text = "Todos", Selected = selectedId == null }
            };

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

        private static (DateOnly Inicio, DateOnly Fim) GetPeriodoPadrao()
        {
            var hoje = DateTime.Today;
            var inicio = new DateOnly(hoje.Year, hoje.Month, 1);
            var fim = inicio.AddMonths(1).AddDays(-1);
            return (inicio, fim);
        }
    }
}
