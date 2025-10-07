using System.Runtime.InteropServices;
using PontoApp.Application.Contracts;

namespace PontoApp.Application.Services;
public class Clock : IClock {
    public DateTimeOffset NowSp() {
        var tzId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "E. South America Standard Time"
            : "America/Sao_Paulo";
        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
    }
}
