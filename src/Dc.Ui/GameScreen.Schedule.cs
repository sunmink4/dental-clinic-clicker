using Dc.Core;
using SkiaSharp;

namespace Dc.Ui;

/// <summary>일정 탭 — 하루 10칸을 진료별로 배정한다.</summary>
public sealed partial class GameScreen
{
    private const int ScheduleTab = 2, ScheduleButtons = 2;
    private const float SlotMinusLeft = 400, SlotPlusLeft = 466, SlotButton = 62;

    private List<Treatment> ScheduleTreatments => Catalog.Treatments.Where(clinic.Unlocked).ToList();

    private int ScheduleCount => ScheduleButtons + ScheduleTreatments.Count;

    private void PressSchedule(float x, float y)
    {
        int index = (int)((y - ListTop + _scroll) / RowH);
        if (index == 0) { clinic.AutoSchedule(); Toast("지금 스탯에 맞게 배정했습니다"); return; }
        if (index == 1) { clinic.Schedule.Clear(); Toast("환자 수요대로 돌아갑니다"); return; }
        var treatments = ScheduleTreatments;
        if (index - ScheduleButtons >= treatments.Count) return;
        var t = treatments[index - ScheduleButtons];
        if (x >= SlotPlusLeft)
        {
            if (!clinic.AddSlot(t)) Toast($"하루 {Clinic.DaySlots}칸을 다 썼습니다");
        }
        else if (x >= SlotMinusLeft) clinic.RemoveSlot(t);
    }

    private void DrawSchedule(SKCanvas c)
    {
        var dark = new SKColor(0x13, 0x4E, 0x4A);
        var gray = new SKColor(0x6B, 0x72, 0x80);
        string state = clinic.ScheduledSlots > 0 ? $"배정 {clinic.ScheduledSlots}/{Clinic.DaySlots}칸" : "환자 수요대로 진료 중";
        DrawChoice(c, new Choice("자동 배정", $"{state} · 현재 스탯에서 효율 좋은 진료 위주로", "배정", true, "up_notes", () => { }), ListTop - _scroll);
        DrawChoice(c, new Choice("배정 지우기", "일정 없이 환자 수요대로 진료합니다", "지우기", true, "ic_patient", () => { }), ListTop + RowH - _scroll);

        var treatments = ScheduleTreatments;
        for (int i = 0; i < treatments.Count; i++)
        {
            var t = treatments[i];
            float y = ListTop + (i + ScheduleButtons) * RowH - _scroll;
            Fill(c, new SKRect(0, y + RowH - 1, W, y + RowH), new SKColor(0xE5, 0xE7, 0xEB));
            Icon(c, "tx_" + t.Id, new SKRect(12, y + 12, 76, y + 76), t.Name[..1]);
            Label(c, $"{t.Name} {clinic.Slots(t)}칸", 88, y + 30, 21, dark);
            string note = $"수가 {KoreanNumber.Format(clinic.PriceNow(t))} · {clinic.SecondsNow(t):0.0}초 · 피로 {t.Difficulty} · 초당 {KoreanNumber.Format(clinic.PriceNow(t) / clinic.SecondsNow(t))} · 숙련 Lv.{clinic.Mastery.Level(t.Id)}";
            var rows = Wrap(note, 300, 14);
            for (int r = 0; r < Math.Min(rows.Count, 2); r++) Label(c, rows[r], 88, y + 54 + r * 19, 14, gray);

            var minus = SKRect.Create(SlotMinusLeft, y + 20, SlotButton - 6, 48);
            var plus = SKRect.Create(SlotPlusLeft, y + 20, SlotButton - 6, 48);
            Rounded(c, minus, new SKColor(0xD1, 0xD5, 0xDB));
            Label(c, "-", minus.MidX, minus.MidY + 9, 26, dark, SKTextAlign.Center);
            Rounded(c, plus, new SKColor(0x0D, 0x94, 0x88));
            Label(c, "+", plus.MidX, plus.MidY + 9, 26, SKColors.White, SKTextAlign.Center);
        }
    }

    private int ChairCycle(int chair) => chair * 97 + (int)((_time + chair * 1.3f) / Math.Max(3, clinic.AverageSeconds));

    /// <summary>일하는 체어 위에 지금 하는 진료 이름을 띄운다.</summary>
    private void DrawChairLabel(SKCanvas c, SKRect box, string text)
    {
        float size = Math.Clamp(box.Width * 0.1f, 9, 14);
        var pill = new SKRect(box.Left + 2, box.Top, box.Right - 2, box.Top + size * 1.5f);
        Rounded(c, pill, new SKColor(0x13, 0x4E, 0x4A, 200));
        Label(c, text, pill.MidX, pill.Bottom - size * 0.35f, size, SKColors.White, SKTextAlign.Center);
    }
}
