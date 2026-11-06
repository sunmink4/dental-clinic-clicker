using Dc.Core;
using Dc.Ui;
using SkiaSharp;
using Xunit;

namespace Dc.Tests;

public class CompanyTests
{
    private static Company Ready(double money = 1e12, double revenue = 1.78e9)
    {
        var company = new Company(new Clinic { Doctor = Gender.Female, ChairColor = 3, SpaceIndex = 2, Chairs = 8, Dentists = 3, Hygienists = 8, Money = money, Revenue = revenue });
        company.Link(0);
        return company;
    }

    [Fact]
    public void Branches_unlock_at_fifty_pyeong_and_cost_grows_fourfold()
    {
        var early = new Company(new Clinic { SpaceIndex = 1, Money = 1e12 });
        Assert.False(early.Open(Site.NewTown, Specialty.None));

        var c = Ready();
        Assert.Equal(1e9, c.OpenCost(Site.NewTown));
        Assert.Equal(5e8, c.OpenCost(Site.Province));
        Assert.Equal(4e9, c.OpenCost(Site.Gangnam));
        Assert.True(c.Open(Site.NewTown, Specialty.None));
        Assert.Equal(4e9, c.OpenCost(Site.NewTown));
        Assert.Equal(1e12 - 1e9, c.Head.Money);
    }

    [Fact]
    public void Fame_is_confirmed_only_when_a_branch_opens_and_multiplies_every_branch()
    {
        var c = Ready();
        double head = c.Head.RevenuePerSecond;
        Assert.Equal(13, c.PendingFame);
        Assert.Equal(0, c.Fame);
        Assert.True(c.Open(Site.NewTown, Specialty.None));
        Assert.Equal(13, c.Fame);
        Assert.Equal(head * 1.26, c.Head.RevenuePerSecond, 3);
        Assert.Equal(1.26, c.Branches[1].FameRate, 6);
    }

    [Fact]
    public void New_branch_starts_small_shares_money_and_keeps_the_players_choices()
    {
        var c = Ready();
        c.Open(Site.Seoul, Specialty.Local);
        var branch = c.Current;
        Assert.Equal(1, c.Selected);
        Assert.Equal(1, branch.Chairs);
        Assert.Equal(1, branch.Dentists);
        Assert.Equal(0, branch.SpaceIndex);
        Assert.Equal(Gender.Female, branch.Doctor);
        Assert.Equal(3, branch.ChairColor);

        double before = c.Head.Money;
        Assert.True(branch.Buy(Purchase.Hygienist));
        Assert.Equal(before - 8e6, c.Head.Money);
        double earned = c.Head.Money, rate = c.RevenuePerSecond;
        c.Tick(1);
        Assert.Equal(earned + rate, c.Head.Money, rate * 1e-3);
        Assert.Equal(c.Head.Money, branch.Money);
    }

    [Fact]
    public void Sites_change_inflow_and_price()
    {
        var gangnam = new Clinic { Site = Site.Gangnam };
        var town = new Clinic();
        var resin = Catalog.Treatments.Single(t => t.Id == "resin");
        Assert.Equal(town.PriceNow(resin) * 1.6, gangnam.PriceNow(resin), 3);
        Assert.Equal(town.Inflow / 1.2 * 0.8, gangnam.Inflow, 6);
    }

    [Fact]
    public void Specialties_apply_their_trade_offs()
    {
        var plain = new Clinic { Orthodontist = true, Ct = true };
        var ortho = new Clinic { Orthodontist = true, Ct = true, Specialty = Specialty.Orthodontics };
        Assert.True(ortho.AveragePrice > plain.AveragePrice);
        Assert.Equal(2.5e9, new Clinic { Specialty = Specialty.Orthodontics }.Cost(Purchase.Orthodontist));

        var implant = new Clinic { Specialty = Specialty.Implant };
        Assert.Equal(0, implant.Cost(Purchase.Ct));
        Assert.Equal(new Clinic().Inflow * 0.9, implant.Inflow, 6);

        var kids = new Clinic { Specialty = Specialty.Pediatric, Patients = 1000 };
        Assert.Equal(new Clinic { Patients = 2000 }.Inflow * 1.5, kids.Inflow, 6);
        Assert.True(kids.AveragePrice < new Clinic().AveragePrice);
        Assert.False(kids.Unlocked(Catalog.Treatments.Single(t => t.Id == "scaling")));
        Assert.True(kids.Unlocked(Catalog.Treatments.Single(t => t.Id == "fluoride")));
        Assert.False(new Clinic().Unlocked(Catalog.Treatments.Single(t => t.Id == "sealant")));

        double crownBase = new Clinic { Scanner = true }.AveragePrice;
        double gangnamAesthetic = new Clinic { Scanner = true, Specialty = Specialty.Aesthetic, Site = Site.Gangnam }.AveragePrice;
        double townAesthetic = new Clinic { Scanner = true, Specialty = Specialty.Aesthetic }.AveragePrice;
        Assert.True(gangnamAesthetic / 1.6 > townAesthetic);
        Assert.True(townAesthetic > crownBase);
    }

