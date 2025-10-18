using Microsoft.AspNetCore.Mvc.Rendering;
using PontoApp.Domain.Entities;

namespace PontoApp.Web.ViewModels
{
    public class PunchMarkViewModel
    {
        public int? EmployeeId { get; set; }
        public PunchType Tipo { get; set; } = PunchType.Entrada;
        public IEnumerable<SelectListItem> Employees { get; set; } = new List<SelectListItem>();
        public string? Mensagem { get; set; }
    }
}