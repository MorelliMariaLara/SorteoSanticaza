using System.Globalization;

namespace SorteoSanticaza.Services;

public static class FormatHelper
{
    private static readonly CultureInfo Ar = CultureInfo.GetCultureInfo("es-AR");

    public static string FormatArs(long cents)
    {
        var value = cents / 100m;
        return value.ToString("C0", Ar);
    }

    public static string FormatDateTimeAr(string iso)
    {
        if (!DateTimeOffset.TryParse(iso, out var dto))
        {
            return iso;
        }

        var ar = TimeZoneInfo.ConvertTime(dto, GetArgentinaTimeZone());
        return ar.ToString("dd/MM/yyyy 'a las' HH:mm", Ar) + " hs";
    }

    public static string PadTicket(int number, int width = 5) =>
        number.ToString().PadLeft(width, '0');

    public static TimeZoneInfo GetArgentinaTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires"); }
        catch { /* ignore */ }
        try { return TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time"); }
        catch { /* ignore */ }
        return TimeZoneInfo.CreateCustomTimeZone("ART", TimeSpan.FromHours(-3), "ART", "ART");
    }
}