    [Fact]
    public void Local_clinics_take_half_the_event_penalty()
    {
        var local = new Clinic { Specialty = Specialty.Local };
        double rate = local.ReputationRate;
        local.ComplaintLeft = 10;
        Assert.Equal(rate * 0.975, local.ReputationRate, 6);
    }

    [Fact]
    public void Changing_specialty_costs_money_and_needs_a_different_choice()
    {
        var c = Ready();
        Assert.False(c.ChangeSpecialty(c.Head, Specialty.None));
        Assert.True(c.ChangeSpecialty(c.Head, Specialty.Local));
        Assert.Equal(1e12 - Company.ChangeSpecialtyCost, c.Head.Money);
    }

    [Fact]
    public void Auto_manage_needs_a_manager_and_then_buys_on_its_own()
    {
        var c = Ready();
        Assert.NotNull(c.Head.Blocked(Purchase.AutoManage));
        c.Head.SpaceIndex = 2;
        Assert.True(c.Head.Buy(Purchase.Manager));
        Assert.True(c.Head.Buy(Purchase.AutoManage));
        double money = c.Head.Money;
        c.Tick(5);
        Assert.True(c.Head.Money < money + c.RevenuePerSecond * 5);
    }

    [Fact]
    public void Offline_pays_every_branch_into_the_shared_wallet()
    {
        var c = Ready();
        c.Open(Site.NewTown, Specialty.None);
        double before = c.Head.Money;
        double earned = c.ApplyOffline(600);
        Assert.Equal(before + earned, c.Head.Money, 0);
        Assert.True(c.Branches[1].Revenue > 0);
    }

    [Fact]
    public void Company_survives_save_and_old_saves_still_load()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "save.json");
        var store = new SaveStore(path);
        var c = Ready();
        c.Open(Site.Gangnam, Specialty.Aesthetic);
        c.Current.Chairs = 3;
        store.Save(c, new Career { Stage = CareerStage.Done }, new EventDirector(), DateTime.UtcNow);

        var loaded = store.Load()!.Company!;
        loaded.Link(0);
        Assert.Equal(2, loaded.Branches.Count);
        Assert.Equal(1, loaded.Selected);
        Assert.Equal(c.Head.Money, loaded.Current.Money);
        Assert.Equal(Site.Gangnam, loaded.Current.Site);
        Assert.Equal(3, loaded.Current.Chairs);
        Assert.Equal(c.FameRate, loaded.Current.FameRate);

        File.WriteAllText(path, """{"Clinic":{"Money":123,"Chairs":2},"SavedUtc":"2026-10-31T00:00:00Z"}""");
        var old = store.Load()!;
        Assert.Null(old.Company);
        Assert.Equal(123, old.Clinic.Money);
        Directory.Delete(Path.GetDirectoryName(path)!, true);
    }

    [Fact]
    public void Branch_tab_is_locked_then_opens_and_switches_branches_by_tap()
    {
        var locked = new Company(new Clinic { Doctor = Gender.Male });
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        var font = SKTypeface.FromFamilyName("Malgun Gothic");
        var career = new Career { Stage = CareerStage.Done };

        using (var first = new GameScreen(locked, career, new EventDirector(), _ => null, font))
        {
            first.Render(surface.Canvas, 540, 960);
            first.PointerDown(490, 520);
            first.PointerUp(490, 520);
            first.PointerDown(460, 640);       // 잠겨 있으면 진료실 탭 그대로 — 묶음 줄이 눌린다
            first.PointerUp(460, 640);
            Assert.Single(locked.Branches);
        }

        var c = Ready();
        using var screen = new GameScreen(c, career, new EventDirector(), _ => null, font);
        void Press(float x, float y) { screen.PointerDown(x, y); screen.PointerUp(x, y); screen.Render(surface.Canvas, 540, 960); }
        screen.Render(surface.Canvas, 540, 960);

        const float top = 554, row = 88;
        Press(490, 520);                       // 지점 탭
        Press(460, top + row * 2 + 40);        // 새 지점 개원 (명성, 본점, 개원 순)
        Press(460, top + row * 1 + 40);        // 입지: 신도시
        Press(460, top + row * 2 + 40);        // 특화: 교정 전문
        Assert.Equal(2, c.Branches.Count);
        Assert.Equal(Specialty.Orthodontics, c.Current.Specialty);
        Assert.Equal(1, c.Selected);

        Press(460, top + row * 1 + 40);        // 본점으로 돌아가기
        Assert.Equal(0, c.Selected);
    }
}
