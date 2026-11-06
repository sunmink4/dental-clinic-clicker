using Dc.Core;
using Dc.Ui;
using SkiaSharp;
using Xunit;
using Xunit.Abstractions;

namespace Dc.Tests;

public class ClinicTests(ITestOutputHelper output)
{
    private static Career Opened => new() { Stage = CareerStage.Done };

    [Theory]
    // 크라운(비급여)은 처음부터 한다. 스캐너는 보철 시간만 25% 줄인다
    [InlineData(false, false, false, 115_000, 5.389)]
    [InlineData(true, false, false, 115_000, 5.056)]
    [InlineData(true, true, false, 193_299, 6.134)]
    [InlineData(true, true, true, 307_500, 6.85)]
    public void Averages_match_the_design_table(bool scanner, bool ct, bool ortho, double price, double seconds)
    {
        var c = new Clinic { Scanner = scanner, Ct = ct, Orthodontist = ortho };
        Assert.InRange(c.AveragePrice, price - 1, price + 1);
        Assert.InRange(c.AverageSeconds, seconds - 0.01, seconds + 0.01);
    }

    [Fact]
    public void Fourth_chair_alone_adds_nothing_but_the_bundle_does()
    {
        var c = new Clinic { Chairs = 3, Hygienists = 3, SpaceIndex = 1, Desks = 5 };
        Assert.Equal(0, c.Gain(Purchase.Chair));
        Assert.True(c.Gain(Purchase.Dentist) <= 0);   // 체어가 없으면 월급만 나간다
        Assert.True(c.Gain(Purchase.Bundle) > 0);
    }

    [Fact]
    public void Costs_match_the_design_formulas()
    {
        var c = new Clinic { Chairs = 6, Dentists = 2, Skill = 2 };
        Assert.Equal(3e7 * Math.Pow(1.22, 5), c.Cost(Purchase.Chair));
        Assert.Equal(1.2e8 * 1.55, c.Cost(Purchase.Dentist));
        Assert.Equal(2e7 * 5.76, c.Cost(Purchase.Skill), 1);
    }

    [Fact]
    public void Move_needs_full_chairs_keeps_everything_and_halves_throughput_while_building()
    {
        var c = new Clinic { Money = 1e9, Chairs = 2, Hygienists = 2 };
        Assert.False(c.Buy(Purchase.Move));
        c.Chairs = 3;
        double served = c.ServedPerSecond;
        Assert.True(c.Buy(Purchase.Move));
        Assert.Equal(1, c.SpaceIndex);
        Assert.Equal(3, c.Chairs);
        Assert.Equal(2, c.Hygienists);
        Assert.Equal(30, c.BuildLeft);
        Assert.False(c.Buy(Purchase.Move));
        Assert.True(c.ServedPerSecond < served);
        c.Tick(30);
        Assert.False(c.Building);
    }

