using Dc.Core;
using Dc.Ui;
using SkiaSharp;
using Xunit;

namespace Dc.Tests;

public class CareerTests
{
    private static Career AtChoice()
    {
        var c = new Career();
        while (c.Stage == CareerStage.Student) c.Tap();
        return c;
    }

    [Fact]
    public void Xp_spent_does_not_slow_exam_progress_and_upgrades_multiply()
    {
        var c = new Career();
        for (int i = 0; i < 20; i++) c.Tap();
        Assert.True(c.Buy(TrainingItem.Loupe));
        Assert.Equal(0, c.Xp);
        Assert.Equal(20, c.TotalXp);
        Assert.Equal(2, c.Tap());
        Assert.False(c.Buy(TrainingItem.Loupe));
    }

    [Fact]
    public void Reaching_the_xp_goal_passes_the_exam_without_a_tap_test()
    {
        var c = AtChoice();
        Assert.Equal(CareerStage.Choice, c.Stage);
        Assert.Equal("합격", c.Title);
        Assert.Equal(Career.PassBonus, c.ReputationBonus);
    }

    [Fact]
    public void Old_saves_stuck_at_the_exam_move_on_to_the_choice()
    {
        var c = new Career { Stage = CareerStage.Exam };
        c.Tick(1);
        Assert.Equal(CareerStage.Choice, c.Stage);
    }

    [Fact]
    public void Residency_tasks_lead_to_pay_doctor_and_opening_carries_everything_over()
    {
        var c = AtChoice();
        c.Choose(Major.Surgery);
        for (int i = 0; i < Career.ScalingGoal + Career.RootGoal; i++) c.Tap();
        Assert.Equal(CareerStage.PayDoctor, c.Stage);

        var clinic = new Clinic();
        Assert.False(c.Open(clinic));
        c.Money = 5e6;
        Assert.True(c.Buy(TrainingItem.Seminar));
        c.Money = 2e7;
        Assert.True(c.Open(clinic));
        Assert.True(c.Done);
        Assert.Equal(5e7, clinic.Money);
        Assert.Equal(1, clinic.Skill);
        Assert.Equal(0.1, clinic.ReputationBonus);
        Assert.Equal(Major.Surgery, clinic.Major);
    }

    [Theory]
    [InlineData(Major.Pediatrics, "불소 도포", "실란트")]
    [InlineData(Major.Orthodontics, "브라켓 부착", "와이어 교체")]
    [InlineData(Major.Surgery, "발치", "사랑니 발치")]
    [InlineData(Major.Prosthodontics, "프렙", "임시치아")]
    [InlineData(Major.Conservative, "와동 형성", "근관 충전")]
    public void Residency_tasks_fit_the_major(Major major, string first, string second)
    {
        var c = new Career { Major = major };
        Assert.Equal((first, second), c.TaskNames);
    }

    [Fact]
    public void Trait_sets_stats_and_favored_subjects_and_the_major_starts_at_level_three()
    {
        var c = new Career { Stage = CareerStage.PayDoctor, Money = 2e7, Trait = Trait.Careful, Major = Major.Prosthodontics };
        var clinic = new Clinic();
        Assert.True(c.Open(clinic));
        Assert.Equal(3, clinic.Mastery.Level(Ability.Diagnosis));
        Assert.Equal(1, clinic.Mastery.Level(Ability.Care));
        Assert.Equal(3, clinic.Mastery.Level("crown"));
        Assert.True(clinic.Mastery.IsFavored("root"));
        Assert.True(clinic.Mastery.IsFavored("fullmouth"));
        Assert.False(clinic.Mastery.IsFavored("ortho"));
    }

    [Fact]
    public void Patient_ages_follow_the_clinic_and_change_what_is_treated()
    {
        var plain = new Clinic();
        Assert.Equal(1, plain.AgeMix.Sum(), 6);
        var kidsMajor = new Clinic { Major = Major.Pediatrics };
        var kidsClinic = new Clinic { Specialty = Specialty.Pediatric };
        Assert.True(kidsMajor.AgeMix[0] > plain.AgeMix[0]);
        Assert.True(kidsClinic.AgeMix[0] > 0.5);
        Assert.True(new Clinic { Site = Site.Province }.AgeMix[3] > plain.AgeMix[3]);

        Assert.True(kidsMajor.Unlocked(Catalog.Treatments.Single(t => t.Id == "fluoride")));
        Assert.Contains(kidsMajor.InflowFactors, f => f.Name == "환자층" && f.Value.Contains("소아"));

        // 노년이 많은 지방은 임플란트가, 소아가 많은 곳은 줄어든다
        var town = new Clinic { Ct = true };
        var old = new Clinic { Ct = true, Site = Site.Province };
        var kids = new Clinic { Ct = true, Major = Major.Pediatrics };
        Assert.True(Share(old, "implant") > Share(town, "implant"));
        Assert.True(Share(kids, "implant") < Share(town, "implant"));
    }

    [Fact]
    public void Children_get_cavity_care_and_crowns()
    {
        var clinic = new Clinic { Major = Major.Pediatrics };
        var cavity = Catalog.Treatments.Single(t => t.Id == "pedo_resin");
        var crown = Catalog.Treatments.Single(t => t.Id == "pedo_crown");
        Assert.True(clinic.Unlocked(cavity));
        Assert.False(clinic.Unlocked(crown));
        Assert.True(Share(clinic, "pedo_resin") > 0.05);
        clinic.Mastery.AddPatients("pedo_resin", 1000);
        Assert.True(clinic.Unlocked(crown));
        Assert.False(new Clinic().Unlocked(cavity));
    }

