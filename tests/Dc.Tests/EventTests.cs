using Dc.Core;
using Dc.Ui;
using SkiaSharp;
using Xunit;

namespace Dc.Tests;

public class EventTests
{
    private static readonly Random Seeded = new(1);

    private static EventDirector Started(EventKind kind, Clinic clinic)
    {
        var e = new EventDirector();
        for (int i = 0; i < e.Timers.Length; i++) e.Timers[i] = 1e9;
        e.Timers[(int)kind] = 0;
        e.Tick(1, clinic, Seeded);
        return e;
    }

    [Fact]
    public void Emergency_rewards_thirty_taps_and_expires_without_penalty()
    {
        var c = new Clinic();
        var e = Started(EventKind.Emergency, c);
        Assert.Equal(EventKind.Emergency, e.Active);
        double money = c.Money, patients = c.Patients;
        for (int i = 0; i < EventDirector.EmergencyGoal; i++) e.Tap(c);
        Assert.Equal(EventKind.None, e.Active);
        Assert.Equal(money + c.RevenuePerSecond * 60, c.Money, 1);
        Assert.True(c.Patients > patients);

        var missed = Started(EventKind.Emergency, c);
        double before = c.Money;
        missed.Tick(EventDirector.EmergencySeconds, c, Seeded);
        Assert.Equal(EventKind.None, missed.Active);
        Assert.Equal(before, c.Money);
    }

    [Fact]
    public void Complaint_right_answer_keeps_reputation_and_wrong_one_costs_five_percent()
    {
        var c = new Clinic();
        double rate = c.ReputationRate;
        var e = Started(EventKind.Complaint, c);
        e.ComplaintCase = 0;
        e.Choose(1, c);
        Assert.Equal(rate * 0.95, c.ReputationRate, 6);
        c.Tick(EventDirector.ComplaintSeconds);
        Assert.Equal(0, c.ComplaintLeft);

        var right = Started(EventKind.Complaint, c);
        right.ComplaintCase = 0;
        right.Choose(0, c);
        Assert.Equal(0, c.ComplaintLeft);
    }

    [Fact]
    public void Ignoring_a_complaint_is_harmless_but_ignoring_a_resignation_loses_a_hygienist()
    {
        var c = new Clinic { Chairs = 12, Dentists = 4, Hygienists = 6, Desks = 2, SpaceIndex = 3 };
        var complaint = Started(EventKind.Complaint, c);
        complaint.Tick(EventDirector.CardSeconds, c, Seeded);
        Assert.Equal(0, c.ComplaintLeft);

        var quit = Started(EventKind.Resignation, c);
        Assert.Equal(EventKind.Resignation, quit.Active);
        quit.Tick(EventDirector.CardSeconds, c, Seeded);
        Assert.Equal(5, c.Hygienists);
    }

    [Fact]
    public void Review_raises_inflow_for_five_minutes()
    {
        var c = new Clinic();
        double inflow = c.Inflow;
        Started(EventKind.Review, c).Choose(0, c);
        Assert.Equal(inflow * 1.5, c.Inflow, 6);
        c.Tick(EventDirector.ReviewSeconds);
        Assert.Equal(0, c.ReviewLeft);
    }

    [Fact]
    public void Breakdown_needs_five_chairs_stops_one_and_repair_needs_money()
    {
        var few = new Clinic();
        Assert.Equal(EventKind.None, Started(EventKind.Breakdown, few).Active);

        var c = new Clinic { Chairs = 5, Dentists = 2, SpaceIndex = 1, Money = 0 };
        var e = Started(EventKind.Breakdown, c);
        Assert.Equal(4, c.ActiveChairs);
        e.Choose(0, c);
        Assert.Equal(EventKind.Breakdown, e.Active);
        c.Money = 1e12;
        e.Choose(0, c);
        Assert.Equal(EventKind.None, e.Active);
        Assert.Equal(5, c.ActiveChairs);
    }

    [Fact]
    public void Audit_fines_overclaimers_sometimes_and_rewards_honest_clinics_after_five()
    {
        var honest = new Clinic { SpaceIndex = 1 };
        var e = new EventDirector();
        for (int i = 0; i < EventDirector.HonestAudits; i++)
        {
            Array.Fill(e.Timers, 1e9);
            e.Timers[(int)EventKind.Audit] = 0;
            Assert.Contains("실사", e.Tick(1, honest, Seeded) is { } said && i < 4 ? said : "실사");
        }
        Assert.True(honest.ModelClinic);

        var cheat = new Clinic { SpaceIndex = 1, Overclaim = true, Money = 1e12 };
        var run = new EventDirector();
        var random = new Random(7);
        for (int i = 0; i < 40; i++)
        {
            Array.Fill(run.Timers, 1e9);
            run.Timers[(int)EventKind.Audit] = 0;
            run.Tick(1, cheat, random);
        }
        Assert.True(cheat.Money < 1e12);
        Assert.False(cheat.ModelClinic);
        Assert.Equal(0, run.AuditsPassed);
    }

    [Fact]
    public void Overclaim_raises_only_insured_treatments()
    {
        double basePrice = new Clinic().AveragePrice;
        double expected = (15_000 * 1.3 * 40 + 100_000 * 25 + 150_000 * 1.3 * 15 + 500_000 * 10) / 90;
        Assert.True(new Clinic { Overclaim = true }.AveragePrice > basePrice);
        Assert.Equal(expected, new Clinic { Overclaim = true }.AveragePrice, 3);
    }

