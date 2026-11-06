using Dc.Core;
using Dc.Ui;
using SkiaSharp;
using Xunit;

namespace Dc.Tests;

public class AbilityTests
{
    private static Clinic Crowded() => new() { Chairs = 12, Dentists = 4, Hygienists = 12, Desks = 3, SpaceIndex = 3, Skill = 5, Scanner = true };

    private static double ClaimsAfter(Clinic c, int seconds)
    {
        for (int i = 0; i < seconds; i++) c.Tick(1);
        return c.Claims;
    }

    [Fact]
    public void Busy_fast_clinics_build_claims_and_reputation_falls()
    {
        Assert.True(ClaimsAfter(Crowded(), 3600) > 0);
        Assert.Equal(new Clinic().ReputationRate * 0.85, new Clinic { Claims = 50 }.ReputationRate, 6);
    }

    [Fact]
    public void Fatigue_rises_with_clicks_lowers_their_value_raises_claims_and_rests_away()
    {
        var c = new Clinic();
        double fresh = c.ClickValue;
        for (int i = 0; i < 200; i++) c.Click();
        Assert.True(c.Fatigue > 40);
        Assert.True(c.ClickValue < fresh);
        double tired = c.Fatigue;
        c.Tick(10);
        Assert.Equal(tired - 10, c.Fatigue, 3);
        c.Fatigue = 100;
        for (int i = 0; i < 100; i++) c.Click();
        Assert.Equal(100, c.Fatigue, 3);
        Assert.InRange(c.ClickValue / fresh, 0.29, 0.3001);   // 쌓인 클레임만큼 평판이 살짝 낮다

        var calm = Crowded();
        var worn = Crowded();
        worn.Fatigue = 100;
        worn.Mastery.Abilities[Ability.Stamina] = 0;
        worn.Tick(1);
        calm.Tick(1);
        Assert.True(worn.Claims >= calm.Claims);
    }

    [Fact]
    public void Rest_recovers_five_times_faster_and_blocks_direct_treatment()
    {
        var c = new Clinic { Fatigue = 100 };
        Assert.True(c.Rest());
        Assert.False(c.Rest());
        double money = c.Money;
        Assert.Equal(0, c.Click());
        Assert.Equal(money, c.Money);
        c.Tick(10);
        Assert.Equal(50, c.Fatigue, 3);
        c.Tick(20);
        Assert.False(c.Resting);
        Assert.True(c.Click() > 0);
    }

    [Fact]
    public void Satisfying_treatment_recovers_fatigue_by_chance()
    {
        var c = new Clinic { Fatigue = 50 };
        Assert.True(c.TrySatisfy(new Random(1), 1000));   // 환자가 충분히 많으면 반드시 나온다
        Assert.Equal(50 - Clinic.SatisfyRecovery, c.Fatigue);
        Assert.False(c.TrySatisfy(new Random(1), 0));

        var random = new Random(5);
        var odds = new Clinic { Fatigue = 100 };
        int hits = 0;
        for (int i = 0; i < 20000; i++)
        {
            odds.Fatigue = 100;
            if (odds.TrySatisfy(random, 1)) hits++;
        }
        Assert.InRange(hits, 300, 500);                   // 약 2%

        var rested = new Clinic { Fatigue = 0 };
        Assert.False(rested.TrySatisfy(new Random(1), 1000));
        Assert.True(new Clinic { Mastery = new Mastery { Abilities = new() { [Ability.Care] = 5 } } }.SatisfyChance > new Clinic().SatisfyChance);
    }

