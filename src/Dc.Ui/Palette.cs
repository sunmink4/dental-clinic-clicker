using SkiaSharp;

namespace Dc.Ui;

/// <summary>
/// 플레이어가 고르는 색. 체어 · 배경 그림은 바꿀 부분을 코발트 블루(#0047FF)로 받아 두고,
/// 그 색 범위의 점만 고른 색으로 바꾼다. 명암은 그대로라 색마다 그림을 따로 받지 않아도 된다.
/// </summary>
public static class Palette
{
    private const float KeyHueFrom = 205, KeyHueTo = 255, KeyMinSaturation = 25, KeyBrightness = 88;

    public static readonly (string Name, SKColor Color)[] Colors =
    [
        ("민트", new SKColor(0x4F, 0xB8, 0xA8)),
        ("네이비", new SKColor(0x2B, 0x3F, 0x6B)),
        ("코랄", new SKColor(0xE0, 0x7A, 0x5F)),
        ("라벤더", new SKColor(0x9A, 0x8F, 0xC7)),
        ("버건디", new SKColor(0x7A, 0x2E, 0x3F)),
        ("베이지", new SKColor(0xC9, 0xB4, 0x96)),
        ("그레이", new SKColor(0x8A, 0x90, 0x96)),
    ];

    public static SKColor[] Recolor(SKColor[] pixels, SKColor target)
    {
        target.ToHsv(out float hue, out float saturation, out float value);
        var result = new SKColor[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            p.ToHsv(out float h, out float s, out float v);
            result[i] = h is >= KeyHueFrom and <= KeyHueTo && s >= KeyMinSaturation
                ? SKColor.FromHsv(hue, saturation * s / 100, Math.Min(100, value * v / KeyBrightness), p.Alpha)
                : p;
        }
        return result;
    }
}
