using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PontoApp.Application.Contracts;
using PontoApp.Web.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PontoApp.Web.Controllers
{
    [AllowAnonymous]
    [Route("onboarding")]
    public class OnboardingController : Controller
    {
        private readonly IInviteService _invites;
        private readonly IOptions<SetupOptions>? _options;

        public OnboardingController(IInviteService invites, IOptions<SetupOptions>? options = null)
        {
            _invites = invites ?? throw new ArgumentNullException(nameof(invites));
            _options = options; // pode ser nulo em ambientes que não usam MasterKey
        }

        [HttpGet("")]
        [HttpGet("create")]
        public IActionResult Invite()
            => View("~/Views/Onboarding/Invite.cshtml", new CompanyInviteViewModel());

        [HttpPost("")]
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite(CompanyInviteViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Onboarding/Invite.cshtml", vm);

            // 🔐 Validação opcional por MasterKey (se configurada)
            var masterKey = _options?.Value?.MasterKey;
            if (!string.IsNullOrWhiteSpace(masterKey))
            {
                if (!string.Equals(vm.AccessKey, masterKey, StringComparison.Ordinal))
                {
                    ModelState.AddModelError(nameof(vm.AccessKey), "Chave de acesso inválida.");
                    return View("~/Views/Onboarding/Invite.cshtml", vm);
                }
            }

            var validity = TimeSpan.FromHours(vm.ValidityHours ?? 48);
            var maxUses = vm.MaxUses.GetValueOrDefault(1);

            // Usa o CNPJ disponível no ViewModel (CnpjDigits ou CNPJ)
            var cnpj = !string.IsNullOrWhiteSpace(vm.CnpjDigits) ? vm.CnpjDigits : vm.CNPJ;
            var companyName = vm.CompanyName?.Trim() ?? string.Empty;

            var invite = await _invites.CreateAdminInviteAsync(companyName, cnpj, validity, maxUses, ct);

            vm.GeneratedLink = Url.ActionLink("Setup", "Setup", new { token = invite.Token }, Request.Scheme);
            vm.GeneratedExpiresAt = invite.ExpiresAtUtc;
            vm.GeneratedToken = invite.Token;

            // limpa a chave do formulário após uso
            vm.AccessKey = string.Empty;

            ModelState.Clear();
            return View("~/Views/Onboarding/Invite.cshtml", vm);
        }
    }
}
