using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoApp.Application.Contracts;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Controllers.Empresa
{
    [Authorize(Roles = "Admin")]
    [Route("empresa")]
    public class HomeController : EmpresaControllerBase
    {
        private readonly ICompanyService _companyService;

        public HomeController(ICompanyService companyService)
            => _companyService = companyService;

        [HttpGet("home")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            if (CompanyId <= 0) return Forbid();

            var company = await _companyService.GetByIdAsync(CompanyId, ct);
            var vm = new EmpresaHomeViewModel
            {
                CompanyId = CompanyId,
                CompanyName = company?.Name ?? "(empresa não encontrada)"
            };

            return View("~/Views/Empresa/Home/Index.cshtml", vm);
        }
    }
}