    private static double Share(Clinic c, string id) =>
        Enumerable.Range(0, 2000).Count(seed => c.TreatmentAt(seed).Id == id) / 2000.0;

    [Fact]
    public void Favored_subjects_grow_faster()
    {
        var plain = new Mastery();
        var gifted = new Mastery { Favored = ["resin"] };
        plain.AddPatients("resin", 1000);
        gifted.AddPatients("resin", 1000);
        Assert.True(gifted.Level("resin") > plain.Level("resin"));
    }

    [Fact]
    public void New_game_asks_for_a_trait_after_gender_before_the_story()
    {
        var clinic = new Clinic();
        var career = new Career();
        var story = new StoryDirector();
        using var screen = new GameScreen(clinic, career, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic")) { Story = story };
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        void Press(float x, float y) { screen.PointerDown(x, y); screen.PointerUp(x, y); screen.Update(0.1); screen.Render(surface.Canvas, 540, 960); }
        screen.Render(surface.Canvas, 540, 960);
        Press(150, 450);
        Assert.Equal(Gender.Male, clinic.Doctor);
        Assert.Null(story.CurrentId);
        Press(270, 230 + 88 + 40);             // 두 번째: 꼼꼼한 사람
        Assert.Equal(Trait.Careful, career.Trait);
        Assert.Equal("childhood", story.CurrentId);
    }

    private static Career PayDoctor() => new() { Stage = CareerStage.PayDoctor };

    private static void FinishCase(Career c)
    {
        var kind = c.Case;
        while (c.Case == kind) c.Tap();
    }

    [Fact]
    public void Pay_doctor_cases_rotate_through_three_subjects_with_a_finish_bonus()
    {
        var c = PayDoctor();
        Assert.Equal(PayCase.Gum, c.Case);
        double first = c.Tap();
        Assert.Equal(15_000, first);
        FinishCase(c);
        Assert.Equal(PayCase.Caries, c.Case);
        Assert.Equal(Career.SpotCount, c.CaseLeft);
        FinishCase(c);
        Assert.Equal(PayCase.Root, c.Case);
        FinishCase(c);
        Assert.Equal(PayCase.Gum, c.Case);
        Assert.Equal([1, 1, 1], c.CasesDone);
    }

    [Fact]
    public void Only_balanced_practice_raises_skill()
    {
        var lopsided = new Career { CasesDone = [30, 30, 0] };
        Assert.Equal(1, lopsided.SkillRate);
        Assert.Equal(1.05, new Career { CasesDone = [3, 3, 3] }.SkillRate, 6);
        Assert.Equal(Career.MaxSkillRate, new Career { CasesDone = [999, 999, 999] }.SkillRate);
    }

    [Fact]
    public void Caries_spots_must_be_hit_where_they_are()
    {
        var c = new Career { Stage = CareerStage.PayDoctor, Case = PayCase.Caries, CaseLeft = 6, Spots = 63 };
        Assert.True(c.TapSpot(2) > 0);
        Assert.Equal(0, c.TapSpot(2));
        Assert.Equal(5, c.CaseLeft);
    }

    [Fact]
    public void Anesthesia_call_pays_big_if_answered_and_costs_nothing_if_missed()
    {
        var c = PayDoctor();
        c.Tick(Career.CallInterval);
        Assert.True(c.CallLeft > 0);
        int left = c.CaseLeft;
        Assert.Equal(15_000 * Career.CallReward, c.Tap());
        Assert.Equal(0, c.CallLeft);
        Assert.Equal(left, c.CaseLeft);

        c.Tick(Career.CallInterval);
        double money = c.Money;
        c.Tick(Career.CallSeconds);
        Assert.Equal(0, c.CallLeft);
        Assert.Equal(money + c.Salary * Career.CallSeconds, c.Money);
    }

    [Fact]
    public void Majors_change_the_clinic_rules()
    {
        Assert.Equal(new Clinic().Inflow * 1.3, new Clinic { Major = Major.Pediatrics }.Inflow, 6);
        var ortho = new Clinic { Major = Major.Orthodontics };
        Assert.True(ortho.Unlocked(Catalog.Treatments.Single(t => t.Id == "ortho")));
        Assert.NotNull(ortho.Blocked(Purchase.Orthodontist));
        var surgery = new Clinic { Ct = true, Major = Major.Surgery };
        Assert.True(surgery.AverageSeconds < new Clinic { Ct = true }.AverageSeconds);
    }

    [Fact]
    public void Screen_walks_from_student_to_clinic_with_taps()
    {
        var clinic = new Clinic { Doctor = Gender.Male };
        var career = new Career { Trait = Trait.Quick };
        using var screen = new GameScreen(clinic, career, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        void Press(float x, float y) { screen.PointerDown(x, y); screen.PointerUp(x, y); screen.Render(surface.Canvas, 540, 960); }
        screen.Render(surface.Canvas, 540, 960);

        while (career.Stage == CareerStage.Student) Press(270, 300);
        Assert.Equal(CareerStage.Choice, career.Stage);
        Press(460, 150 + 40);                       // 바로 페이닥터
        Assert.Equal(CareerStage.PayDoctor, career.Stage);
        double before = career.Money;
        Press(270, 300);
        Assert.Equal(before + 15_000, career.Money);
        career.Money = 2e7;
        Press(460, 500 + 40);                       // 개원하기
        Assert.True(career.Done);
        Assert.Equal(5e7, clinic.Money);
    }
}
