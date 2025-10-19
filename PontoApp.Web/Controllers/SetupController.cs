using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PontoApp.Application.Contracts;
using PontoApp.Application.DTOs;
using PontoApp.Application.Services;
using PontoApp.Web.Support;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Controllers;

[AllowAnonymous]
public class SetupController(
    SetupOrchestrator setup,
    IInviteService invites,
    IAuthService auth,
    IOptions<SetupOptions> options) : Controller
{
    private readonly SetupOrchestrator _setup = setup;
    private readonly IInviteService _invites = invites;
    private readonly IAuthService _auth = auth;
    private readonly IOptions<SetupOptions> _options = options;

    // GET /setup?token=...
    [HttpGet("/setup")]
    public async Task<IActionResult> Setup([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return NotFound();

        var ok = await _invites.ValidateAsync(token);
        if (!ok)
            return View("SetupInvalid"); // crie uma view simples informando convite inválido/expirado

        return View(new SetupEmpresaAdminViewModel { Token = token });
    }

    // POST /setup
    [ValidateAntiForgeryToken]
    [HttpPost("/setup")]
    public async Task<IActionResult> Setup(SetupEmpresaAdminViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(vm);

        // monta DTOs
        var companyDto = new CreateCompanyDto(
            vm.RazaoSocial.Trim(), vm.CNPJ, vm.IE, vm.IM,
            vm.Logradouro, vm.Numero, vm.Complemento,
            vm.Bairro, vm.Cidade, vm.UF, vm.CEP, vm.Pais,
            vm.Telefone, vm.EmailContato?.Trim().ToLowerInvariant(),
            await vm.Logo.ToBytesAsync()
        );

        var adminDto = new CreateUserAdminDto(
            CompanyId: 0, // o orchestrator vai injetar o novo Id
            Name: vm.AdminNome.Trim(),
            Email: vm.AdminEmail.Trim().ToLowerInvariant(),
            Password: vm.AdminSenha
        );

        try
        {
            // orquestra: valida/consome token, cria empresa e admin em transação
            var companyId = await _setup.RunAsync(vm.Token, companyDto, adminDto, ct);

            // autentica o admin recém-criado
            var login = await _auth.ValidateCredentialsAsync(vm.AdminEmail, vm.AdminSenha, ct);
            if (login is not null && login.Value.UserId.HasValue)
            {
                var principal = _auth.BuildPrincipal(
                    login.Value.UserId.Value,
                    login.Value.CompanyId,
                    login.Value.Name,
                    login.Value.Roles
                );
                await HttpContext.SignInAsync("Cookies", principal);
                return Redirect("/empresa/home");
            }

            // fallback: se não autenticou por algum motivo
            TempData["setup_ok"] = "Empresa e administrador criados com sucesso. Faça login.";
            return Redirect("/login");
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
    public async Task<IActionResult> CreateInvite([FromQuery] int validityHours = 24, [FromQuery] int maxUses = 1)
    {
        // verificação simples por chave (não expõe UI)
        if (!Request.Headers.TryGetValue("X-MasterKey", out var provided) ||
            string.IsNullOrWhiteSpace(_options.Value.MasterKey) ||
            !string.Equals(provided.ToString(), _options.Value.MasterKey, StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        var dto = await _invites.CreateAdminInviteAsync(TimeSpan.FromHours(validityHours), maxUses);
        var link = $"{Request.Scheme}://{Request.Host}/setup?token={dto.Token}";
        return Ok(new { link, expiresAtUtc = dto.ExpiresAtUtc });
    }
}

/// <summary> Opções do Setup lidas do appsettings </summary>
public sealed class SetupOptions
{
    public string MasterKey { get; set; } = ""; // appsettings: "Setup": { "MasterKey": "..." }
}
