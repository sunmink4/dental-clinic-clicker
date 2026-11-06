using Dc.Core;
using Dc.Ui;
using SkiaSharp;
using Xunit;

namespace Dc.Tests;

public class StoryTests
{
    private static void Finish(StoryDirector story)
    {
        while (story.CurrentId != null) story.Advance();
    }

    [Fact]
    public void Prologue_plays_in_order_ending_with_the_new_student_and_never_repeats()
    {
        var story = new StoryDirector();
        var company = new Company(new Clinic());
        var career = new Career();
        var seen = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            story.Update(company, career);
            if (story.CurrentId is { } id) seen.Add(id);
            Finish(story);
        }
        Assert.Equal(["childhood", "study", "student"], seen);
    }

    [Fact]
    public void Each_career_step_has_its_scene()
    {
        var story = new StoryDirector { Seen = ["childhood", "study", "student"] };
        var company = new Company(new Clinic());
        var career = new Career { Stage = CareerStage.Choice };
        foreach (var (stage, id) in new[] { (CareerStage.Choice, "result"), (CareerStage.Resident, "resident"), (CareerStage.PayDoctor, "paydoctor"), (CareerStage.Done, "opening") })
        {
            career.Stage = stage;
            story.Update(company, career);
            Assert.Equal(id, story.CurrentId);
            Finish(story);
        }
    }

    [Fact]
    public void Screen_pauses_the_game_during_a_scene_and_tap_and_skip_move_it_on()
    {
        var clinic = new Clinic { Doctor = Gender.Male };
        var career = new Career { Stage = CareerStage.PayDoctor };
        var story = new StoryDirector { Seen = ["childhood", "study", "student", "result", "resident"] };
        using var screen = new GameScreen(clinic, career, new EventDirector(), _ => null, SKTypeface.FromFamilyName("Malgun Gothic")) { Story = story };
        using var surface = SKSurface.Create(new SKImageInfo(540, 960));

        screen.Update(1);
        screen.Render(surface.Canvas, 540, 960);
        Assert.Equal("paydoctor", story.CurrentId);
        double money = career.Money;
        screen.Update(5);
        Assert.Equal(money, career.Money);

        screen.PointerDown(270, 700);
        screen.PointerUp(270, 700);
        Assert.Equal(1, story.LineIndex);
        Assert.Equal(money, career.Money);

        screen.PointerDown(500, 30);           // 건너뛰기
        screen.PointerUp(500, 30);
        Assert.Null(story.CurrentId);
        screen.Update(5);
        Assert.True(career.Money > money);
    }

    [Theory]
    [InlineData("어머니", "face_mother")]
    [InlineData("치과 선생님", "face_old_dentist")]
    [InlineData("선배", "face_senior")]
    [InlineData("교수님", "face_professor")]
    [InlineData("실장", "face_manager")]
    [InlineData("나", null)]
    public void Speakers_have_portraits(string speaker, string? image) => Assert.Equal(image, StoryDirector.Portrait(speaker));

    [Fact]
    public void Cutscene_picture_changes_on_marked_lines_and_stays_until_the_next()
    {
        var story = new StoryDirector();
        story.Update(new Company(new Clinic()), new Career());
        Assert.Equal("story_childhood", story.Picture);
        story.Advance();
        Assert.Equal("story_childhood", story.Picture);
        story.Advance();
        Assert.Equal("story_kind_dentist", story.Picture);
        story.Advance();
        Assert.Equal("story_kind_dentist", story.Picture);
    }

    [Fact]
    public void Story_is_saved()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "save.json");
        var store = new SaveStore(path);
        store.Save(new Company(new Clinic()), new Career(), new EventDirector(), new StoryDirector { Seen = ["childhood"] }, DateTime.UtcNow);
        Assert.Equal(["childhood"], store.Load()!.Story!.Seen);
        Directory.Delete(Path.GetDirectoryName(path)!, true);
    }
}
