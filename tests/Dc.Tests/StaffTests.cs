using Dc.Core;
using Dc.Ui;
using SkiaSharp;
using Xunit;

namespace Dc.Tests;

public class StaffTests
{
    private static Clinic Busy() => new() { Doctor = Gender.Male, Chairs = 4, Hygienists = 4, SpaceIndex = 1, Desks = 8, Money = 1e10 };

    [Fact]
    public void Candidates_differ_in_years_subject_and_salary()
    {
        var random = new Random(3);
        var c = new Clinic();
        c.RefreshCandidates(random);
        Assert.Equal(3, c.Candidates.Count);
        Assert.All(c.Candidates, h => Assert.InRange(h.Years, 1, 15));
        var senior = new DentistHire("가", 10, DentistSubject.Surgery, 0, 0);
        Assert.True(DentistHire.Rookie.Salary < new DentistHire("나", 10, DentistSubject.General, DentistHire.BaseSalary * 3, 1.3).Salary);
        Assert.Equal("구강외과", senior.SubjectName);
    }

    [Fact]
    public void Staff_candidates_have_full_names()
    {
        var random = new Random(5);
        for (int i = 0; i < 200; i++)
        {
            var hire = StaffHire.Random(random, i % 2 == 0);
            string person = hire.Name.Split(' ')[0];
            Assert.InRange(person.Length, 3, 4);   // 성 + 이름 두 글자
            Assert.EndsWith(hire.Assistant ? " 조무사" : " 위생사", hire.Name);
        }
    }

    [Fact]
    public void Hiring_pays_the_fee_adds_a_dentist_and_salary_comes_out_of_revenue()
    {
        var c = Busy();
        var hire = new DentistHire("김 선생", 5, DentistSubject.General, 20_000, 1);
        c.Candidates = [hire];
        double money = c.Money, cost = c.FeeOf(hire);
        Assert.True(c.Hire(hire));
        Assert.Equal(money - cost, c.Money);
        Assert.Equal(2, c.Dentists);
        Assert.Empty(c.Candidates);
        Assert.Equal(c.GrossPerSecond - 20_000, c.RevenuePerSecond, 3);
    }

    [Fact]
    public void Salaries_can_never_make_income_negative()
    {
        var c = Busy();
        c.Hired = [new DentistHire("비싼 선생", 15, DentistSubject.General, 1e9, 1)];
        Assert.Equal(c.GrossPerSecond * 0.5, c.RevenuePerSecond, 3);
        Assert.False(c.CanAffordSalary(new DentistHire("또 비싼", 15, DentistSubject.General, 1e9, 1)));
    }

    [Fact]
    public void Experience_speeds_treatment_and_subject_raises_its_price()
    {
        var plain = Busy();
        var veteran = Busy();
        veteran.Hired = [new DentistHire("가", 10, DentistSubject.General, 0, 1)];
        Assert.True(veteran.Capacity > plain.Capacity);

        var surgeon = new Clinic { Ct = true, Hired = [new DentistHire("가", 1, DentistSubject.Surgery, 0, 1)] };
        Assert.True(surgeon.AveragePrice > new Clinic { Ct = true }.AveragePrice);
    }

    [Fact]
    public void Preview_does_not_change_the_clinic()
    {
        var c = Busy();
        var hire = new DentistHire("가", 5, DentistSubject.General, 10_000, 1);
        c.GainOf(hire);
        c.Gain(Purchase.Dentist);
        Assert.Empty(c.Hired);
        Assert.Equal(1, c.Dentists);
    }

    [Fact]
    public void Auto_and_bundle_hire_a_rookie_with_a_salary()
    {
        var c = new Clinic { Chairs = 3, SpaceIndex = 1, Money = 1e10 };
        Assert.True(c.Buy(Purchase.Bundle));
        Assert.Equal("신입", Assert.Single(c.Hired).Name);
    }

    [Fact]
    public void Hires_are_saved()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "save.json");
        var c = new Clinic { Hired = [new DentistHire("김 선생", 7, DentistSubject.Orthodontics, 123, 1.1)], Candidates = [DentistHire.Rookie] };
        new SaveStore(path).Save(c, new Career(), new EventDirector(), DateTime.UtcNow);
        var loaded = new SaveStore(path).Load()!.Clinic;
        Assert.Equal(c.Hired, loaded.Hired);
        Assert.Equal(c.Candidates, loaded.Candidates);
        Directory.Delete(Path.GetDirectoryName(path)!, true);
    }

    [Fact]
    public void Dentist_row_opens_candidates_and_tapping_one_hires()
    {
        var c = Busy();
        using var screen = new GameScreen(c, new Career { Stage = CareerStage.Done }, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic"));
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));
        void Press(float x, float y) { screen.PointerDown(x, y); screen.PointerUp(x, y); screen.Render(surface.Canvas, 540, 960); }
        screen.Render(surface.Canvas, 540, 960);
        const float top = 554, row = 88;
        Press(460, top + row * 2 + 40);        // 치과의사 줄 → 지원자 화면
        Assert.Equal(1, c.Dentists);
        Assert.Equal(3, c.Candidates.Count);
        Press(460, top + row * 2 + 40);        // 첫 지원자 (뒤로, 정보, 지원자 순)
        Assert.Equal(2, c.Dentists);
        Assert.Single(c.Hired);
    }
}
