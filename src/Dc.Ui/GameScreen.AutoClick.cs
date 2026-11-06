using Dc.Core;
using SkiaSharp;

namespace Dc.Ui;

/// <summary>자동 클릭 — 머리글 오른쪽, 초당 매출 아래. 간격(ms)은 − + 로 조정한다.</summary>
public sealed partial class GameScreen
{
    public const int AutoStep = 50, AutoMin = 50, AutoMax = 2000;
    private const float AutoTop = 50, AutoH = 22, AutoHit = 8;
    private const int MaxAutoPerFrame = 10;
    private static readonly SKRect MinusBox = new(322, AutoTop, 350, AutoTop + AutoH);
    private static readonly SKRect PlusBox = new(424, AutoTop, 452, AutoTop + AutoH);
    private static readonly SKRect ToggleBox = new(458, AutoTop, 526, AutoTop + AutoH);

    private double _autoWaited;

    private void TickAutoClick(double seconds)
    {
        var head = company.Head;
        if (!head.AutoClick) { _autoWaited = 0; return; }
        _autoWaited += seconds * 1000;
        for (int i = 0; _autoWaited >= head.AutoClickMs && i < MaxAutoPerFrame; i++)
        {
            _autoWaited -= head.AutoClickMs;
            Tap();
        }
        _autoWaited = Math.Min(_autoWaited, head.AutoClickMs);
    }

    private static bool Inside(SKRect box, float x, float y) =>
        x >= box.Left && x <= box.Right && y >= box.Top - AutoHit && y <= box.Bottom + AutoHit;

    /// <summary>자동 클릭 조작을 눌렀으면 참.</summary>
    private bool PressAutoClick(float x, float y)
    {
        var head = company.Head;
        if (Inside(ToggleBox, x, y)) head.AutoClick = !head.AutoClick;
        else if (Inside(MinusBox, x, y)) head.AutoClickMs = Math.Max(AutoMin, head.AutoClickMs - AutoStep);
        else if (Inside(PlusBox, x, y)) head.AutoClickMs = Math.Min(AutoMax, head.AutoClickMs + AutoStep);
        else return false;
        return true;
    }

    private void DrawAutoClick(SKCanvas c)
    {
        var head = company.Head;
        var dark = new SKColor(0x13, 0x4E, 0x4A);
        var field = new SKColor(0xCC, 0xFB, 0xF1);
        Rounded(c, MinusBox, field);
        Label(c, "-", MinusBox.MidX, MinusBox.MidY + 7, 20, dark, SKTextAlign.Center);
        Label(c, $"{head.AutoClickMs}ms", (MinusBox.Right + PlusBox.Left) / 2, AutoTop + 17, 16, SKColors.White, SKTextAlign.Center);
        Rounded(c, PlusBox, field);
        Label(c, "+", PlusBox.MidX, PlusBox.MidY + 7, 20, dark, SKTextAlign.Center);
        Rounded(c, ToggleBox, head.AutoClick ? new SKColor(0xF5, 0x9E, 0x0B) : field);
        Label(c, head.AutoClick ? "자동 켬" : "자동 끔", ToggleBox.MidX, ToggleBox.MidY + 6, 15, dark, SKTextAlign.Center);
    }
}
