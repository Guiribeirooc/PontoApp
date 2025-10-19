using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PontoApp.Web.Controllers.Empresa
{
    [Authorize(Roles = "Admin")]
    [Route("empresa")]
    public class HomeController : EmpresaControllerBase
    {
        [HttpGet("home")]
        public IActionResult Index()
        {
            // aqui você pode carregar dados da empresa logada:
            // var company = _companyService.GetById(CompanyId);
            return View("~/Views/Empresa/Home/Index.cshtml");
        }
    }
}
