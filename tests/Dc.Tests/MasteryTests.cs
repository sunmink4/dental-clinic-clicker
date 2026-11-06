using Dc.Core;
using Dc.Ui;
using SkiaSharp;
using Xunit;

namespace Dc.Tests;

public class MasteryTests
{
    private static Treatment Of(string id) => Catalog.Treatments.Single(t => t.Id == id);

    [Fact]
    public void Seminars_raise_level_and_cost_doubles()
    {
        var c = new Clinic { Money = 1e12 };
        var scaling = Of("scaling");
        double first = c.Mastery.SeminarCost(scaling);
        double priceBefore = c.AveragePrice, timeBefore = c.AverageSeconds;
        Assert.True(c.Study(scaling));
        Assert.Equal(1, c.Mastery.Level("scaling"));
        Assert.Equal(first * 2, c.Mastery.SeminarCost(scaling));
        Assert.True(c.AveragePrice > priceBefore);
        Assert.True(c.AverageSeconds < timeBefore);
    }

    [Fact]
    public void Treating_patients_builds_each_subject_by_its_share()
    {
        var c = new Clinic { Chairs = 3, Hygienists = 3, Desks = 3 };
        for (int i = 0; i < 3600; i++) c.Tick(1);
        Assert.True(c.Mastery.Level("scaling") > c.Mastery.Level("root"));
        Assert.True(c.Mastery.Level("scaling") >= 1);
        Assert.Equal(0, c.Mastery.Level("implant"));   // 해금 안 된 진료는 늘지 않는다
    }

    [Fact]
    public void Prerequisite_gates_the_next_subject()
    {
        var c = new Clinic { Money = 1e12 };
        Assert.Contains("스케일링", c.Mastery.StudyBlocked(Of("resin")));
        Assert.False(c.Study(Of("resin")));
        for (int i = 0; i < 3; i++) c.Study(Of("scaling"));
        Assert.True(c.Study(Of("resin")));
    }

    [Fact]
    public void New_treatments_unlock_through_the_tree()
    {
        var c = new Clinic { Money = 1e12, Scanner = true };
        var extraction = Of("extraction");
        var inlay = Of("inlay");
        Assert.False(c.Unlocked(extraction));
        Assert.False(c.Unlocked(inlay));
        for (int i = 0; i < 3; i++) c.Study(Of("scaling"));
        Assert.True(c.Unlocked(extraction));
        for (int i = 0; i < 3; i++) c.Study(extraction);
        Assert.True(c.Unlocked(Of("wisdom")));
        for (int i = 0; i < 3; i++) c.Study(Of("resin"));
        Assert.True(c.Unlocked(inlay));
        Assert.False(new Clinic().Unlocked(inlay));
    }

    [Fact]
    public void Field_reputation_grows_with_its_patients_and_draws_more_of_them()
    {
        static double Share(Clinic c, string id) =>
            Enumerable.Range(0, 3000).Count(seed => c.TreatmentAt(seed).Id == id) / 3000.0;

        var plain = new Clinic();
        var famous = new Clinic();
        for (int i = 0; i < 100; i++) famous.Mastery.AddFieldPatients("root", 1000);
        Assert.True(famous.Mastery.FieldReputation(Field.Conservative) > plain.Mastery.FieldReputation(Field.Conservative));
        Assert.True(Share(famous, "root") > Share(plain, "root"));
        Assert.Equal(1, plain.Mastery.FieldReputation(Field.Prosthodontics));
    }

    [Fact]
    public void Major_field_starts_with_a_better_reputation()
    {
        var mastery = new Mastery { Major = Major.Prosthodontics };
        Assert.Equal(1.3, mastery.FieldReputation(Field.Prosthodontics), 6);
        Assert.Equal(1, mastery.FieldReputation(Field.Conservative));
        Assert.Equal(1.3, new Mastery { Major = Major.Pediatrics }.FieldReputation(Field.Pediatrics), 6);
    }

    [Fact]
    public void Treating_patients_builds_field_reputation_and_it_shows_in_the_list()
    {
        var c = new Clinic { Chairs = 3, Hygienists = 3 };
        for (int i = 0; i < 600; i++) c.Tick(1);
        Assert.True(c.Mastery.FieldReputation(Field.Periodontics) > 1);
        Assert.Contains(c.InflowFactors, f => f.Name == "진료과 평판" && f.How.Contains("보철"));
    }

    [Fact]
    public void Major_subjects_go_higher_and_pay_more()
    {
        var mastery = new Mastery { Major = Major.Pediatrics };
        mastery.AddPatients("fluoride", 1e9);
        mastery.AddPatients("root", 1e9);
        Assert.Equal(Mastery.MajorMaxLevel, mastery.Level("fluoride"));
        Assert.Equal(Mastery.MaxLevel, mastery.Level("root"));
        Assert.Equal(3.0, mastery.PriceRate("fluoride"), 6);
        Assert.Equal(1.6, mastery.PriceRate("root"), 6);
    }

    [Fact]
    public void Saves_from_before_majors_get_the_bonus_on_load()
    {
        var head = new Clinic { Major = Major.Surgery };
        new Company(head).Link(0);
        Assert.True(head.Mastery.IsMajor("implant"));
    }

    [Fact]
    public void Seminars_stop_at_five_and_level_stops_at_twenty()
    {
        var c = new Clinic { Money = 1e15 };
        var scaling = Of("scaling");
        for (int i = 0; i < 9; i++) c.Study(scaling);
        Assert.Equal(Mastery.MaxSeminars, c.Mastery.SeminarsOf("scaling"));
        c.Mastery.AddPatients("scaling", 1e9);
        Assert.Equal(Mastery.MaxLevel, c.Mastery.Level("scaling"));
    }

    [Fact]
    public void Branches_share_mastery_and_it_is_saved()
    {
        var company = new Company(new Clinic { SpaceIndex = 2, Money = 1e12 });
        company.Link(0);
        company.Open(Site.NewTown, Specialty.None);
        company.Head.Study(Of("scaling"));
        Assert.Equal(1, company.Current.Mastery.Level("scaling"));

        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "save.json");
        new SaveStore(path).Save(company, new Career(), new EventDirector(), DateTime.UtcNow);
        var loaded = new SaveStore(path).Load()!.Company!;
        loaded.Link(0);
        Assert.Equal(1, loaded.Current.Mastery.Level("scaling"));
        Directory.Delete(Path.GetDirectoryName(path)!, true);
    }

    [Fact]
    public void Skill_tree_page_opens_and_studies_by_tap()
    {
        var c = new Clinic { Doctor = Gender.Male, Money = 1e10 };
        using var screen = new GameScreen(c, new Career { Stage = CareerStage.Done }, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        void Press(float x, float y) { screen.PointerDown(x, y); screen.PointerUp(x, y); screen.Render(surface.Canvas, 540, 960); }
        screen.Render(surface.Canvas, 540, 960);
        const float top = 554;
        Press(50, 520);                    // 스킬 탭
        Press(460, top + 44 + 40);         // 스케일링 세미나
        Assert.Equal(1, c.Mastery.Level("scaling"));
    }
}
