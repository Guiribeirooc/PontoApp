using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace PontoApp.Web.Authorization
{
    public class OwnEmployeeHandler : AuthorizationHandler<OwnEmployeeRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OwnEmployeeRequirement requirement)
        {
            var empIdClaim = context.User.FindFirstValue("EmployeeId");
            if (string.IsNullOrEmpty(empIdClaim))
                return Task.CompletedTask;

            if (context.Resource is Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext mvc &&
                mvc.RouteData.Values.TryGetValue("employeeId", out var routeVal) &&
                routeVal?.ToString() == empIdClaim)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}