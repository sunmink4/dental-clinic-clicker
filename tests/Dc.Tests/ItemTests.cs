using Dc.Core;
using Dc.Ui;
using SkiaSharp;
using Xunit;

namespace Dc.Tests;

public class ItemTests
{
    private static Item Of(string id) => Items.All.Single(i => i.Id == id);

    [Fact]
    public void Every_slot_has_items()
    {
        foreach (var slot in Enum.GetValues<ItemSlot>()) Assert.Contains(Items.All, i => i.Slot == slot);
    }

    [Fact]
    public void Buying_equips_and_effects_apply_only_while_worn()
    {
        var c = new Clinic { Money = 1e9 };
        double seconds = c.AverageSeconds;
        Assert.True(c.BuyItem(Of("lp1")));
        Assert.Equal(seconds * 0.95, c.AverageSeconds, 6);
        Assert.False(c.BuyItem(Of("lp1")));
        c.Mastery.TakeOff(ItemSlot.Loupe);
        Assert.Equal(seconds, c.AverageSeconds, 6);
    }

    [Fact]
    public void One_item_per_slot_and_effects_stack_across_slots()
    {
        var c = new Clinic { Money = 1e10 };
        c.BuyItem(Of("lp1"));
        c.BuyItem(Of("lp2"));
        Assert.Equal("lp2", c.Mastery.Worn(ItemSlot.Loupe)!.Id);
        double one = c.AverageSeconds;
        c.BuyItem(Of("hp1"));
        Assert.Equal(one * 0.94, c.AverageSeconds, 6);
        double inflow = c.Inflow;
        c.BuyItem(Of("gw1"));
        Assert.Equal(inflow * 1.03, c.Inflow, 6);
    }

    [Fact]
    public void Items_are_saved_and_cannot_be_bought_without_money()
    {
        Assert.False(new Clinic { Money = 0 }.BuyItem(Of("fs1")));
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "save.json");
        var c = new Clinic { Money = 1e9 };
        c.BuyItem(Of("ms1"));
        new SaveStore(path).Save(c, new Career(), new EventDirector(), DateTime.UtcNow);
        var mastery = new SaveStore(path).Load()!.Company!.Head.Mastery;
        Assert.Equal("ms1", mastery.Worn(ItemSlot.Mask)!.Id);
        Directory.Delete(Path.GetDirectoryName(path)!, true);
    }

    [Fact]
    public void Item_tab_buys_through_slots()
    {
        var c = new Clinic { Doctor = Gender.Male, Money = 1e9 };
        using var screen = new GameScreen(c, new Career { Stage = CareerStage.Done }, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        void Press(float x, float y) { screen.PointerDown(x, y); screen.PointerUp(x, y); screen.Render(surface.Canvas, 540, 960); }
        screen.Render(surface.Canvas, 540, 960);
        Press(270, 520);                       // 아이템 탭
        Press(460, 554 + 40);                  // 페이스쉴드 칸
        Press(460, 554 + 88 + 40);             // 일회용 페이스쉴드 구매
        Assert.Equal("fs1", c.Mastery.Worn(ItemSlot.FaceShield)!.Id);
        Press(460, 554 + 88 + 40);             // 해제
        Assert.Null(c.Mastery.Worn(ItemSlot.FaceShield));
    }
}
