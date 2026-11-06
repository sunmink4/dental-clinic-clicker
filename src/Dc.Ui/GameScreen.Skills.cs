using Dc.Core;
using SkiaSharp;

namespace Dc.Ui;

/// <summary>스킬 탭 — 진료 과목 숙련도와 경영 능력, 클레임 직접 응대.</summary>
public sealed partial class GameScreen
{
    /// <summary>불소 · 실란트는 소아치과 특화 지점에서만 보인다.</summary>
    private List<Treatment> SkillTreatments =>
        Catalog.Treatments.Where(t => clinic.Unlocked(t) || t.Id is not ("fluoride" or "sealant" or "pedo_resin" or "pedo_crown")).ToList();

    private IReadOnlyList<Choice> SkillChoices()
    {
        var mastery = clinic.Mastery;
        var list = new List<Choice>();
        if (_skillCategory == 1) AddManagement(list);
        if (_skillCategory == 2) AddConsulting(list);
        if (_skillCategory != 0) return list;
        foreach (var t in SkillTreatments)
        {
            string? blocked = mastery.StudyBlocked(t);
            string indent = new(' ', Mastery.Depth(t.Id) * 2);
            string title = $"{indent}{(Mastery.Depth(t.Id) > 0 ? "└ " : "")}{t.Name} Lv.{mastery.Level(t.Id)}";
            string good = mastery.IsMajor(t.Id) ? $"전공(최대 Lv.{mastery.LevelCap(t.Id)}) · " : mastery.IsFavored(t.Id) ? "잘하는 과목 · " : "";
            string note = blocked ?? $"{good}난이도 {t.Difficulty} · 단가 +{(mastery.PriceRate(t.Id) - 1) * 100:0}% · 환자 {mastery.PatientsToNext(t.Id)}명 더";
            string price = mastery.SeminarsOf(t.Id) >= mastery.SeminarCap(t.Id) ? "완료" : KoreanNumber.Format(mastery.SeminarCost(t));
            list.Add(new(title, note, price, blocked == null && clinic.Money >= mastery.SeminarCost(t), "tx_" + t.Id, () =>
            {
                if (mastery.StudyBlocked(t) is { } reason) Toast(reason);
                else clinic.Study(t);
            }));
        }
        list.Add(new($"손 빠르기 Lv.{clinic.Skill}", "진료 시간 -12%, 대신 클레임이 더 쌓입니다", KoreanNumber.Format(clinic.Cost(Purchase.Skill)),
            clinic.Money >= clinic.Cost(Purchase.Skill), "up_skill", () => clinic.Buy(Purchase.Skill)));
        list.Add(AbilityChoice(Ability.Technique));
        return list;
    }

    private Choice AbilityChoice(Ability ability)
    {
        var mastery = clinic.Mastery;
        return new($"{Mastery.AbilityName(ability)} Lv.{mastery.Level(ability)}", Mastery.AbilityNote(ability), KoreanNumber.Format(mastery.AbilityCost(ability)),
            clinic.Money >= mastery.AbilityCost(ability), "sk_" + ability.ToString().ToLowerInvariant(), () => clinic.BuyAbility(ability));
    }

    private void AddManagement(List<Choice> list)
    {
        list.Add(new($"원장 피로 {clinic.Fatigue:0}%", FatigueNote, clinic.Resting ? $"{Math.Ceiling(clinic.RestLeft):0}초" : "휴식",
            !clinic.Resting && clinic.Fatigue > 0, Hero("director"), RestNow));
        list.Add(AbilityChoice(Ability.Leadership));
        list.Add(AbilityChoice(Ability.Stamina));
    }

    private void AddConsulting(List<Choice> list)
    {
        list.Add(new($"클레임 직접 응대 · 현재 {clinic.Claims:0}%", "원장이 나서서 클레임 20을 줄입니다", KoreanNumber.Format(clinic.ClaimHandleCost),
            clinic.Claims > 0 && clinic.Money >= clinic.ClaimHandleCost, "ev_complaint", () => clinic.HandleClaims()));
        list.Add(AbilityChoice(Ability.Diagnosis));
        list.Add(AbilityChoice(Ability.Consult));
        list.Add(AbilityChoice(Ability.Care));
    }

    /// <summary>피로의 효과와 푸는 법.</summary>
    private const string FatigueNote = "높을수록 직접 진료 수입 감소(최대 −70%), 클레임 증가(최대 2배). 쉬면 30초간 5배 회복, 그동안 직접 진료 불가";

    private void RestNow()
    {
        if (clinic.Rest()) Toast($"{Clinic.RestSeconds:0}초 휴식 — 피로가 5배 빨리 풀립니다");
    }

    private const float CategoryH = 44;
    private static readonly string[] SkillCategories = ["진료 스킬", "경영 스킬", "상담 스킬"];
    private int _skillCategory;

    private void PressSkills(float x, float y)
    {
        if (y < ListTop + CategoryH)
        {
            _skillCategory = Math.Clamp((int)(x / (W / SkillCategories.Length)), 0, SkillCategories.Length - 1);
            _scroll = 0;
            return;
        }
        var choices = SkillChoices();
        int index = (int)((y - ListTop - CategoryH + _scroll) / RowH);
        if (index >= choices.Count) return;
        if (choices[index].Enabled) { choices[index].Pick(); return; }
        if (choices[index].Price is "휴식" or "완료" || choices[index].Price.EndsWith('초')) return;
        var treatments = SkillTreatments;
        Toast(_skillCategory == 0 && index < treatments.Count && clinic.Mastery.StudyBlocked(treatments[index]) is { } reason ? reason : "돈이 모자랍니다");
    }

    private void DrawSkills(SKCanvas c)
    {
        var choices = SkillChoices();
        for (int i = 0; i < choices.Count; i++) DrawChoice(c, choices[i], ListTop + CategoryH + i * RowH - _scroll);
        float width = W / SkillCategories.Length;
        for (int i = 0; i < SkillCategories.Length; i++)
        {
            var box = new SKRect(i * width, ListTop, (i + 1) * width, ListTop + CategoryH);
            Fill(c, box, i == _skillCategory ? new SKColor(0xCC, 0xFB, 0xF1) : new SKColor(0xE5, 0xE7, 0xEB));
            Label(c, SkillCategories[i], box.MidX, box.MidY + 7, 18, new SKColor(0x13, 0x4E, 0x4A), SKTextAlign.Center);
        }
    }

    private void DrawClaims(SKCanvas c)
    {
        var calm = new SKColor(0xCC, 0xFB, 0xF1);
        var hot = new SKColor(0xFC, 0xA5, 0xA5);
        string fatigue = clinic.Resting ? $"휴식 {Math.Ceiling(clinic.RestLeft):0}초" : $"피로 {clinic.Fatigue:0}%";
        Label(c, fatigue, W - 16, 106, 16, clinic.Fatigue >= Clinic.ClaimWarning ? hot : calm, SKTextAlign.Right);
        Label(c, $"클레임 {clinic.Claims:0}%", W - 104, 126, 16, clinic.Claims >= Clinic.ClaimWarning ? hot : calm, SKTextAlign.Right);
        var rest = new SKRect(W - 86, 114, W - 16, 142);
        Rounded(c, rest, clinic.Resting ? new SKColor(0x6B, 0x72, 0x80) : calm);
        Label(c, clinic.Resting ? "쉬는 중" : "휴식", rest.MidX, rest.MidY + 6, 15, new SKColor(0x13, 0x4E, 0x4A), SKTextAlign.Center);
    }
}
