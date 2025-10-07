using Microsoft.AspNetCore.Mvc.Rendering;

namespace PontoApp.Web.ViewModels
{
    public class AdminManualPunchViewModel
    {
        public int? EmployeeId { get; set; }
        public string DataHoraLocal { get; set; } = ""; 
        public PunchType Tipo { get; set; } = PunchType.Entrada;
        public string Justificativa { get; set; } = "";
        public IEnumerable<SelectListItem> Employees { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
