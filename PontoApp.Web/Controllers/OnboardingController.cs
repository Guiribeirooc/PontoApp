using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public OnboardingController(IInviteService invites)
        {
            _invites = invites;
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

            var validity = TimeSpan.FromHours(vm.ValidityHours ?? 48);
            var maxUses = vm.MaxUses.GetValueOrDefault(1);

            var invite = await _invites.CreateAdminInviteAsync(vm.CompanyName.Trim(), vm.CnpjDigits, validity, maxUses, ct);

            vm.GeneratedLink = Url.ActionLink("Setup", "Setup", new { token = invite.Token }, Request.Scheme);
            vm.GeneratedExpiresAt = invite.ExpiresAtUtc;
            vm.GeneratedToken = invite.Token;

            ModelState.Clear();
            return View("~/Views/Onboarding/Invite.cshtml", vm);
        }
    }
}