    [Fact]
    public void Seasons_double_the_treatment_weight()
    {
        double normal = new Clinic().AverageSeconds;
        Assert.True(new Clinic { Month = 12 }.AverageSeconds < normal);          // 12월 스케일링이 몰려 평균이 짧아진다
        var ortho = new Clinic { Orthodontist = true };
        var november = new Clinic { Orthodontist = true, Month = 11 };
        Assert.True(november.AveragePrice > ortho.AveragePrice);
    }

    [Fact]
    public void Moving_brings_the_wreath_but_the_preview_ignores_it()
    {
        var c = new Clinic { Money = 1e9, Chairs = 3 };
        double inflow = new Clinic { SpaceIndex = 1 }.Inflow;
        Assert.True(c.Buy(Purchase.Move));
        Assert.True(c.WreathLeft > 0);
        Assert.Equal(inflow * 2, c.Inflow, 6);
    }

    [Fact]
    public void Salesman_comes_when_inflow_stalls_and_gives_a_fitting_hint()
    {
        var c = new Clinic { Chairs = 3, Hygienists = 3 };
        var e = new EventDirector();
        Array.Fill(e.Timers, 1e9);
        Assert.True(c.Inflow < c.Capacity);
        for (int i = 0; i < EventDirector.StallLimit; i++) e.Tick(1, c, Seeded);
        Assert.Equal(EventKind.Salesman, e.Active);
        Assert.Contains("데스크", e.Card(c)!.Body);
        e.Choose(0, c);
        Assert.Equal(EventKind.None, e.Active);
        for (int i = 0; i < EventDirector.StallLimit; i++) e.Tick(1, c, Seeded);
        Assert.Equal(EventKind.None, e.Active);          // 바로 또 오지 않는다
    }

    [Fact]
    public void Hygienist_recommends_rest_at_seventy_percent_fatigue_once()
    {
        var c = new Clinic { Hygienists = 1, Fatigue = 75 };
        var e = new EventDirector();
        e.Tick(1, c, Seeded);
        Assert.Equal(EventKind.RestAdvice, e.Active);
        Assert.Equal("치과위생사", e.Card(c)!.Title);
        e.Choose(1, c);                                  // 조금만 더 한다
        Assert.False(c.Resting);
        e.Tick(1, c, Seeded);
        Assert.Equal(EventKind.None, e.Active);          // 또 묻지 않는다

        c.Fatigue = 10;
        e.Tick(1, c, Seeded);
        c.Fatigue = 90;
        e.Tick(1, c, Seeded);
        Assert.Equal(EventKind.RestAdvice, e.Active);    // 풀렸다 다시 차면 또 권한다
        Assert.Contains("휴식", e.Choose(0, c));
        Assert.True(c.Resting);
    }

    [Fact]
    public void Old_saves_with_fewer_timers_still_tick()
    {
        var e = new EventDirector { Timers = [0, 240, 600, 900, 1800, 3600, 7200] };
        e.Tick(1, new Clinic(), Seeded);
        Assert.Equal(239, e.Timers[1]);
    }

    [Fact]
    public void Offline_time_brings_no_events_but_effects_run_out()
    {
        var c = new Clinic { ReviewLeft = 100, BrokenLeft = 100, ComplaintLeft = 100 };
        c.ApplyOffline(3600);
        Assert.Equal(0, c.ReviewLeft);
        Assert.Equal(0, c.BrokenLeft);
        Assert.Equal(0, c.ComplaintLeft);
    }

    [Fact]
    public void Event_state_is_saved_with_the_game()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "save.json");
        var store = new SaveStore(path);
        var c = new Clinic { Overclaim = true, ReviewLeft = 12, ModelClinic = true };
        var e = new EventDirector { AuditsPassed = 3 };
        e.Timers[1] = 77;
        store.Save(c, new Career { Stage = CareerStage.Done }, e, DateTime.UtcNow);
        var loaded = store.Load()!;
        Assert.True(loaded.Clinic.Overclaim);
        Assert.Equal(12, loaded.Clinic.ReviewLeft);
        Assert.True(loaded.Clinic.ModelClinic);
        Assert.Equal(3, loaded.Events!.AuditsPassed);
        Assert.Equal(77, loaded.Events.Timers[1]);
        Directory.Delete(Path.GetDirectoryName(path)!, true);
    }

    [Fact]
    public void Screen_shows_the_card_and_its_buttons_work()
    {
        var clinic = new Clinic { Doctor = Gender.Male };
        var events = Started(EventKind.Complaint, clinic);
        events.ComplaintCase = 0;
        using var screen = new GameScreen(clinic, new Career { Stage = CareerStage.Done }, events, _ => null, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        screen.Render(surface.Canvas, 540, 960);

        double money = clinic.Money;
        screen.PointerDown(270, 170);    // 카드 윗부분은 진료로 넘어가지 않는다
        screen.PointerUp(270, 170);
        Assert.Equal(money, clinic.Money);
        Assert.Equal(EventKind.Complaint, events.Active);

        screen.PointerDown(270, 340);    // 첫 번째 선택지
        screen.PointerUp(270, 340);
        Assert.Equal(EventKind.None, events.Active);
        Assert.Equal(0, clinic.ComplaintLeft);
    }
}
