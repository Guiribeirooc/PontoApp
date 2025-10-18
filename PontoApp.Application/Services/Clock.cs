using System.Runtime.InteropServices;
using PontoApp.Application.Contracts;

namespace PontoApp.Application.Services;

public class Clock : IClock
{
    private static readonly TimeZoneInfo SpTz =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")
            : TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public DateTime NowSp()
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SpTz);
        return DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
    }
}
