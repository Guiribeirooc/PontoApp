using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PontoApp.Application.Contracts;
using PontoApp.Application.DTOs;
using PontoApp.Application.Services;
using PontoApp.Web.Support;
using PontoApp.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace PontoApp.Web.Controllers;

[AllowAnonymous]
public class SetupController(
    SetupOrchestrator setup,
    IInviteService invites,
    IOptions<SetupOptions> options,
    IWebHostEnvironment env) : Controller
{
    private static readonly HashSet<string> PhotoExtWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private const long MaxPhotoBytes = 2 * 1024 * 1024; // 2 MB

    private readonly SetupOrchestrator _setup = setup;
    private readonly IInviteService _invites = invites;
    private readonly IOptions<SetupOptions> _options = options;
    private readonly IWebHostEnvironment _env = env;

    // GET /setup?token=...
    [HttpGet("/setup")]
    public async Task<IActionResult> Setup([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return NotFound();

        var invite = await _invites.GetDetailsAsync(token);
        if (invite is null || invite.IsConsumed || invite.ExpiresAtUtc < DateTime.UtcNow)
            return View("SetupInvalid");

        return View(new SetupEmpresaAdminViewModel
        {
            Token = token,
            RazaoSocial = invite.CompanyName,
            CNPJ = invite.CompanyDocument
        });
    }

    // POST /setup
    [ValidateAntiForgeryToken]
    [HttpPost("/setup")]
    public async Task<IActionResult> Setup(SetupEmpresaAdminViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var invite = await _invites.GetDetailsAsync(vm.Token, ct);
        if (invite is null || invite.IsConsumed || invite.ExpiresAtUtc < DateTime.UtcNow)
            return View("SetupInvalid");

        if (!string.Equals(OnlyDigits(vm.CNPJ), invite.CompanyDocument, StringComparison.Ordinal))
            ModelState.AddModelError(nameof(vm.CNPJ), "CNPJ não corresponde ao convite emitido.");

        if (!string.Equals(vm.RazaoSocial.Trim(), invite.CompanyName, StringComparison.Ordinal))
            ModelState.AddModelError(nameof(vm.RazaoSocial), "Nome da empresa não corresponde ao convite emitido.");

        if (!vm.AdminNascimento.HasValue)
            ModelState.AddModelError(nameof(vm.AdminNascimento), "Informe a data de nascimento.");

        if (!ModelState.IsValid)
            return View(vm);

        if (vm.AdminFoto is { Length: > 0 } && !ValidatePhoto(vm.AdminFoto, out var photoError))
        {
            ModelState.AddModelError(nameof(vm.AdminFoto), photoError);
            return View(vm);
        }

        var logoBytes = await vm.Logo.ToBytesAsync(ct);
        string? photoPath = null;
        if (vm.AdminFoto is { Length: > 0 })
            photoPath = await SavePhotoAsync(vm.AdminFoto, ct);

        var dto = new OnboardingCreateDto
        {
            CompanyName = invite.CompanyName,
            CompanyDocument = invite.CompanyDocument,
            CompanyIE = vm.IE,
            CompanyIM = vm.IM,
            CompanyLogradouro = vm.Logradouro,
            CompanyNumero = vm.Numero,
            CompanyComplemento = vm.Complemento,
            CompanyBairro = vm.Bairro,
            CompanyCidade = vm.Cidade,
            CompanyUF = vm.UF,
            CompanyCEP = vm.CEP,
            CompanyPais = vm.Pais,
            CompanyTelefone = vm.Telefone,
            CompanyEmailContato = vm.EmailContato?.Trim().ToLowerInvariant(),
            CompanyLogo = logoBytes,
            AdminName = vm.AdminNome.Trim(),
            AdminCpf = vm.AdminCPF,
            AdminEmail = vm.AdminEmail.Trim().ToLowerInvariant(),
            AdminBirthDate = DateOnly.FromDateTime(vm.AdminNascimento!.Value.Date),
            AdminPhone = vm.AdminTelefone,
            AdminDepartment = vm.AdminDepartamento,
            AdminRole = vm.AdminCargo,
            AdminMatricula = vm.AdminMatricula,
            AdminPhotoPath = photoPath,
            AdminPassword = vm.AdminSenha
        };

        try
        {
            await _setup.RunAsync(vm.Token, dto, ct);

            TempData["setup_ok"] = "Empresa e administrador criados com sucesso. Faça login com suas credenciais.";
            return RedirectToAction("Login", "Account");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    // POST /internal/setup/invite  (gera link /setup?token=...)
    // Protegido por MasterKey no header: X-MasterKey: <valor do appsettings>
    [HttpPost("/internal/setup/invite")]
    public async Task<IActionResult> CreateInvite([FromQuery] string companyName, [FromQuery] string companyDocument, [FromQuery] int validityHours = 24, [FromQuery] int maxUses = 1)
    {
        // verificação simples por chave (não expõe UI)
        if (!Request.Headers.TryGetValue("X-MasterKey", out var provided) ||
            string.IsNullOrWhiteSpace(_options.Value.MasterKey) ||
            !string.Equals(provided.ToString(), _options.Value.MasterKey, StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(companyDocument))
            return BadRequest("Informe o nome e o CNPJ da empresa.");

        var dto = await _invites.CreateAdminInviteAsync(companyName.Trim(), companyDocument, TimeSpan.FromHours(validityHours), maxUses);
        var link = $"{Request.Scheme}://{Request.Host}/setup?token={dto.Token}";
        return Ok(new { link, expiresAtUtc = dto.ExpiresAtUtc });
    }

    private static string OnlyDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return new string(value.Where(char.IsDigit).ToArray());
    }

    private static bool ValidatePhoto(IFormFile file, out string error)
    {
        error = string.Empty;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!PhotoExtWhitelist.Contains(ext))
        {
            error = "Formato de imagem não permitido. Use JPG, PNG ou WEBP.";
            return false;
        }

        var ct = file.ContentType?.ToLowerInvariant();
        if (ct is null || (!ct.StartsWith("image/jpeg") && !ct.StartsWith("image/png") && !ct.StartsWith("image/webp")))
        {
            error = "Tipo de conteúdo inválido para imagem.";
            return false;
        }

        if (file.Length > MaxPhotoBytes)
        {
            error = "A imagem excede o tamanho máximo (2 MB).";
            return false;
        }

        return true;
    }

    private async Task<string> SavePhotoAsync(IFormFile foto, CancellationToken ct)
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
}

/// <summary> Opções do Setup lidas do appsettings </summary>
public sealed class SetupOptions
{
    public string MasterKey { get; set; } = ""; // appsettings: "Setup": { "MasterKey": "..." }
}
