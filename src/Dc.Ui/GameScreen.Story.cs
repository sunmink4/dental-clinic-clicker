using Dc.Core;
using SkiaSharp;

namespace Dc.Ui;

/// <summary>대사 장면 — 화면 전체를 덮고, 누를 때마다 다음 대사로 넘어간다. 그동안 게임 시간은 멈춘다.</summary>
public sealed partial class GameScreen
{
    private const float BoxH = 240, SkipW = 110, SkipH = 56, TextSize = 23, LineGap = 34;

    /// <summary>대사 진행. 없으면 대사 없이 돌아간다.</summary>
    public StoryDirector? Story { get; init; }

    private bool Telling => Story?.CurrentId != null;

    private void PressStory(float x, float y)
    {
        if (x > W - SkipW && y < SkipH) Story!.Skip();
        else Story!.Advance();
    }

    private void DrawStory(SKCanvas c)
    {
        if (Story?.Current is not { } scene || Story.Line is not { } line) return;
        var dark = new SKColor(0x10, 0x2A, 0x30);
        Fill(c, new SKRect(0, 0, W, _height), dark);
        var top = new SKRect(0, 0, W, _height - BoxH);
        // 장면 그림이 아직 없으면 지금 치과를 뒤에 깐다
        // 주인공 성별 그림(_m / _f)이 있으면 그것을 먼저 쓴다
        var picture = (Story.Picture is { } name ? Image(name + Suffix) ?? Image(name) : null) ?? Tinted($"bg_space{clinic.SpaceIndex + 1}", clinic.InteriorColor);
        if (picture != null) DrawCover(c, picture, top);

        Rounded(c, new SKRect(W - SkipW + 6, 10, W - 10, SkipH - 10), new SKColor(0, 0, 0, 140));
        Label(c, "건너뛰기", W - SkipW / 2 - 2, 35, 17, SKColors.White, SKTextAlign.Center);

        float boxTop = _height - BoxH;
        Fill(c, new SKRect(0, boxTop, W, _height), new SKColor(0xFF, 0xFB, 0xF5));
        const float size = 170;
        if (line.Speaker == "나")
            Icon(c, Hero(scene.Face), SKRect.Create(W - size - 10, boxTop - size, size, size), "나");
        else if (StoryDirector.Portrait(line.Speaker) is { } portrait)
            Icon(c, portrait, SKRect.Create(10, boxTop - size, size, size), line.Speaker[..1]);
        Label(c, line.Speaker, 24, boxTop + 42, 24, new SKColor(0x0F, 0x76, 0x6E));
        string text = line.Text.Replace("{title}", career.Title);
        float y = boxTop + 84;
        foreach (string row in Wrap(text, W - 48, TextSize))
        {
            Label(c, row, 24, y, TextSize, dark);
            y += LineGap;
        }
        Label(c, "눌러서 계속 ▼", W - 20, _height - 18, 15, new SKColor(0x6B, 0x72, 0x80), SKTextAlign.Right);
    }

    /// <summary>폭에 맞춰 낱말 단위로 줄을 나눈다.</summary>
    private List<string> Wrap(string text, float width, float size)
    {
        _font.Size = size;
        var rows = new List<string>();
        string row = "";
        foreach (string word in text.Split(' '))
        {
            string next = row.Length == 0 ? word : row + " " + word;
            if (_font.MeasureText(next) <= width) { row = next; continue; }
            rows.Add(row);
            row = word;
        }
        rows.Add(row);
        return rows;
    }
}
