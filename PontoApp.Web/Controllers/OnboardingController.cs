using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Application.DTOs;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Controllers
{
    [AllowAnonymous]
    [Route("onboarding")]
    public class OnboardingController : Controller
    {
        private readonly IOnboardingService _onboarding;

        public OnboardingController(IOnboardingService onboarding)
            => _onboarding = onboarding;

        // GET /onboarding  e também /onboarding/create
        [HttpGet("")]
        [HttpGet("create")]
        public IActionResult Create()
            => View("~/Views/Onboarding/Create.cshtml", new CompanyCreateViewModel());

        // POST /onboarding  e também /onboarding/create
        [HttpPost("")]
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CompanyCreateViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Onboarding/Create.cshtml", vm);

            var dto = new OnboardingCreateDto
            {
                CompanyName = vm.Name,
                CompanyDocument = vm.CNPJ,
                AdminName = vm.AdminName,
                AdminEmail = vm.AdminEmail,
                AdminPassword = vm.AdminPassword
            };

            try
            {
                var (company, admin) = await _onboarding.CreateCompanyWithAdminAsync(dto);
                TempData["Success"] = $"Empresa '{company.Name}' criada. Admin: {admin.Email}";
                return RedirectToAction("Index", "Home");
            }
            catch (DbUpdateException dbex)
            {
                // Dica: tratar violação de índice único (nome da empresa/email já existente)
                ModelState.AddModelError(string.Empty, "Não foi possível salvar. Verifique se já não existe uma empresa ou e-mail iguais.");
                ModelState.AddModelError(string.Empty, dbex.GetBaseException().Message);
                return View("~/Views/Onboarding/Create.cshtml", vm);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("~/Views/Onboarding/Create.cshtml", vm);
            }
        }
    }
}
