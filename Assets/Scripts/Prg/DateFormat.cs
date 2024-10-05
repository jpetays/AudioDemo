using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>
/// Formats <c>DateTime</c> using <c>CultureInfo.InvariantCulture</c> for consistent results.<br />
/// https://blog.mzikmund.com/2019/11/using-proper-culture-with-c-string-interpolation/
/// </summary>
/// <remarks>Default <c>CultureInfo</c> can be operating system dependent and user configurable.</remarks>
[SuppressMessage("ReSharper", "CheckNamespace")]
public static class DateFormat
{
    public static string WithMinutes(this DateTime dateTime) =>
        ((FormattableString)$"{dateTime:yyyy-MM-dd HH:mm}").ToString(CultureInfo.InvariantCulture);

    public static string WithSeconds(this DateTime dateTime) =>
        ((FormattableString)$"{dateTime:yyyy-MM-dd HH:mm:ss}").ToString(CultureInfo.InvariantCulture);
}
