using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PontoApp.Application.Contracts;
using PontoApp.Web.ViewModels;
using System;
using System.Threading;

namespace PontoApp.Web.Controllers
{
    [AllowAnonymous]
    [Route("onboarding")]
    public class OnboardingController : Controller
    {
        private readonly IInviteService _invites;
        private readonly IOptions<SetupOptions> _options;

        public OnboardingController(IInviteService invites, IOptions<SetupOptions> options)
        {
            _invites = invites;
            _options = options;
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

            if (string.IsNullOrWhiteSpace(_options.Value.MasterKey) ||
                !string.Equals(vm.AccessKey, _options.Value.MasterKey, StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(vm.AccessKey), "Chave de acesso inválida.");
                return View("~/Views/Onboarding/Invite.cshtml", vm);
            }

            var validity = TimeSpan.FromHours(vm.ValidityHours ?? 48);
            var maxUses = vm.MaxUses.GetValueOrDefault(1);

            var invite = await _invites.CreateAdminInviteAsync(vm.CompanyName.Trim(), vm.CNPJ, validity, maxUses, ct);

            vm.GeneratedLink = Url.ActionLink("Setup", "Setup", new { token = invite.Token }, Request.Scheme);
            vm.GeneratedExpiresAt = invite.ExpiresAtUtc;
            vm.AccessKey = string.Empty;

            ModelState.Clear();
            return View("~/Views/Onboarding/Invite.cshtml", vm);
        }
    }
}
