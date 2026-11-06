using Dc.Core;
using SkiaSharp;

namespace Dc.Ui;

/// <summary>개원 전 수련 화면 — 원내생 · 국가고시 · 전공 고르기 · 수련의 · 페이닥터.</summary>
public sealed partial class GameScreen
{
    private sealed record Choice(string Title, string Note, string Price, bool Enabled, string Image, Action Pick);

    private string Suffix => company.Head.Doctor == Gender.Female ? "_f" : "_m";

    private IReadOnlyList<Choice> TrainingChoices() => career.Stage switch
    {
        CareerStage.Student =>
        [
            Item(TrainingItem.Loupe, "루페", "클릭당 XP ×2", "up_loupe", "XP"),
            Item(TrainingItem.ModelTeeth, "모형 치아 세트", "자동 실습 0.5 XP/s", "up_model", "XP"),
            Item(TrainingItem.SeniorNotes, "선배 노트", "자동 실습 ×2", "up_notes", "XP"),
            Item(TrainingItem.AllNighter, "밤샘 실습", "클릭당 XP ×2", "up_allnight", "XP"),
        ],
        CareerStage.Choice =>
        [
            Pick(Major.None, "바로 페이닥터", "전공 없이 빠르게 개원 준비", Hero("paydoctor")),
            Pick(Major.Orthodontics, "교정과 수련", "교정 전문의 없이 교정 해금", "tx_ortho"),
            Pick(Major.Surgery, "구강악안면외과 수련", "임플란트 진료 시간 −20%", "tx_implant"),
            Pick(Major.Prosthodontics, "보철과 수련", "크라운 · 전악 재건을 잘합니다", "tx_crown"),
            Pick(Major.Conservative, "보존과 수련", "레진 · 신경치료를 잘합니다", "tx_root"),
            Pick(Major.Pediatrics, "소아치과 수련", "환자 유입 +30%", "ic_patient"),
        ],
        CareerStage.PayDoctor =>
        [
            new("개원하기", $"자기자본 {KoreanNumber.Format(Career.OwnCapital)}원 + 대출 {KoreanNumber.Format(Career.Loan)}원",
                career.CanOpen ? "개원" : "돈 부족", career.CanOpen, Hero("director"), () => career.Open(clinic)),
            Item(TrainingItem.Incentive, "인센티브 협상", "클릭당 인센티브 ×2", "up_incentive", "원"),
            Item(TrainingItem.NightShift, "야간 진료", "월급 ×1.5", "up_nightshift", "원"),
            Item(TrainingItem.Seminar, "개원 세미나", "개원 시 숙련도 Lv.1", "up_seminar", "원"),
        ],
        _ => [],
    };

    private Choice Item(TrainingItem item, string title, string note, string image, string unit)
    {
        string price = career.Has(item) ? "구매 완료" : KoreanNumber.Format(Career.Cost(item)) + (unit == "XP" ? " XP" : "");
        return new Choice(title, note, price, career.CanBuy(item), image, () => career.Buy(item));
    }

    private Choice Pick(Major major, string title, string note, string image) =>
        new(title, note, "선택", true, image, () => career.Choose(major));

    private void PressTraining(float x, float y)
    {
        if (career.Stage == CareerStage.Choice) { PressCareerChoice(y); return; }
        if (y is >= ClinicTop and < ClinicBottom)
        {
            if (!Hunting) { Tap(); return; }
            for (int i = 0; i < Career.SpotCount; i++)
            {
                var (sx, sy) = SpotAt(i);
                if (MathF.Abs(x - sx) >= SpotHit || MathF.Abs(y - sy) >= SpotHit) continue;
                ShowEarned(career.TapSpot(i));
                return;
            }
            return;
        }
        if (y < ClinicBottom) return;
        var choices = TrainingChoices();
        int index = (int)((y - ClinicBottom) / RowH);
        if (index >= choices.Count) return;
        if (choices[index].Enabled) choices[index].Pick();
        else Toast(career.Stage == CareerStage.Student ? "XP가 모자랍니다" : "돈이 모자랍니다");
    }

    private const float SpotHit = 38, SpotRadius = 15;

    /// <summary>충치 점을 직접 눌러야 하는 때.</summary>
    private bool Hunting => career.Stage == CareerStage.PayDoctor && career.Case == PayCase.Caries && career.CallLeft <= 0;

    private static (float X, float Y) SpotAt(int index) =>
        (W / 2 + MathF.Cos(index * 2.4f) * (40 + index * 11), 310 + MathF.Sin(index * 2.4f) * (28 + index * 11));

    private void ShowEarned(double value)
    {
        _bounce = 1;
        if (value > 0)
            _floaters.Add(new Floater { X = W / 2 + Random.Shared.Next(-70, 70), Y = 410, Text = "+" + KoreanNumber.Format(value) });
    }

    private string CaseImage => career.Case switch
    {
        PayCase.Gum => "tx_scaling",
        PayCase.Root => "tx_root",
        _ => Image("tap_mouth") != null ? "tap_mouth" : "tap_tooth",
    };

