using System.Globalization;

namespace Dc.Core;

public static class KoreanNumber
{
    private static readonly string[] Units = ["", "만", "억", "조", "경", "해", "자", "양", "구", "간"];

    /// <summary>만 · 억 · 조 단위 표기. 1,500 / 2.9만 / 3,000만 / 1.5억. 늘 내림 — 가진 돈보다 많아 보이면 안 된다.</summary>
    public static string Format(double value)
    {
        if (value < 1e4) return Math.Floor(value).ToString("N0", CultureInfo.InvariantCulture);
        int unit = Math.Min((int)(Math.Log10(value) / 4), Units.Length - 1);
        double scaled = value / Math.Pow(10, unit * 4);
        string number = scaled < 100
            ? (Math.Floor(scaled * 10) / 10).ToString("0.#", CultureInfo.InvariantCulture)
            : Math.Floor(scaled).ToString("N0", CultureInfo.InvariantCulture);
        return number + Units[unit];
    }
}
