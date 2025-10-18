using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PontoApp.Domain.Entities;
using PontoApp.Domain.Enums;
using PontoApp.Domain.Interfaces;
using PontoApp.Infrastructure.EF;
using PontoApp.Web.Utils;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
public class EmployeeController(IEmployeeRepository repo, IPunchRepository punchRepo, IWebHostEnvironment env, AppDbContext db) : Controller
{
    private readonly AppDbContext _db = db;
    private readonly IEmployeeRepository _repo = repo;
    private readonly IPunchRepository _punchRepo = punchRepo;
    private readonly IWebHostEnvironment _env = env;
    private const long MaxPhotoBytes = 2 * 1024 * 1024;
    private static readonly HashSet<string> PhotoExtWhitelist = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp" };

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var list = await _repo.Query()
                              .OrderBy(e => e.Nome)
                              .ToListAsync(ct);
        return View(list);
    }

    [HttpGet]
    public IActionResult Create() => View(new EmployeeFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel vm, CancellationToken ct)
    {
        if (vm is null) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        vm.Nome = (vm.Nome ?? string.Empty).Trim();
        vm.Pin = (vm.Pin ?? string.Empty).Trim();
        vm.Email = (vm.Email ?? string.Empty).Trim();
        vm.Cpf = CpfUtils.OnlyDigits(vm.Cpf ?? string.Empty);

        if (vm.Jornada == WorkScheduleKind.Intermitente)
        {
            vm.ShiftStart = null;
            vm.ShiftEnd = null;
        }

        var deleted = await _repo.QueryAll()
            .FirstOrDefaultAsync(e =>
                e.IsDeleted &&
                (e.Pin == vm.Pin || e.Cpf == vm.Cpf || e.Pin.EndsWith(":" + vm.Pin)), ct);

        if (deleted is not null)
        {
            deleted.IsDeleted = false;
            deleted.DeletedAt = null;
            deleted.Nome = vm.Nome;
            deleted.Ativo = vm.Ativo;
            deleted.Pin = vm.Pin;
            deleted.Cpf = vm.Cpf;
            deleted.Email = vm.Email;
            deleted.BirthDate = vm.BirthDate;
            deleted.Jornada = vm.Jornada;
            deleted.ShiftStart = ParseTimeOrNull(vm.ShiftStart);
            deleted.ShiftEnd = ParseTimeOrNull(vm.ShiftEnd);
            deleted.Phone = vm.Phone?.Trim();
            deleted.NisPis = vm.NisPis?.Trim();
            deleted.City = vm.City?.Trim();
            deleted.State = vm.State?.Trim();
            deleted.Departamento = vm.Departamento?.Trim();
            deleted.Cargo = vm.Cargo?.Trim();
            deleted.Matricula = vm.Matricula?.Trim();
            deleted.HourlyRate = vm.HourlyRate;
            deleted.AdmissionDate = vm.AdmissionDate;
            deleted.HasTimeBank = vm.HasTimeBank;
            deleted.TrackingStart = vm.TrackingStart;
            deleted.TrackingEnd = vm.TrackingEnd;
            deleted.VacationAccrualStart = vm.VacationAccrualStart;
            deleted.ManagerName = vm.ManagerName?.Trim();
            deleted.EmployerName = vm.EmployerName?.Trim();
            deleted.UnitName = vm.UnitName?.Trim();

            if (vm.Foto is { Length: > 0 })
            {
                if (!ValidatePhoto(vm.Foto, out var err))
                {
                    ModelState.AddModelError(nameof(vm.Foto), err!);
                    return View(vm);
                }
                if (!string.IsNullOrWhiteSpace(deleted.PhotoPath))
                    ExcluirArquivoFisico(deleted.PhotoPath);

                deleted.PhotoPath = await SalvarFotoAsync(vm.Foto, ct);
            }

            await _repo.UpdateAsync(deleted, ct);
            await _repo.SaveChangesAsync(ct);

            TempData["ok"] = "Colaborador(a) restaurado(a) e atualizado(a).";
            return RedirectToAction(nameof(Index));
        }

        if (await _repo.Query().AnyAsync(e => e.Pin == vm.Pin, ct))
        {
            ModelState.AddModelError(nameof(vm.Pin), "Já existe um(a) colaborador(a) ativo(a) com esse PIN.");
            return View(vm);
        }
        if (!string.IsNullOrEmpty(vm.Cpf) && await _repo.Query().AnyAsync(e => e.Cpf == vm.Cpf, ct))
        {
            ModelState.AddModelError(nameof(vm.Cpf), "Já existe um(a) colaborador(a) ativo(a) com este CPF.");
            return View(vm);
        }

        var emp = new Employee
        {
            Nome = vm.Nome,
            Pin = vm.Pin,
            Cpf = vm.Cpf,
            Email = vm.Email,
            BirthDate = vm.BirthDate,
            Ativo = vm.Ativo,
            Jornada = vm.Jornada,
            ShiftStart = ParseTimeOrNull(vm.ShiftStart),
            ShiftEnd = ParseTimeOrNull(vm.ShiftEnd),
            Phone = vm.Phone?.Trim(),
            NisPis = vm.NisPis?.Trim(),
            City = vm.City?.Trim(),
            State = vm.State?.Trim(),
            Departamento = vm.Departamento?.Trim(),
            Cargo = vm.Cargo?.Trim(),
            Matricula = vm.Matricula?.Trim(),
            HourlyRate = vm.HourlyRate,
            AdmissionDate = vm.AdmissionDate,
            HasTimeBank = vm.HasTimeBank,
            TrackingStart = vm.TrackingStart,
            TrackingEnd = vm.TrackingEnd,
            VacationAccrualStart = vm.VacationAccrualStart,
            ManagerName = vm.ManagerName?.Trim(),
            EmployerName = vm.EmployerName?.Trim(),
            UnitName = vm.UnitName?.Trim()
        };

        if (vm.Foto is { Length: > 0 })
        {
            if (!ValidatePhoto(vm.Foto, out var errMsg))
            {
                ModelState.AddModelError(nameof(vm.Foto), errMsg!);
                return View(vm);
            }
            emp.PhotoPath = await SalvarFotoAsync(vm.Foto, ct);
        }

        await _repo.AddAsync(emp, ct);
        await _repo.SaveChangesAsync(ct);

        TempData["ok"] = "Colaborador(a) cadastrado(a) com sucesso.";
        return RedirectToAction(nameof(Index));
    }


    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var emp = await _repo.GetByIdAsync(id, ct);
        if (emp is null) return NotFound();

        var vm = new EmployeeFormViewModel
        {
            Id = emp.Id,
            Nome = emp.Nome,
            Pin = emp.Pin,
            Cpf = emp.Cpf,
            Email = emp.Email,
            BirthDate = emp.BirthDate,
            Ativo = emp.Ativo,
            FotoAtualPath = emp.PhotoPath,
            ShiftStart = emp.ShiftStart?.ToString("HH:mm"),
            ShiftEnd = emp.ShiftEnd?.ToString("HH:mm"),
            Jornada = emp.Jornada,
            Phone = emp.Phone,
            NisPis = emp.NisPis,
            City = emp.City,
            State = emp.State,
            Departamento = emp.Departamento,
            Cargo = emp.Cargo,
            Matricula = emp.Matricula,
            HourlyRate = emp.HourlyRate,
            AdmissionDate = emp.AdmissionDate,
            HasTimeBank = emp.HasTimeBank,
            TrackingStart = emp.TrackingStart,
            TrackingEnd = emp.TrackingEnd,
            VacationAccrualStart = emp.VacationAccrualStart,
            ManagerName = emp.ManagerName,
            EmployerName = emp.EmployerName,
            UnitName = emp.UnitName
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmployeeFormViewModel vm, CancellationToken ct)
    {
        //if (!ModelState.IsValid)
        //{
        //    var allErrors = ModelState
        //        .Where(kv => kv.Value.Errors.Count > 0)
        //        .Select(kv => new
        //        {
        //            Field = string.IsNullOrEmpty(kv.Key) ? "(model)" : kv.Key,
        //            Errors = kv.Value.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)
        //        })
        //        .ToList();

        //    // logue/inspecione no debug
        //    System.Diagnostics.Debug.WriteLine(
        //        string.Join(Environment.NewLine, allErrors.Select(e => $"{e.Field}: {string.Join(" | ", e.Errors)}")));
        //}

        if (!ModelState.IsValid) return View(vm);

        var emp = await _repo.GetByIdAsync(vm.Id!.Value, ct);
        if (emp is null) return NotFound();

        var newPin = (vm.Pin ?? "").Trim();
        var newCpf = (vm.Cpf ?? "").Trim();

        if (await _repo.Query().AnyAsync(e => e.Id != emp.Id && e.Pin == newPin, ct))
        {
            ModelState.AddModelError(nameof(vm.Pin), "Já existe outro(a) colaborador(a) com esse PIN.");
            return View(vm);
        }
        if (await _repo.Query().AnyAsync(e => e.Id != emp.Id && e.Cpf == newCpf, ct))
        {
            ModelState.AddModelError(nameof(vm.Cpf), "Já existe outro(a) colaborador(a) com este CPF.");
            return View(vm);
        }

        emp.Nome = (vm.Nome ?? "").Trim();
        emp.Pin = newPin;
        emp.Cpf = newCpf;
        emp.Email = (vm.Email ?? "").Trim();
        emp.BirthDate = vm.BirthDate;
        emp.Ativo = vm.Ativo;
        emp.ShiftStart = ParseTimeOrNull(vm.ShiftStart);
        emp.ShiftEnd = ParseTimeOrNull(vm.ShiftEnd);
        emp.Jornada = vm.Jornada;
        emp.Phone = vm.Phone?.Trim();
        emp.NisPis = vm.NisPis?.Trim();
        emp.City = vm.City?.Trim();
        emp.State = vm.State?.Trim();
        emp.Departamento = vm.Departamento?.Trim();
        emp.Cargo = vm.Cargo?.Trim();
        emp.Matricula = vm.Matricula?.Trim();
        emp.HourlyRate = vm.HourlyRate;
        emp.AdmissionDate = vm.AdmissionDate;
        emp.HasTimeBank = vm.HasTimeBank;
        emp.TrackingStart = vm.TrackingStart;
        emp.TrackingEnd = vm.TrackingEnd;
        emp.VacationAccrualStart = vm.VacationAccrualStart;
        emp.ManagerName = vm.ManagerName?.Trim();
        emp.EmployerName = vm.EmployerName?.Trim();
        emp.UnitName = vm.UnitName?.Trim();

        if (vm.Foto is { Length: > 0 })
        {
            if (!ValidatePhoto(vm.Foto, out var err))
            {
                ModelState.AddModelError(nameof(vm.Foto), err!);
                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(emp.PhotoPath))
                ExcluirArquivoFisico(emp.PhotoPath);

            emp.PhotoPath = await SalvarFotoAsync(vm.Foto, ct);
        }

        await _repo.UpdateAsync(emp, ct);
        await _repo.SaveChangesAsync(ct);

        TempData["ok"] = "Colaborador(a) atualizado(a).";
        return RedirectToAction(nameof(Index));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var emp = await _repo.GetByIdAsync(id, ct);
        if (emp is null) return NotFound();

        emp.Ativo = !emp.Ativo;
        await _repo.UpdateAsync(emp, ct);
        await _repo.SaveChangesAsync(ct);

        TempData["ok"] = emp.Ativo
            ? "Colaborador(a) reativado(a) com sucesso."
            : "Colaborador(a) desativado(a) com sucesso.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var emp = await _repo.GetByIdAsync(id, ct);
        if (emp is null) return NotFound();

        var temPonto = await _punchRepo.Query().AnyAsync(p => p.EmployeeId == id, ct);
        if (temPonto)
        {
            TempData["err"] = "Não é possível excluir: o(a) colaborador(a) possui registros de ponto. Desative-o(a) em vez disso.";
            return RedirectToAction(nameof(Index));
        }

        emp.IsDeleted = true;
        emp.DeletedAt = DateTime.Now;
        if (!string.IsNullOrEmpty(emp.Pin))
            emp.Pin = $"{emp.Pin}:{DateTime.Now}";

        await _repo.UpdateAsync(emp, ct);
        await _repo.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(emp.PhotoPath))
            ExcluirArquivoFisico(emp.PhotoPath);

        TempData["ok"] = "Colaborador(a) removido(a).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var e = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return NotFound();

        var vm = new EmployeeDetailsViewModel
        {
            Id = e.Id,
            Nome = e.Nome,
            PhotoUrl = string.IsNullOrWhiteSpace(e.PhotoPath) ? null : e.PhotoPath,
            Cpf = e.Cpf,
            BirthDate = e.BirthDate,
            Email = e.Email,
            Phone = e.Phone,
            NisPis = e.NisPis,
            Cidade = e.City,
            Estado = e.State,
            Departamento = e.Departamento,
            Cargo = e.Cargo,
            Matricula = e.Matricula,
            ValorHora = e.HourlyRate,
            Admissao = e.AdmissionDate,
            BancoHorasHabilitado = e.HasTimeBank,
            InicioRegistro = e.TrackingStart,
            FimRegistro = e.TrackingEnd,
            InicioAquisitivoFerias = e.VacationAccrualStart,
            Gestor = e.ManagerName,
            Empregador = e.EmployerName,
            Unidade = e.UnitName,
            Jornada = e.Jornada,
            ShiftStart = e.ShiftStart,
            ShiftEnd = e.ShiftEnd,
            TimeZoneDisplay = "São Paulo, Brasil"
        };

        return View(vm);
    }


    private static bool ValidatePhoto(IFormFile foto, out string? error)
    {
        error = null;
        var ext = Path.GetExtension(foto.FileName);
        if (!PhotoExtWhitelist.Contains(ext))
        {
            error = "Formato de imagem não permitido. Use JPG, PNG ou WEBP.";
            return false;
        }
        var ct = foto.ContentType?.ToLowerInvariant();
        if (ct is null || (!ct.StartsWith("image/jpeg") && !ct.StartsWith("image/png") && !ct.StartsWith("image/webp")))
        {
            error = "Tipo de conteúdo inválido para imagem.";
            return false;
        }
        if (foto.Length > MaxPhotoBytes)
        {
            error = "A imagem excede o tamanho máximo (2 MB).";
            return false;
        }
        return true;
    }

    private async Task<string> SalvarFotoAsync(IFormFile foto, CancellationToken ct)
    {
        var folder = Path.Combine(_env.WebRootPath, "img", "employees");
        Directory.CreateDirectory(folder);

        var ext = Path.GetExtension(foto.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(folder, fileName);

        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await foto.CopyToAsync(fs, ct);

        return "/img/employees/" + fileName;
    }

    private void ExcluirArquivoFisico(string publicPath)
    {
        try
        {
            var full = Path.Combine(_env.WebRootPath, publicPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }
        catch { /* ignore */ }
    }

    private static TimeOnly? ParseTimeOrNull(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return TimeOnly.TryParse(s, out var t) ? t : null;
    }
}