    private void DrawCase(SKCanvas c)
    {
        var dark = new SKColor(0x13, 0x4E, 0x4A);
        Label(c, $"{Career.CaseName(career.Case)} {career.CaseLeft}", W / 2, ClinicTop + 30, 22, dark, SKTextAlign.Center);
        if (career.Case == PayCase.Caries)
        {
            int spots = career.Spots == 0 ? (1 << Career.SpotCount) - 1 : career.Spots;
            _paint.Style = SKPaintStyle.Fill;
            _paint.Color = new SKColor(0x3B, 0x24, 0x16);
            for (int i = 0; i < Career.SpotCount; i++)
            {
                if ((spots >> i & 1) == 0) continue;
                var (x, y) = SpotAt(i);
                c.DrawCircle(x, y, SpotRadius, _paint);
            }
        }
        if (career.CallLeft <= 0) return;
        var banner = new SKRect(20, ClinicTop + 44, W - 20, ClinicTop + 104);
        Rounded(c, banner, new SKColor(0xC2, 0x41, 0x0C));
        Label(c, $"옆 체어 마취 호출! 누르면 달려갑니다 {Math.Ceiling(career.CallLeft):0}초", W / 2, banner.MidY + 7, 18, SKColors.White, SKTextAlign.Center);
    }

    private void DrawTraining(SKCanvas c)
    {
        var (title, value, goal) = career.Stage switch
        {
            CareerStage.Student => ("원내생", $"{Math.Floor(career.Xp)} XP", $"누적 XP {Math.Floor(career.TotalXp)} / {Career.ExamGoalXp:0} → 국가고시"),
            CareerStage.Choice => ($"국가고시 {career.Title}", "진로 선택", "수련하면 전공을 얻고, 시간이 더 걸립니다"),
            CareerStage.Resident => ("수련의", "수련 과제", $"{career.TaskNames.First} {career.ScalingDone}/{Career.ScalingGoal} · {career.TaskNames.Second} {career.RootDone}/{Career.RootGoal}"),
            _ => ("페이닥터", KoreanNumber.Format(career.Money) + "원", $"월급 {KoreanNumber.Format(career.Salary)}원/s · 진료 실력 ×{career.SkillRate:0.00} · 목표 {KoreanNumber.Format(Career.OwnCapital)}원"),
        };
        Fill(c, new SKRect(0, 0, W, ClinicTop), new SKColor(0x0F, 0x76, 0x6E));
        Label(c, title, 18, 44, 22, new SKColor(0xCC, 0xFB, 0xF1));
        Label(c, value, 18, 92, 38, SKColors.White);
        Label(c, goal, 18, 130, 19, new SKColor(0xFE, 0xF0, 0x8A));

        var area = new SKRect(0, ClinicTop, W, ClinicBottom);
        Fill(c, area, new SKColor(0xF3, 0xF4, 0xF6));
        string target = career.Stage switch { CareerStage.Student => "tap_model", CareerStage.PayDoctor => CaseImage, _ => "tap_tooth" };
        float size = 200 * (1 + _bounce * 0.08f);
        Icon(c, target, SKRect.Create(W / 2 - size / 2, 310 - size / 2, size, size), "실습");
        string face = career.Stage switch
        {
            CareerStage.Student or CareerStage.Exam => "student",
            CareerStage.Resident => "resident",
            _ => "paydoctor",
        };
        // 루페를 사면 초상도 루페를 쓴다
        string portrait = Hero(face);
        if (career.Has(TrainingItem.Loupe) && Image(portrait + "_loupe") != null) portrait += "_loupe";
        Icon(c, portrait, new SKRect(12, 414, 90, 492), "나");
        if (career.Stage == CareerStage.PayDoctor) DrawCase(c);
        Label(c, Hunting ? "충치를 눌러 잡으세요" : "화면을 눌러 진료", W / 2, 490, 16, new SKColor(0x13, 0x4E, 0x4A), SKTextAlign.Center);

        Fill(c, new SKRect(0, ClinicBottom, W, _height), new SKColor(0xFF, 0xFB, 0xF5));
        var choices = TrainingChoices();
        float listTop = career.Stage == CareerStage.Choice ? ClinicTop : ClinicBottom;
        if (listTop < ClinicBottom) Fill(c, new SKRect(0, ClinicTop, W, _height), new SKColor(0xFF, 0xFB, 0xF5));
        for (int i = 0; i < choices.Count; i++) DrawChoice(c, choices[i], listTop + i * RowH);
    }

    /// <summary>진로 고르기는 줄이 많아 진료 화면까지 덮는다.</summary>
    private void PressCareerChoice(float y)
    {
        var choices = TrainingChoices();
        int index = (int)((y - ClinicTop) / RowH);
        if (y >= ClinicTop && index < choices.Count) choices[index].Pick();
    }

    private void DrawChoice(SKCanvas c, Choice choice, float y)
    {
        var gray = new SKColor(0x6B, 0x72, 0x80);
        Fill(c, new SKRect(0, y + RowH - 1, W, y + RowH), new SKColor(0xE5, 0xE7, 0xEB));
        Icon(c, choice.Image, new SKRect(12, y + 12, 76, y + 76), choice.Title[..1]);
        // 메모가 길면 버튼 왼쪽 폭에 맞춰 두 줄로 나눈다
        var rows = Wrap(choice.Note, 298, 14);
        float titleY = rows.Count > 1 ? y + 28 : y + 38;
        Label(c, choice.Title, 88, titleY, 21, new SKColor(0x13, 0x4E, 0x4A));
        for (int i = 0; i < Math.Min(rows.Count, 2); i++) Label(c, rows[i], 88, titleY + 25 + i * 19, 14, gray);
        var button = new SKRect(392, y + 18, 528, y + 70);
        Rounded(c, button, choice.Enabled ? new SKColor(0x0D, 0x94, 0x88) : new SKColor(0xD1, 0xD5, 0xDB));
        Label(c, choice.Price, button.MidX, button.MidY + 7, 19, choice.Enabled ? SKColors.White : gray, SKTextAlign.Center);
    }
}
