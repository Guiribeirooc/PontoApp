using Microsoft.AspNetCore.Mvc;

namespace PontoApp.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index(string? msg = null)
    {
        ViewBag.Mensagem = msg;
        return View();
    }
}