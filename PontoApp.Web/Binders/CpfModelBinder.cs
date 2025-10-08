using Microsoft.AspNetCore.Mvc.ModelBinding;
using PontoApp.Web.Utils;

namespace PontoApp.Web.Binders
{
    public class CpfModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext ctx)
        {
            var raw = ctx.ValueProvider.GetValue(ctx.ModelName).FirstValue;
            var digits = CpfUtils.OnlyDigits(raw);
            ctx.Result = ModelBindingResult.Success(digits);
            return Task.CompletedTask;
        }
    }
}
