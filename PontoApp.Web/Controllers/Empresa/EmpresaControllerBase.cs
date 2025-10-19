using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PontoApp.Web.Controllers.Empresa
{
    [Authorize(Roles = "Admin")]
    [Route("empresa/[controller]/[action]")]
    public abstract class EmpresaControllerBase : Controller
    {
        protected int CompanyId =>
            int.TryParse(User.FindFirstValue("CompanyId"), out var id) ? id : 0;
    }
}