    [Fact]
    public void Offline_is_capped_at_eight_hours_at_half_rate_and_ignores_clock_going_back()
    {
        var eight = new Clinic();
        var week = new Clinic();
        double a = eight.ApplyOffline(8 * 3600), b = week.ApplyOffline(7 * 24 * 3600);
        Assert.Equal(a, b);
        Assert.Equal(0, new Clinic().ApplyOffline(-100));
        Assert.True(a > 0);
        Assert.True(a < new Clinic().RevenuePerSecond * 3 * 8 * 3600 * 0.5);
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1500, "1,500")]
    [InlineData(29_000, "2.9만")]
    [InlineData(3e7, "3,000만")]
    [InlineData(1.5e8, "1.5억")]
    [InlineData(99_999_999, "9,999만")]
    [InlineData(4.2e12, "4.2조")]
    [InlineData(5e16, "5경")]
    public void Korean_numbers(double value, string text) => Assert.Equal(text, KoreanNumber.Format(value));

    [Fact]
    public void Save_round_trips_and_a_broken_file_is_set_aside()
    {
        string dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        string path = Path.Combine(dir, "save.json");
        var store = new SaveStore(path);
        var when = new DateTime(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);
        store.Save(new Clinic { Doctor = Gender.Female, Money = 123, Chairs = 3, SpaceIndex = 2, Scanner = true, BuildLeft = 9 }, new Career { Stage = CareerStage.Resident, ScalingDone = 7 }, new EventDirector(), when);
        var loaded = store.Load()!;
        Assert.Equal(Gender.Female, loaded.Clinic.Doctor);
        Assert.Equal(7, loaded.Career!.ScalingDone);
        Assert.Equal(when, loaded.SavedUtc);
        Assert.Equal(123, loaded.Clinic.Money);
        Assert.Equal(3, loaded.Clinic.Chairs);
        Assert.Equal(2, loaded.Clinic.SpaceIndex);
        Assert.True(loaded.Clinic.Scanner);
        Assert.Equal(9, loaded.Clinic.BuildLeft);

        File.WriteAllText(path, "{ not json");
        Assert.Null(store.Load());
        Assert.True(File.Exists(path + ".bad"));
        Directory.Delete(dir, true);
    }

    /// <summary>기획 09 와 같은 탐욕 전략(처음 1시간 2클릭/s). 수치 밸런스가 기획대로 나오는지 본다.</summary>
    [Fact]
    public void Greedy_play_reaches_milestones_near_the_design_simulation()
    {
        var buys = new[] { Purchase.Chair, Purchase.Dentist, Purchase.Bundle, Purchase.Hygienist, Purchase.Desk, Purchase.Sterilizer, Purchase.Skill, Purchase.Scanner, Purchase.Ct, Purchase.Orthodontist };
        var c = new Clinic();
        var reached = new Dictionary<string, double>();
        const int step = 5;                    // 1초 단위는 너무 오래 걸린다
        for (int t = 0; t < 72 * 3600; t += step)
        {
            if (t < 3600) for (int i = 0; i < 2 * step; i++) c.Click();
            c.Tick(step);
            if (c.Blocked(Purchase.Move) == null && c.Money >= c.Cost(Purchase.Move))
            {
                c.Buy(Purchase.Move);
                reached.TryAdd(c.Space.Name, t);
                continue;
            }
            var options = buys.Where(p => c.Blocked(p) == null && c.Gain(p) > 0).ToList();
            if (options.Count == 0) continue;
            var best = options.MinBy(p => c.Cost(p) / c.Gain(p));
            if (c.Buy(best) && best is Purchase.Scanner or Purchase.Ct or Purchase.Orthodontist) reached.TryAdd(best.ToString(), t);
        }
        foreach (var (name, t) in reached) output.WriteLine($"{name}: {t / 3600:0.00}h");
        Assert.InRange(reached["상가 확장"] / 3600, 0.3, 2);
        Assert.InRange(reached["메디컬빌딩 한 층"] / 3600, 1.5, 8);
        Assert.InRange(reached["치과병원"] / 3600, 12, 60);
    }

    [Fact]
    public void Recolor_changes_only_the_cobalt_key_and_keeps_its_shading_and_alpha()
    {
        var bright = new SKColor(0x00, 0x47, 0xFF);
        var shaded = new SKColor(0x00, 0x24, 0x80, 200);
        var skin = new SKColor(0xE8, 0xBE, 0xA0);
        var sky = new SKColor(0xB8, 0xD4, 0xEE);
        var target = new SKColor(0xE0, 0x7A, 0x5F);

        var result = Palette.Recolor([bright, shaded, skin, sky], target);

        Assert.InRange((int)result[0].Red, target.Red, 255);
        Assert.True(result[0].Red > result[0].Blue);
        Assert.True(result[1].Red < result[0].Red);
        Assert.Equal(200, result[1].Alpha);
        Assert.Equal(skin, result[2]);
        Assert.Equal(sky, result[3]);
    }

    [Theory]
    [InlineData(Gender.Female, "chair_busy_f.png", "chair_busy_m.png")]
    [InlineData(Gender.Male, "chair_busy_m.png", "chair_busy_f.png")]
    public void Busy_chairs_show_the_protagonist_then_hired_dentists_of_the_other_gender(Gender gender, string own, string hired)
    {
        var clinic = new Clinic { Doctor = gender, SpaceIndex = 2, Chairs = 4, Dentists = 2, Desks = 30, Patients = 1e6 };
        var asked = new List<string>();
        Stream? Open(string name) { asked.Add(name); return null; }
        using var screen = new GameScreen(clinic, Opened, new EventDirector(), Open, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        screen.Render(surface.Canvas, 540, 960);
        Assert.Contains(own, asked);
        Assert.Contains(hired, asked);
        Assert.Contains("chair_wait.png", asked);
    }

    [Fact]
    public void One_dentist_visits_one_chair_at_a_time_and_hygienists_work_alone_elsewhere()
    {
        var clinic = new Clinic { Doctor = Gender.Male, Chairs = 3, Hygienists = 3, Desks = 30, Patients = 1e6 };
        var asked = new List<string>();
        Stream? Open(string name) { asked.Add(name); return null; }
        using var screen = new GameScreen(clinic, Opened, new EventDirector(), Open, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        screen.Render(surface.Canvas, 540, 960);
        Assert.Single(asked, n => n == "chair_busy_hyg_m.png");
        Assert.Contains("chair_hyg_only.png", asked);
    }

    [Fact]
    public void Auto_click_toggle_and_spinner_work_and_are_saved()
    {
        var clinic = new Clinic { Doctor = Gender.Male };
        using var screen = new GameScreen(clinic, Opened, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        void Press(float x, float y) { screen.PointerDown(x, y); screen.PointerUp(x, y); screen.Render(surface.Canvas, 540, 960); }
        screen.Render(surface.Canvas, 540, 960);

        Press(438, 66);                        // +
        Assert.Equal(250, clinic.AutoClickMs);
        Press(336, 66);                        // −
        Press(336, 66);
        Assert.Equal(150, clinic.AutoClickMs);
        for (int i = 0; i < 9; i++) Press(336, 66);
        Assert.Equal(GameScreen.AutoMin, clinic.AutoClickMs);

        double money = clinic.Money;
        screen.Update(1);
        Assert.Equal(money + new Clinic().RevenuePerSecond, clinic.Money, 0);   // 끔: 진료 수입만
        Press(490, 66);
        Assert.True(clinic.AutoClick);
        money = clinic.Money;
        screen.Update(0.2);                    // 50ms 간격이면 4회
        Assert.InRange(clinic.Money - money, clinic.ClickValue * 3.5, clinic.ClickValue * 4.5 + 2000);
    }

    [Fact]
    public void Move_tab_relocates_with_three_chairs_and_enough_money()
    {
        var clinic = new Clinic { Doctor = Gender.Male, Chairs = 3, Money = 2e8 };
        using var screen = new GameScreen(clinic, Opened, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        void Press(float x, float y) { screen.PointerDown(x, y); screen.PointerUp(x, y); screen.Render(surface.Canvas, 540, 960); }
        screen.Render(surface.Canvas, 540, 960);
        Press(420, 520);                       // 이전 탭
        Press(460, 554 + 40);
        Assert.Equal(1, clinic.SpaceIndex);
    }

    [Fact]
    public void New_game_asks_first_then_calls_the_host()
    {
        int started = 0;
        var clinic = new Clinic { Doctor = Gender.Male };
        using var screen = new GameScreen(clinic, Opened, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic")) { NewGame = () => started++ };
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        void Press(float x, float y) { screen.PointerDown(x, y); screen.PointerUp(x, y); screen.Render(surface.Canvas, 540, 960); }
        screen.Render(surface.Canvas, 540, 960);
        Press(36, 36);                         // 메뉴
        Press(460, 554 + 50 * 3 + 25);         // 새 게임 (내 정보, 꾸미기, 장비 배치 다음)
        Assert.Equal(0, started);
        Press(460, 554 + 88 + 40);             // 취소
        Assert.Equal(0, started);
        Press(460, 554 + 50 * 3 + 25);         // 새 게임
        Press(460, 554 + 40);                  // 시작
        Assert.Equal(1, started);
    }

    [Fact]
    public void Three_save_slots_are_separate_and_slot_one_is_the_old_file()
    {
        string folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var slots = new SaveSlots(folder);
        Assert.Equal(1, slots.Current);
        Assert.Equal("비어 있음", slots.Summary(2));
        new SaveStore(Path.Combine(folder, "save.json")).Save(new Clinic { Money = 111 }, new Career { Stage = CareerStage.Done }, new EventDirector(), DateTime.UtcNow);
        slots.Store(2).Save(new Clinic { Money = 222 }, new Career(), new EventDirector(), DateTime.UtcNow);
        Assert.Equal(111, slots.Store(1).Load()!.Company!.Head.Money);
        Assert.Equal(222, slots.Store(2).Load()!.Company!.Head.Money);
        Assert.Contains("25평", slots.Summary(1));
        Assert.Contains("수련 중", slots.Summary(2));
        slots.Select(2);
        Assert.Equal(2, new SaveSlots(folder).Current);
        Directory.Delete(folder, true);
    }

    [Fact]
    public void Slot_menu_switches_through_the_host()
    {
        string folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        int chosen = 0;
        var clinic = new Clinic { Doctor = Gender.Male };
        using var screen = new GameScreen(clinic, Opened, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic")) { Slots = new SaveSlots(folder), SwitchSlot = n => chosen = n };
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        void Press(float x, float y) { screen.PointerDown(x, y); screen.PointerUp(x, y); screen.Render(surface.Canvas, 540, 960); }
        screen.Render(surface.Canvas, 540, 960);
        Press(36, 36);
        Press(460, 554 + 50 * 3 + 25);         // 저장 슬롯
        Press(460, 554 + 40);                  // 사용 중인 1번은 그대로
        Assert.Equal(0, chosen);
        Press(460, 554 + 88 * 2 + 40);         // 3번
        Assert.Equal(3, chosen);
    }

    [Fact]
    public void Large_staff_tires_hygienists_and_sterilizers_relieve_them()
    {
        var small = new Clinic { Chairs = 4, Hygienists = 4, SpaceIndex = 1 };
        small.Tick(1000);
        Assert.Equal(0, small.StaffFatigue);

        var big = new Clinic { Chairs = 12, Dentists = 4, Hygienists = 10, SpaceIndex = 3, Money = 1e10 };
        double fresh = big.Capacity;
        big.Tick(1000);
        Assert.True(big.StaffFatigue > 50);
        Assert.True(big.Capacity < fresh);
        for (int i = 0; i < 2; i++) Assert.True(big.Buy(Purchase.Sterilizer));
        big.Tick(1000);
        Assert.Equal(0, big.StaffFatigue);
        Assert.NotNull(new Clinic().Blocked(Purchase.Sterilizer));
    }

    [Fact]
    public void Tapping_inflow_and_capacity_opens_their_factor_lists()
    {
        var clinic = new Clinic { Doctor = Gender.Male, Desks = 2 };
        Assert.Contains(clinic.InflowFactors, f => f.Name == "데스크 2명" && f.Value == "×1.8");
        Assert.Contains(clinic.CapacityFactors, f => f.Name.StartsWith("위생사 피로"));
        using var screen = new GameScreen(clinic, Opened, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        void Press(float x, float y) { screen.PointerDown(x, y); screen.PointerUp(x, y); screen.Render(surface.Canvas, 540, 960); }
        screen.Render(surface.Canvas, 540, 960);
        double money = clinic.Money;
        Press(60, 122);                        // 유입 목록
        Press(460, 620);                       // 목록을 누르면 닫히고 구매는 일어나지 않는다
        Assert.Equal(money, clinic.Money);
        Press(220, 122);                       // 처리 목록
        Press(100, 520);
        Assert.Equal(money, clinic.Money);
    }

    private static Treatment Of(string id) => Catalog.Treatments.Single(t => t.Id == id);

    [Fact]
    public void Schedule_replaces_patient_demand_and_is_capped_at_ten_slots()
    {
        var c = new Clinic();
        double seconds = c.AverageSeconds;
        for (int i = 0; i < 10; i++) Assert.True(c.AddSlot(Of("resin")));
        Assert.False(c.AddSlot(Of("root")));
        Assert.Equal(100_000, c.AveragePrice, 3);
        Assert.Equal(5, c.AverageSeconds, 3);
        Assert.NotEqual(seconds, c.AverageSeconds);
        c.RemoveSlot(Of("resin"));
        Assert.True(c.AddSlot(Of("root")));
        c.Schedule.Clear();
        Assert.Equal(115_000, c.AveragePrice, 3);
    }

    [Fact]
    public void Schedule_on_a_locked_treatment_does_not_break_the_clinic()
    {
        var c = new Clinic { Schedule = new() { ["implant"] = 10 } };
        Assert.Equal(0, c.ScheduledSlots);
        Assert.Equal(115_000, c.AveragePrice, 3);
    }

    [Fact]
    public void Auto_schedule_favors_what_this_doctor_does_well()
    {
        var c = new Clinic();
        c.AutoSchedule();
        Assert.Equal(Clinic.DaySlots, c.ScheduledSlots);
        var best = Catalog.Treatments.Where(c.Unlocked).MaxBy(c.Efficiency)!;
        Assert.Equal(5, c.Slots(best));

        var skilled = new Clinic();
        skilled.Mastery.AddPatients("root", 1e6);
        skilled.AutoSchedule();
        Assert.Equal(5, skilled.Slots(Of("root")));
    }

    [Fact]
    public void Hard_days_tire_the_director_more()
    {
        var easy = new Clinic { Chairs = 3, Hygienists = 3, Desks = 8 };
        easy.AddSlot(Of("scaling"));
        var hard = new Clinic { Chairs = 3, Hygienists = 3, Desks = 8 };
        hard.AddSlot(Of("root"));
        easy.Fatigue = hard.Fatigue = 50;
        easy.Tick(1);
        hard.Tick(1);
        Assert.True(hard.Fatigue > easy.Fatigue - 0.5);
        Assert.True(hard.AverageDifficulty > easy.AverageDifficulty);
    }

    [Fact]
    public void Chair_treatment_follows_the_schedule()
    {
        var c = new Clinic();
        c.AddSlot(Of("root"));
        for (int seed = 0; seed < 50; seed++) Assert.Equal("root", c.TreatmentAt(seed).Id);
        Assert.Contains(Enumerable.Range(0, 200).Select(s => new Clinic().TreatmentAt(s).Id), id => id == "resin");
    }

    [Fact]
    public void Schedule_tab_adds_and_auto_assigns_slots()
    {
        var clinic = new Clinic { Doctor = Gender.Male };
        using var screen = new GameScreen(clinic, Opened, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        void Press(float x, float y) { screen.PointerDown(x, y); screen.PointerUp(x, y); screen.Render(surface.Canvas, 540, 960); }
        screen.Render(surface.Canvas, 540, 960);
        Press(220, 520);                       // 일정 탭
        Press(490, 554 + 88 * 2 + 40);         // 스케일링 +
        Assert.Equal(1, clinic.Slots(Of("scaling")));
        Press(430, 554 + 88 * 2 + 40);         // 스케일링 -
        Assert.Equal(0, clinic.Slots(Of("scaling")));
        Press(460, 554 + 40);                  // 자동 배정
        Assert.Equal(Clinic.DaySlots, clinic.ScheduledSlots);
        Press(460, 554 + 88 + 40);             // 지우기
        Assert.Equal(0, clinic.ScheduledSlots);
    }

    [Fact]
    public void Game_logic_runs_every_sixth_frame_without_losing_time()
    {
        var clinic = new Clinic { Doctor = Gender.Male };
        var reference = new Clinic();
        using var screen = new GameScreen(clinic, Opened, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic"));
        double money = clinic.Money;
        for (int i = 0; i < 5; i++) screen.Update(1 / 60.0);
        Assert.Equal(money, clinic.Money);
        screen.Update(1 / 60.0);
        reference.Tick(0.1);
        Assert.Equal(reference.Money, clinic.Money, 3);
    }

    [Fact]
    public void Profile_shows_stats_major_and_favored_subjects()
    {
        var career = new Career { Trait = Trait.Careful, Major = Major.Conservative, Title = "수석" };
        var clinic = new Clinic();
        clinic.Mastery.Favored = ["root"];
        var rows = career.Profile(clinic.Mastery);
        Assert.Contains(rows, r => r.Name == "꼼꼼함" && r.Value == "5");
        Assert.Contains(rows, r => r.Name == "전공" && r.Value == "보존과");
        Assert.Contains(rows, r => r.Name == "잘하는 과목" && r.How == "신경치료");
    }

    [Fact]
    public void Wiki_opens_from_the_menu_and_reads_topics()
    {
        Assert.Contains(Wiki.Topics, t => t.Title == "평판" && t.Lines.Any(l => l.Contains("진료과 평판")));
        var clinic = new Clinic { Doctor = Gender.Male };
        using var screen = new GameScreen(clinic, Opened, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        void Press(float x, float y) { screen.PointerDown(x, y); screen.PointerUp(x, y); screen.Render(surface.Canvas, 540, 960); }
        screen.Render(surface.Canvas, 540, 960);
        double money = clinic.Money;
        Press(36, 36);
        Press(100, 554 + 56 * 2 + 30);         // 위키 (내 정보, 꾸미기 다음)
        for (int i = 0; i < Wiki.Topics.Length; i++)
        {
            Press(100, 60 + 52 * i + 26);      // 주제 열기
            screen.Wheel(-5);
            screen.Render(surface.Canvas, 540, 960);
            Press(100, 30);                    // 목록으로
        }
        Press(100, 30);                        // 메뉴로
        Press(100, 520);                       // 닫기
        Press(270, 300);
        Assert.True(clinic.Money > money);
    }

    [Fact]
    public void Spare_hygienists_treat_alone_on_chairs_without_a_dentist()
    {
        var c = new Clinic { Chairs = 5, Hygienists = 3, SpaceIndex = 1, Desks = 30 };
        Assert.Equal(0, c.HygieneChairs);
        double before = c.Capacity;
        c.Hygienists = 5;
        Assert.Equal(2, c.HygieneChairs);
        Assert.Equal(before * (3 + 2 * c.HygieneChairRate) / 3, c.Capacity, 6);
        Assert.Equal(156.0 / 485, c.HygieneChairRate, 6);    // 기본 4종 중 스케일링 · 임프레션 몫
    }

    [Fact]
    public void Consulting_hygienists_raise_uninsured_prices_and_ease_claims()
    {
        var c = new Clinic { Chairs = 3, Hygienists = 3, Scanner = true, Money = 1e10 };
        var crown = Catalog.Treatments.Single(t => t.Id == "crown");
        var resin = Catalog.Treatments.Single(t => t.Id == "resin");
        double crownPrice = c.PriceNow(crown), resinPrice = c.PriceNow(resin);
        Assert.True(c.Buy(Purchase.Consultant));
        Assert.Equal(crownPrice * 1.06, c.PriceNow(crown), 3);
        Assert.Equal(resinPrice, c.PriceNow(resin), 3);
        Assert.NotNull(new Clinic().Blocked(Purchase.Consultant));
        c.Buy(Purchase.Consultant);
        c.Buy(Purchase.Consultant);
        Assert.NotNull(c.Blocked(Purchase.Consultant));

        var plain = new Clinic { Claims = 50 };
        var helped = new Clinic { Claims = 50, Consultants = 2 };
        plain.Tick(10);
        helped.Tick(10);
        Assert.True(helped.Claims < plain.Claims);
    }

    [Fact]
    public void Hygienists_do_scaling_and_impressions_not_root_canals()
    {
        var basic = new Clinic();
        var jobs = Enumerable.Range(0, 100).Select(basic.HygieneJob).Distinct().ToList();
        Assert.Contains("스케일링", jobs);
        Assert.Contains("임프레션", jobs);   // 크라운 본뜨기
        Assert.DoesNotContain("신경치료", jobs);
        Assert.True(basic.HygieneChairRate > 0);
        Assert.Equal(0, Clinic.HygieneShare("root"));
        Assert.Equal(1, new Clinic { Schedule = new() { ["scaling"] = 10 } }.HygieneChairRate, 6);
    }

    [Fact]
    public void Reset_sets_the_old_save_aside()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "save.json");
        var store = new SaveStore(path);
        store.Save(new Clinic(), new Career(), new EventDirector(), DateTime.UtcNow);
        store.Reset();
        Assert.Null(store.Load());
        Assert.True(File.Exists(path + ".old"));
        Directory.Delete(Path.GetDirectoryName(path)!, true);
    }

    [Fact]
    public void Tapping_a_chair_badge_hires_right_there()
    {
        var clinic = new Clinic { Doctor = Gender.Male };
        using var screen = new GameScreen(clinic, Opened, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        screen.Render(surface.Canvas, 540, 960);
        double money = clinic.Money;
        screen.PointerDown(90, 318);
        screen.PointerUp(90, 318);
        Assert.Equal(3, clinic.StaffCandidates.Count);   // 표시를 누르면 이력서가 열린다
        var hire = clinic.StaffCandidates[0];
        double fee = clinic.FeeOf(hire);
        screen.PointerDown(460, 554 + 88 * 2 + 40);
        screen.PointerUp(460, 554 + 88 * 2 + 40);
        Assert.Equal(1, clinic.Hygienists);
        Assert.Equal(money - fee, clinic.Money);
    }

    [Fact]
    public void Decor_tab_picks_chair_and_interior_colors_and_saves_them()
    {
        var clinic = new Clinic { Doctor = Gender.Male };
        using var screen = new GameScreen(clinic, Opened, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        screen.Render(surface.Canvas, 540, 960);

        void Press(float x, float y) { screen.PointerDown(x, y); screen.PointerUp(x, y); }
        Press(36, 36);                        // 왼쪽 위 원 → 메뉴
        Press(460, 554 + 56 + 30);            // 꾸미기 (내 정보 다음)
        Press(18 + 72 * 2 + 36, 554 + 70);    // 체어 — 코랄
        Press(18 + 72 * 4 + 36, 554 + 190);   // 인테리어 — 버건디
        Assert.Equal(2, clinic.ChairColor);
        Assert.Equal(4, clinic.InteriorColor);

        double money = clinic.Money;
        Press(100, 520);                      // 뒤로 → 메뉴
        Press(100, 520);                      // 닫기
        Press(270, 300);                      // 게임으로 돌아와 진료
        Assert.True(clinic.Money > money);

        screen.Render(surface.Canvas, 540, 960);
        if (Environment.GetEnvironmentVariable("DC_SHOT") is { } shot)
        {
            using var data = surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(Path.ChangeExtension(shot, ".decor.png"), data.ToArray());
        }
    }

    [Fact]
    public void Equipment_is_bought_once_and_raises_capacity()
    {
        var c = new Clinic { Money = 1e8, Chairs = 1 };
        double capacity = c.Capacity;
        var autoclave = Equipment.Find("autoclave")!;
        Assert.True(c.BuyEquipment(autoclave));
        Assert.False(c.BuyEquipment(autoclave));
        Assert.Equal(1e8 - 2e7, c.Money);
        Assert.Equal(capacity * 1.03, c.Capacity, 6);

        Assert.True(c.SellEquipment(c.Placed[0], 0.5));
        Assert.Equal(capacity, c.Capacity, 6);
        Assert.Equal(1e8 - 1e7, c.Money);
    }

    [Fact]
    public void Scanner_and_ct_appear_once_on_the_floor_and_cannot_be_sold()
    {
        var c = new Clinic { Scanner = true, Ct = true };
        c.PlaceOwnedEquipment();
        c.PlaceOwnedEquipment();
        Assert.Equal(2, c.Placed.Count);
        Assert.False(c.SellEquipment(c.Placed[0], 0.5));
    }

    [Fact]
    public void Equipment_cannot_be_dropped_on_a_chair()
    {
        var c = new Clinic { Doctor = Gender.Female, Money = 1e9, Chairs = 1 };
        var career = new Career { Stage = CareerStage.Done };
        using var screen = new GameScreen(c, career, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        void Press(float x, float y) { screen.PointerDown(x, y); screen.PointerUp(x, y); screen.Render(surface.Canvas, 540, 960); }
        screen.Render(surface.Canvas, 540, 960);
        Press(36, 36);                         // 메뉴
        Press(200, 554 + 50 * 2 + 25);         // 장비 배치
        Press(460, 554 + 88 + 40);             // 첫 장비
        var placed = Assert.Single(c.Placed);
        Assert.True(placed.Y >= 0);
        float x0 = placed.X, y0 = placed.Y;

        void Drag(float toX, float toY)
        {
            screen.PointerDown(placed.X * 540, 150 + placed.Y * 350);
            screen.PointerMove(toX, toY);
            screen.PointerUp(toX, toY);
            screen.Render(surface.Canvas, 540, 960);
        }

        Drag(90, 240);    // 체어 위
        Assert.Equal((x0, y0), (placed.X, placed.Y));
        Drag(200, 460);   // 대기실 위
        Assert.Equal((x0, y0), (placed.X, placed.Y));
        Drag(x0 * 540 + 60, 150 + y0 * 350);   // 빈 바닥
        Assert.NotEqual(x0, placed.X);
    }

    [Fact]
    public void Assistants_staff_chairs_at_lower_rate_and_cannot_work_alone()
    {
        var withHygienist = new Clinic { Chairs = 4, Hygienists = 1 };
        var withAssistant = new Clinic { Chairs = 4, Assistants = 1 };
        Assert.Equal(withHygienist.StaffedChairs, withAssistant.StaffedChairs);
        Assert.True(withAssistant.Capacity < withHygienist.Capacity);

        var solo = new Clinic { Chairs = 6, Dentists = 1, Hygienists = 3, Assistants = 3 };
        Assert.Equal(0, solo.HygieneChairs);   // 의사 체어는 조무사가 채워도, 단독 진료는 남는 위생사만 한다
    }

    [Fact]
    public void Staff_cost_salary_and_field_bonus_apply_only_to_hires()
    {
        var c = new Clinic { Money = 1e9, Chairs = 3 };
        var hire = new StaffHire("김 위생사", false, 5, StaffField.Infection, 2000, 1);
        double fee = c.FeeOf(hire), salary = c.TotalSalary, capacity = c.Capacity;
        c.StaffCandidates = [hire];
        Assert.True(c.HireStaff(hire));
        Assert.Equal(1e9 - fee, c.Money);
        Assert.Empty(c.StaffCandidates);
        Assert.Equal(salary + 2000, c.TotalSalary);
        Assert.True(c.Capacity > capacity);

        c.LoseHygienist();
        Assert.Equal(0, c.Hygienists);
        Assert.Empty(c.Staff);
    }

    [Fact]
    public void Staff_cannot_exceed_chairs()
    {
        var c = new Clinic { Money = 1e9, Chairs = 1, Assistants = 1 };
        Assert.False(c.HireStaff(StaffHire.Random(new Random(1), false)));
    }

    [Fact]
    public void Patients_queue_then_leave_when_the_waiting_room_is_full()
    {
        var c = new Clinic { Chairs = 1, ReviewLeft = 1e6, WreathLeft = 1e6 };   // 유입을 크게 늘려 처리를 넘긴다
        Assert.True(c.Inflow > c.Capacity);
        for (int i = 0; i < 600; i++) c.Tick(1);
        Assert.True(c.Waiting <= c.WaitingLimit + 1e-6);
        Assert.True(c.LostPatients > 0);

        c.ReviewLeft = c.WreathLeft = 0;
        c.Chairs = 3; c.Hygienists = 3;
        for (int i = 0; i < 600; i++) c.Tick(1);
        Assert.Equal(0, c.Waiting);   // 처리가 넉넉하면 대기열이 빠진다
    }

    [Fact]
    public void Screen_renders_without_any_images_and_buying_by_tap_works()
    {
        string? shot = Environment.GetEnvironmentVariable("DC_SHOT");
        var clinic = new Clinic();
        using var screen = new GameScreen(clinic, Opened, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        screen.Render(surface.Canvas, 540, 960);

        // 고르기 전에는 시간도 진료도 멈춰 있다
        screen.Update(10);
        screen.Tap();
        Assert.Equal(5e7, clinic.Money);

        // 오른쪽 카드 = 여자 원장
        screen.PointerDown(400, 450);
        screen.PointerUp(400, 450);
        Assert.Equal(Gender.Female, clinic.Doctor);
        screen.Render(surface.Canvas, 540, 960);

        // 진료실 탭 네 번째 줄(치위생사)을 누른다
        float y = 554 + 88 * 3 + 40;
        screen.PointerDown(460, y);
        screen.PointerUp(460, y);
        y = 554 + 88 * 2 + 40;   // 첫 이력서
        screen.PointerDown(460, y);
        screen.PointerUp(460, y);
        Assert.Equal(1, clinic.Hygienists);
        screen.PointerDown(460, 554 + 40);   // 뒤로
        screen.PointerUp(460, 554 + 40);

        // 진료 화면을 누르면 돈이 는다
        double before = clinic.Money;
        screen.PointerDown(270, 300);
        screen.PointerUp(270, 300);
        Assert.True(clinic.Money > before);

        screen.Render(surface.Canvas, 540, 960);
        if (shot != null)
        {
            using var data = surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(shot, data.ToArray());
        }
    }
}
