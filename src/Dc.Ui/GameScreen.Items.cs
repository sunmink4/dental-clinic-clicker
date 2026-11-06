using Dc.Core;
using SkiaSharp;

namespace Dc.Ui;

/// <summary>아이템 탭 — 부위별 장착 칸과, 칸을 누르면 나오는 구매 · 장착 목록.</summary>
public sealed partial class GameScreen
{
    private const int ItemTab = 3;
    private ItemSlot? _itemSlot;

    private static string SlotIcon(ItemSlot slot) => slot == ItemSlot.Loupe ? "up_loupe" : "it_" + slot.ToString().ToLowerInvariant();

    private IReadOnlyList<Choice> ItemChoices()
    {
        var mastery = clinic.Mastery;
        if (_itemSlot is not { } slot)
            return Enum.GetValues<ItemSlot>().Select(s =>
            {
                var worn = mastery.Worn(s);
                return new Choice($"{Items.SlotName(s)} · {worn?.Name ?? "없음"}", worn?.Note ?? "눌러서 고릅니다", "고르기", true, SlotIcon(s), () => { _itemSlot = s; _scroll = 0; });
            }).ToList();

        var list = new List<Choice> { new("뒤로", "아이템 칸으로 돌아갑니다", "←", true, "mn_close", () => { _itemSlot = null; _scroll = 0; }) };
        foreach (var item in Items.All.Where(i => i.Slot == slot))
        {
            bool owned = mastery.Owns(item), worn = mastery.Worn(slot) == item;
            string price = worn ? "해제" : owned ? "장착" : KoreanNumber.Format(item.Cost);
            list.Add(new(item.Name, item.Note, price, owned || clinic.Money >= item.Cost, SlotIcon(slot), () =>
            {
                if (worn) mastery.TakeOff(slot);
                else if (owned) mastery.PutOn(item);
                else clinic.BuyItem(item);
            }));
        }
        return list;
    }

    private void PressItems(float y)
    {
        var choices = ItemChoices();
        int index = (int)((y - ListTop + _scroll) / RowH);
        if (index >= choices.Count) return;
        if (choices[index].Enabled) choices[index].Pick();
        else Toast("돈이 모자랍니다");
    }

    private void DrawItems(SKCanvas c)
    {
        var choices = ItemChoices();
        for (int i = 0; i < choices.Count; i++) DrawChoice(c, choices[i], ListTop + i * RowH - _scroll);
    }

    private const float PillTop = 110, PillBottom = 138;

    /// <summary>유입 · 처리는 누르면 요소 목록이 열리는 버튼이다.</summary>
    private void DrawFactorButtons(SKCanvas c)
    {
        var dark = new SKColor(0x13, 0x4E, 0x4A);
        var inflow = new SKRect(14, PillTop, InflowRight - 8, PillBottom);
        var capacity = new SKRect(InflowRight - 4, PillTop, FactorRight - 8, PillBottom);
        Rounded(c, inflow, new SKColor(0xFE, 0xF0, 0x8A));
        Label(c, $"유입 {clinic.Inflow:0.00} ›", inflow.MidX, inflow.MidY + 6, 15, dark, SKTextAlign.Center);
        Rounded(c, capacity, new SKColor(0xFE, 0xF0, 0x8A));
        Label(c, $"처리 {clinic.Capacity:0.00} ›", capacity.MidX, capacity.MidY + 6, 15, dark, SKTextAlign.Center);
        string verdict = clinic.Capacity < clinic.Inflow ? "처리 부족" : "유입 부족";
        Label(c, verdict, FactorRight - 2, PillBottom - 8, 14, new SKColor(0xFE, 0xF0, 0x8A));
    }
}
