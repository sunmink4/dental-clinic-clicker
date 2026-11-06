using Dc.Core;
using Dc.Ui;
using SkiaSharp;
using Xunit;

namespace Dc.Tests;

public class ArtTests
{
    /// <summary>DC_RECOLOR 에 그림 경로를 주면 코랄로 바꾼 미리보기를 옆에 저장한다.</summary>
    [Fact]
    public void Recolor_preview_of_real_art()
    {
        if (Environment.GetEnvironmentVariable("DC_RECOLOR") is not { } path) return;
        using var bitmap = SKBitmap.Decode(path);
        bitmap.Pixels = Palette.Recolor(bitmap.Pixels, Palette.Colors[2].Color);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(Path.ChangeExtension(path, ".coral.png"), data.ToArray());
    }

    /// <summary>DC_ART 에 assets/images 경로, DC_ART_OUT 에 저장 폴더를 주면 실제 그림으로 그린 화면을 저장한다.</summary>
    [Fact]
    public void Screens_with_real_art()
    {
        if (Environment.GetEnvironmentVariable("DC_ART") is not { } images) return;
        string output = Environment.GetEnvironmentVariable("DC_ART_OUT")!;
        Stream? Open(string name) => File.Exists(Path.Combine(images, name)) ? File.OpenRead(Path.Combine(images, name)) : null;
        var font = SKTypeface.FromFamilyName("Malgun Gothic");

        void Shot(string name, Clinic clinic, Career career, EventDirector? events = null, StoryDirector? story = null, bool menu = false, bool profile = false, bool hireStaff = false, (float X, float Y)? press = null)
        {
            using var screen = new GameScreen(clinic, career, events ?? new EventDirector(), Open, font) { Story = story };
            screen.Update(0);
            using var surface = SKSurface.Create(new SKImageInfo(540, 960));
            screen.Render(surface.Canvas, 540, 960);
            if (menu) { screen.PointerDown(36, 36); screen.PointerUp(36, 36); }
            if (hireStaff) { screen.PointerDown(460, 554 + 88 * 3 + 40); screen.PointerUp(460, 554 + 88 * 3 + 40); }
            if (profile) { screen.PointerDown(200, 585); screen.PointerUp(200, 585); }
            if (press is { } p) { screen.PointerDown(p.X, p.Y); screen.PointerUp(p.X, p.Y); }
            screen.Render(surface.Canvas, 540, 960);
            using var data = surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(Path.Combine(output, name + ".png"), data.ToArray());
        }

        Shot("event", new Clinic { Doctor = Gender.Male, Chairs = 3, Hygienists = 2, ChairColor = 1, ReviewLeft = 120 }, new Career { Stage = CareerStage.Done },
             new EventDirector { Active = EventKind.Complaint, ActiveLeft = 22, ComplaintCase = 1 });
        Shot("story", new Clinic { Doctor = Gender.Female }, new Career { Trait = Trait.Warm }, null, new StoryDirector { Seen = ["childhood", "study"], LineIndex = 0 });
        Shot("menu", new Clinic { Doctor = Gender.Male }, new Career { Stage = CareerStage.Done }, null, null, true);
        Shot("pick", new Clinic(), new Career());
        Shot("header", new Clinic { Doctor = Gender.Male, Chairs = 3, Fatigue = 72, Claims = 12, Hygienists = 1 }, new Career { Stage = CareerStage.Done });
        Shot("student", new Clinic { Doctor = Gender.Female }, new Career { Xp = 45, TotalXp = 120, Trait = Trait.Warm });
        Shot("trait", new Clinic { Doctor = Gender.Female }, new Career());
        var floor = new Clinic { Doctor = Gender.Female, Face = 2, Chairs = 3, Hygienists = 2, Desks = 2, Money = 1e9, Scanner = true, Ct = true, Waiting = 4, LostPatients = 7 };
        foreach (var gear in Equipment.Shop) floor.BuyEquipment(gear);
        Shot("floor", floor, new Career { Stage = CareerStage.Done, Title = "원장" });
        var staff = new Clinic { Doctor = Gender.Female, Face = 2, Chairs = 3, Hygienists = 0, Money = 1e9 };
        staff.RefreshStaffCandidates(new Random(3));
        Shot("staff", staff, new Career { Stage = CareerStage.Done, Title = "원장" }, hireStaff: true);
        Shot("profile", new Clinic { Doctor = Gender.Female, Face = 3, Chairs = 3 }, new Career { Stage = CareerStage.Done, Title = "원장" }, menu: true, profile: true);
        Shot("branches", new Clinic { Doctor = Gender.Female, Face = 3, Chairs = 3, Money = 1e9 }, new Career { Stage = CareerStage.Done }, press: (90, 36));
        Shot("hero_tap", new Clinic { Doctor = Gender.Female, Face = 3, Chairs = 3 }, new Career { Stage = CareerStage.Done }, press: (500, 450));
        var doctors = new Clinic { Doctor = Gender.Female, Chairs = 6, SpaceIndex = 2, Money = 1e12 };
        doctors.RefreshCandidates(new Random(4));
        Shot("dentists", doctors, new Career { Stage = CareerStage.Done }, press: (460, 554 + 88 * 2 + 40));
        Shot("choice", new Clinic { Doctor = Gender.Female }, new Career { Stage = CareerStage.Choice, Title = "합격", Trait = Trait.Warm });
        Shot("clinic", new Clinic { Doctor = Gender.Male, SpaceIndex = 2, Chairs = 6, Dentists = 2, Hygienists = 4, Desks = 2, Money = 3e8, ChairColor = 2, InteriorColor = 1 },
             new Career { Stage = CareerStage.Done });
    }
}
