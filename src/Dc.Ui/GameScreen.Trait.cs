using Dc.Core;
using SkiaSharp;

namespace Dc.Ui;

/// <summary>주인공 만들기 — 성별 다음에 성향을 고른다. 성향이 스탯과 잘하는 과목을 정한다.</summary>
public sealed partial class GameScreen
{
    private const float TraitTop = 230;

    /// <summary>성향을 고를 차례.</summary>
    private bool PickingTrait => clinic.Doctor != null && career.Stage == CareerStage.Student && career.Trait == Trait.None;

    private static Trait[] Traits => [Trait.Quick, Trait.Careful, Trait.Warm, Trait.Tough];

    private void PressTrait(float y)
    {
        int index = (int)((y - TraitTop) / RowH);
        if (y >= TraitTop && index < Traits.Length) career.Trait = Traits[index];
    }

    private void DrawTrait(SKCanvas c)
    {
        var dark = new SKColor(0x13, 0x4E, 0x4A);
        Fill(c, new SKRect(0, 0, W, _height), new SKColor(0xFF, 0xFB, 0xF5));
        Label(c, "어떤 사람인가요?", W / 2, 110, 34, dark, SKTextAlign.Center);
        Label(c, "성향이 스탯과 잘하는 과목을 정합니다", W / 2, 160, 19, dark, SKTextAlign.Center);
        for (int i = 0; i < Traits.Length; i++)
        {
            var trait = Traits[i];
            var stats = Career.StatsOf(trait);
            string good = string.Join(" · ", Career.TraitFavors(trait).Select(id => Catalog.Treatments.First(t => t.Id == id).Name));
            DrawChoice(c, new Choice(Career.TraitName(trait), $"손재주 {stats.Hand} 꼼꼼함 {stats.Focus} 친화력 {stats.Care} 체력 {stats.Body} · {good}",
                "선택", true, "ic_xp", () => { }), TraitTop + i * RowH);
        }
    }
}