    [Fact]
    public void Tapping_fatigue_in_the_header_rests()
    {
        var c = new Clinic { Doctor = Gender.Male, Fatigue = 80 };
        using var screen = new GameScreen(c, new Career { Stage = CareerStage.Done }, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        screen.Render(surface.Canvas, 540, 960);
        screen.PointerDown(490, 120);
        screen.PointerUp(490, 120);
        Assert.True(c.Resting);
    }

    [Fact]
    public void Harder_treatments_raise_risk_and_mastery_lowers_it()
    {
        var hard = Crowded();
        hard.Ct = true;
        hard.Orthodontist = true;
        double easy = ClaimsAfter(Crowded(), 1800);
        Assert.True(ClaimsAfter(hard, 1800) >= easy);
        Assert.True(hard.AverageDifficulty > new Clinic().AverageDifficulty);
        var mastery = new Mastery();
        var implant = Catalog.Treatments.Single(t => t.Id == "implant");
        double risk = mastery.Risk(implant);
        mastery.AddPatients("implant", 5000);
        Assert.True(mastery.Risk(implant) < risk);
    }

    [Fact]
    public void Claims_stay_between_zero_and_one_hundred()
    {
        var c = Crowded();
        c.Mastery.Abilities[Ability.Care] = 0;
        ClaimsAfter(c, 100_000);
        Assert.InRange(c.Claims, 0, 100);
        // 작은 치과도 진료를 하면 클레임이 0 에 붙지 않고 어느 선에서 머문다
        Assert.InRange(ClaimsAfter(new Clinic(), 3600), 1, 20);
        Assert.InRange(ClaimsAfter(new Clinic { Claims = 100 }, 3600), 1, 20);
    }

    [Fact]
    public void Technique_and_care_lower_claims()
    {
        double plain = ClaimsAfter(Crowded(), 1800);
        var skilled = Crowded();
        skilled.Mastery.Abilities[Ability.Diagnosis] = 8;
        var caring = Crowded();
        caring.Mastery.Abilities[Ability.Care] = 8;
        Assert.True(ClaimsAfter(skilled, 1800) < plain);
        Assert.True(ClaimsAfter(caring, 1800) < plain);
    }

    [Fact]
    public void Offline_time_adds_no_claims()
    {
        var c = Crowded();
        c.ApplyOffline(3600);
        Assert.Equal(0, c.Claims);
    }

    [Fact]
    public void Abilities_change_the_rules_and_cost_more_each_level()
    {
        var c = new Clinic { Money = 1e12, Chairs = 3, Scanner = true };
        double first = c.Mastery.AbilityCost(Ability.Technique), price = c.AveragePrice;
        Assert.True(c.BuyAbility(Ability.Technique));
        Assert.Equal(price * 1.04, c.AveragePrice, 3);
        Assert.True(c.Mastery.AbilityCost(Ability.Technique) > first);

        double consult = c.AveragePrice;
        c.BuyAbility(Ability.Consult);
        Assert.True(c.AveragePrice > consult);

        double inflow = c.Inflow;
        c.BuyAbility(Ability.Care);
        Assert.Equal(inflow * 1.03, c.Inflow, 6);

        double capacity = c.Capacity;
        c.BuyAbility(Ability.Leadership);
        Assert.True(c.Capacity > capacity);
    }

    [Fact]
    public void Handling_claims_costs_money_and_needs_claims()
    {
        var c = new Clinic { Money = 1e9, Claims = 30 };
        double cost = c.ClaimHandleCost;
        Assert.True(c.HandleClaims());
        Assert.Equal(10, c.Claims);
        Assert.Equal(1e9 - cost, c.Money);
        c.Claims = 0;
        Assert.False(c.HandleClaims());
    }

    [Fact]
    public void Many_claims_bring_complaints_sooner()
    {
        var calm = new EventDirector();
        var angry = new EventDirector();
        var random = new Random(1);
        calm.Tick(100, new Clinic(), random);
        angry.Tick(100, new Clinic { Claims = 100 }, random);
        Assert.True(angry.Timers[(int)EventKind.Complaint] < calm.Timers[(int)EventKind.Complaint]);
    }

    [Fact]
    public void Skill_tab_sits_left_of_the_clinic_tab_and_buys_abilities()
    {
        var c = new Clinic { Doctor = Gender.Male, Money = 1e10 };
        using var screen = new GameScreen(c, new Career { Stage = CareerStage.Done }, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        void Press(float x, float y) { screen.PointerDown(x, y); screen.PointerUp(x, y); screen.Render(surface.Canvas, 540, 960); }
        screen.Render(surface.Canvas, 540, 960);
        const float top = 554, row = 88;
        Press(50, 520);                    // 스킬 탭
        Press(450, 574);                   // 상담 스킬
        Press(460, top + 44 + row + 40);   // 정확한 진단 (응대 다음)
        Assert.Equal(1, c.Mastery.Level(Ability.Diagnosis));
        Press(270, 574);                   // 경영 스킬
        Press(460, top + 44 + row + 40);   // 직원 관리 (피로 다음)
        Assert.Equal(1, c.Mastery.Level(Ability.Leadership));
    }
}
