using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PontoApp.Domain.Entities;
using PontoApp.Domain.Interfaces;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
public class EmployeeController : Controller
{
    private readonly IEmployeeRepository _repo;
    private readonly IPunchRepository _punchRepo;
    private readonly IWebHostEnvironment _env;
    private const long MaxPhotoBytes = 2 * 1024 * 1024;
    private static readonly HashSet<string> PhotoExtWhitelist = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp" };

    public EmployeeController(IEmployeeRepository repo, IPunchRepository punchRepo, IWebHostEnvironment env)
    {
        _repo = repo;
        _punchRepo = punchRepo;
        _env = env;
    }

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
        if (!ModelState.IsValid) return View(vm);

        vm.Nome = vm.Nome?.Trim() ?? "";
        vm.Pin = vm.Pin?.Trim() ?? "";
        vm.Cpf = vm.Cpf?.Trim() ?? "";
        vm.Email = vm.Email?.Trim() ?? "";

        var deleted = await _repo.QueryAll()
                                 .FirstOrDefaultAsync(e =>
                                      e.IsDeleted &&
                                      (e.Pin == vm.Pin || e.Pin.EndsWith(":" + vm.Pin)), ct);
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
            deleted.ShiftStart = ParseTimeOrNull(vm.ShiftStart);
            deleted.ShiftEnd = ParseTimeOrNull(vm.ShiftEnd);

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
        if (await _repo.Query().AnyAsync(e => e.Cpf == vm.Cpf, ct))
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
            ShiftStart = ParseTimeOrNull(vm.ShiftStart),
            ShiftEnd = ParseTimeOrNull(vm.ShiftEnd)
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
            ShiftEnd = emp.ShiftEnd?.ToString("HH:mm")
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmployeeFormViewModel vm, CancellationToken ct)
    {
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
        emp.DeletedAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrEmpty(emp.Pin))
            emp.Pin = $"{emp.Pin}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        await _repo.UpdateAsync(emp, ct);
        await _repo.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(emp.PhotoPath))
            ExcluirArquivoFisico(emp.PhotoPath);

        TempData["ok"] = "Colaborador(a) removido(a).";
        return RedirectToAction(nameof(Index));
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
