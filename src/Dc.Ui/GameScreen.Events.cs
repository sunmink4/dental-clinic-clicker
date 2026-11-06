using Dc.Core;
using SkiaSharp;

namespace Dc.Ui;

/// <summary>이벤트 카드 — 진료실 화면 위에 뜬다. 아래 구매 목록은 그대로 쓸 수 있다.</summary>
public sealed partial class GameScreen
{
    private const float CardLeft = 20, CardRight = W - 20, CardTop = ClinicTop + 10, CardBottom = ClinicBottom - 10;
    private const float ChoiceTop = CardTop + 150, ChoiceH = 54;

    private void TickEvents(double seconds)
    {
        if (events.Tick(seconds, clinic, Random.Shared) is { } message) Toast(message);
    }

    /// <summary>진료실 화면 누르기를 카드가 받았으면 참.</summary>
    private bool PressEvent(float y)
    {
        if (events.Active == EventKind.None) return false;
        if (events.Active == EventKind.Emergency)
        {
            _bounce = 1;
            if (events.Tap(clinic) is { } message) Toast(message);
            return true;
        }
        int index = (int)((y - ChoiceTop) / ChoiceH);
        if (y >= ChoiceTop && events.Card(clinic) is { } card && index < card.Choices.Length && events.Choose(index, clinic) is { } result)
            Toast(result);
        return true;
    }

    private void DrawEvent(SKCanvas c)
    {
        if (events.Card(clinic) is not { } card) return;
        var box = new SKRect(CardLeft, CardTop, CardRight, CardBottom);
        var dark = new SKColor(0x13, 0x4E, 0x4A);
        Rounded(c, box, new SKColor(0xFF, 0xFB, 0xF5, 245));
        Icon(c, card.Image, new SKRect(CardLeft + 14, CardTop + 14, CardLeft + 134, CardTop + 134), "!");
        Label(c, card.Title, CardLeft + 148, CardTop + 50, 24, dark);
        var body = Wrap(card.Body, CardRight - CardLeft - 164, 17);
        for (int i = 0; i < Math.Min(body.Count, 3); i++) Label(c, body[i], CardLeft + 148, CardTop + 78 + i * 22, 17, dark);
        Label(c, $"{Math.Ceiling(events.ActiveLeft):0}초", CardRight - 16, CardTop + 30, 16, new SKColor(0xC2, 0x41, 0x0C), SKTextAlign.Right);

        for (int i = 0; i < card.Choices.Length; i++)
        {
            var button = new SKRect(CardLeft + 14, ChoiceTop + i * ChoiceH + 3, CardRight - 14, ChoiceTop + (i + 1) * ChoiceH - 3);
            Rounded(c, button, new SKColor(0x0D, 0x94, 0x88));
            Label(c, card.Choices[i], button.MidX, button.MidY + 7, 19, SKColors.White, SKTextAlign.Center);
        }
    }

    /// <summary>지금 걸려 있는 효과를 진료실 오른쪽 아래에 적는다.</summary>
    private void DrawEffects(SKCanvas c)
    {
        var lines = new List<string>();
        if (clinic.WreathLeft > 0) lines.Add($"개원 축하 화환 유입 ×2 {Math.Ceiling(clinic.WreathLeft):0}초");
        if (clinic.ReviewLeft > 0) lines.Add($"좋은 리뷰 유입 ×1.5 {Math.Ceiling(clinic.ReviewLeft):0}초");
        if (clinic.BrokenLeft > 0) lines.Add($"체어 고장 {Math.Ceiling(clinic.BrokenLeft):0}초");
        if (clinic.ComplaintLeft > 0) lines.Add($"평판 −5% {Math.Ceiling(clinic.ComplaintLeft / 60):0}분");
        if (clinic.ModelClinic) lines.Add("모범 치과");
        if (clinic.StaffFatigue >= 1) lines.Add($"위생사 피로 {clinic.StaffFatigue:0}%");
        for (int i = 0; i < lines.Count; i++)
            Label(c, lines[i], W - 12, 484 - i * 18, 14, new SKColor(0x13, 0x4E, 0x4A), SKTextAlign.Right);
    }

    private const float InsuranceY = 286, InsuranceH = 60;

    private void DrawInsurance(SKCanvas c)
    {
        if (clinic.SpaceIndex < 1) return;
        var dark = new SKColor(0x13, 0x4E, 0x4A);
        float y = ListTop + InsuranceY;
        Label(c, "보험 청구 모드", 20, y + 26, 21, dark);
        Label(c, "보험 진료 매출 +30%, 실사에서 적발 위험", 20, y + 50, 14, new SKColor(0x6B, 0x72, 0x80));
        var button = new SKRect(392, y + 4, 528, y + InsuranceH - 4);
        Rounded(c, button, clinic.Overclaim ? new SKColor(0xC2, 0x41, 0x0C) : new SKColor(0xD1, 0xD5, 0xDB));
        Label(c, clinic.Overclaim ? "켜짐" : "꺼짐", button.MidX, button.MidY + 8, 22, clinic.Overclaim ? SKColors.White : dark, SKTextAlign.Center);
    }
}
