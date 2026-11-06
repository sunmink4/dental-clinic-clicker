using Dc.Core;
using SkiaSharp;

namespace Dc.Ui;

/// <summary>
/// 게임 화면 전체 — PC · 안드로이드가 함께 쓴다. 호스트는 틀마다 Update · Render 를 부르고 포인터를 넘기기만 한다.
/// 가로 540 기준 논리 좌표로 그린다. 세로가 더 긴 화면에서는 아래 구매 목록이 길어진다.
/// </summary>
public sealed partial class GameScreen(Company company, Career career, EventDirector events, Func<string, Stream?> openImage, SKTypeface typeface) : IDisposable
{
    public GameScreen(Clinic clinic, Career career, EventDirector events, Func<string, Stream?> openImage, SKTypeface typeface)
        : this(new Company(clinic), career, events, openImage, typeface) { }

    /// <summary>지금 보고 있는 지점.</summary>
    private Clinic clinic => company.Current;

    private const float W = 540, BaseH = 960, ClinicTop = 150, ClinicBottom = 500, TabH = 54, RowH = 88;
    private const float DentistStaySeconds = 3, FatigueLeft = 440, FatigueTop = 104, InflowRight = 150, FactorRight = 288;
    private const float DragStart = 10, PickTop = 300, PickBottom = 620;

    private sealed record Row(Purchase Item, string Title, string Note, string Image);
    private sealed class Floater { public float X, Y, Age, Size = 24; public string Text = ""; }

    private static readonly (string Name, Row[] Rows)[] Tabs =
    [
        ("스킬", []),
        ("진료실", [
            new(Purchase.Bundle, "체어 + 페이닥터", "신입 페이닥터와 함께 사면 바로 가동", "chair_idle"),
            new(Purchase.Chair, "유닛체어", "의사 1명이 3대, 남는 체어는 위생사 단독 진료", "chair_idle"),
            new(Purchase.Dentist, "페이닥터 채용", "이력서를 보고 고릅니다. 체어 3대를 더 가동", "face_dentist"),
            new(Purchase.Hygienist, "치위생사 · 간호조무사", "이력서를 보고 고릅니다. 체어 효율 상승", "face_hygienist"),
            new(Purchase.Desk, "데스크", "환자 유입 +40%", "face_desk"),
            new(Purchase.Consultant, "상담 위생사", "상담을 맡깁니다. 비급여 단가 +6%, 클레임 해소 상승", "face_hygienist"),
            new(Purchase.Sterilizer, "소독 이모님", "기구 소독을 맡아 위생사 3명의 피로를 덥니다", "face_desk"),
        ]),
        ("일정", []),
        ("아이템", []),
        ("장비", [
            new(Purchase.Scanner, "구강스캐너", "인레이 · 온레이 · 크라운 시간 -25%", "eq_scanner"),
            new(Purchase.Ct, "CT", "임플란트 해금", "eq_ct"),
            new(Purchase.Manager, "실장", "전체 매출 +20%", "face_manager"),
            new(Purchase.Orthodontist, "교정 전문의", "교정 해금", "face_ortho"),
            new(Purchase.AutoManage, "자동 경영", "실장이 이 지점을 알아서 운영", "face_manager"),
        ]),
        ("이전", [new(Purchase.Move, "공간 이전", "", "eq_move")]),
        ("지점", []),
    ];

    private const float SwatchSize = 72, ChairSwatchY = 44, InteriorSwatchY = 164;

    private readonly Dictionary<string, SKImage?> _images = [];
    private readonly List<Floater> _floaters = [];
    private readonly SKPaint _paint = new() { IsAntialias = true };
    private readonly SKPaint _imagePaint = new() { IsAntialias = true };   // 도형 색 · 투명도의 영향을 받지 않게 따로 둔다
    private readonly SKFont _font = new(typeface);

    private float _scale = 1, _offsetX, _height = BaseH;
    private const int SkillTab = 0, ClinicTab = 1;
    private int _tab = ClinicTab;
    private float _scroll, _pointerX, _pointerY, _startY, _startScroll;
    private bool _down, _dragging, _tiredWarned;
    private const int LogicEveryFrames = 6;
    private const double SlowFrame = 0.1;
    private int _frame;
    private double _unticked;
    private float _bounce, _time;
    private string _toast = "";
    private float _toastLeft;

    public void Toast(string text) { _toast = text; _toastLeft = 3; }

    public void Update(double seconds)
    {
        if (PickingTrait) return;
        if (clinic.Doctor != null) Story?.Update(company, career);
        if (clinic.Doctor != null && !Telling)
        {
            TickAutoClick(seconds);
            if (career.Done)
            {
                // 게임 계산은 6프레임에 한 번, 쌓인 시간을 몰아서 한다. 프레임이 느린 기기는 바로 계산한다
                _unticked += seconds;
                if (++_frame % LogicEveryFrames == 0 || _unticked >= SlowFrame)
                {
                    company.Tick(_unticked);
                    TickEvents(_unticked);
                    if (clinic.TrySatisfy(Random.Shared, clinic.ServedPerSecond * _unticked)) ShowSatisfied();
                    _unticked = 0;
                }
            }
            else career.Tick(seconds);
        }
        float dt = (float)seconds;
        _time += dt;
        _bounce = Math.Max(0, _bounce - dt * 6);
        _toastLeft = Math.Max(0, _toastLeft - dt);
        foreach (var f in _floaters) f.Age += dt;
        _floaters.RemoveAll(f => f.Age > 0.8f);
    }

    private void ShowSatisfied() => _floaters.Add(new Floater { X = ToothX + Random.Shared.Next(-40, 40), Y = 400, Size = 15, Text = $"만족! 피로 -{Clinic.SatisfyRecovery:0}" });

    public void Tap()
    {
        if (Telling) { Story!.Advance(); return; }
        if (clinic.Doctor == null || PressEvent(0)) return;
        bool student = career.Stage == CareerStage.Student;
        if (career.Done && clinic.Resting) { Toast("쉬는 중에는 직접 진료를 할 수 없습니다"); return; }
        double value = career.Done ? clinic.Click() : career.Tap();
        if (career.Done && value > 0 && clinic.TrySatisfy(Random.Shared, 1)) ShowSatisfied();
        if (career.Done && clinic.Fatigue >= 100 && !_tiredWarned)
        {
            _tiredWarned = true;
            Toast("피로 100%: 직접 진료 수입 −70%. 피로를 눌러 쉬세요");
        }
        if (clinic.Fatigue < 50) _tiredWarned = false;
        _bounce = 1;
        if (value > 0)
            _floaters.Add(new Floater { X = (career.Done ? ToothX : W / 2) + Random.Shared.Next(-70, 70), Y = 410, Text = "+" + KoreanNumber.Format(value) + (student ? " XP" : "") });
    }

    // ── 입력 ──

    private (float X, float Y) ToLogical(float x, float y) => ((x - _offsetX) / _scale, y / _scale);

    public void PointerDown(float x, float y)
    {
        (_pointerX, _pointerY) = ToLogical(x, y);
        _startY = _pointerY;
        _startScroll = _scroll;
        _down = true;
        _dragging = false;
        GrabEquipment(_pointerX, _pointerY);
    }

    public void PointerMove(float x, float y)
    {
        if (!_down) return;
        (_pointerX, _pointerY) = ToLogical(x, y);
        if (_moving != null) { MoveEquipment(_pointerX, _pointerY); return; }
        if (Math.Abs(_pointerY - _startY) > DragStart) _dragging = _startY > ListTop || (InWiki && Reading);
        if (_dragging) SetScroll(_startScroll + _startY - _pointerY);
    }

    public void PointerUp(float x, float y)
    {
        if (!_down) return;
        _down = false;
        if (_moving != null) { DropEquipment(); return; }
        if (_dragging) return;
        (_pointerX, _pointerY) = ToLogical(x, y);
        Press(_pointerX, _pointerY);
    }

    public void Wheel(float notches) => SetScroll(_scroll - notches * RowH);

    private float ListTop => ClinicBottom + TabH;
    private int ListCount => _arranging ? EquipmentChoices().Count : _tab == BranchTab ? BranchChoices().Count : _tab == ClinicTab && _hiring ? HiringChoices().Count : _tab == SkillTab ? SkillChoices().Count + 1 : _tab == ScheduleTab ? ScheduleCount : _tab == ItemTab ? ItemChoices().Count : Tabs[_tab].Rows.Length;
    private float MaxScroll => InWiki
        ? (Reading ? Math.Max(0, WikiHeight + WikiHeader + 60 - _height) : 0)
        : Math.Max(0, ListCount * RowH - (_height - ListTop));
    private void SetScroll(float value) => _scroll = Math.Clamp(value, 0, MaxScroll);

    private void Press(float x, float y)
    {
        if (x is < 0 or > W) return;
        if (PickingFace) { PressFace(x, y); return; }
        if (PickingTrait) { PressTrait(y); return; }
        if (Telling) { PressStory(x, y); return; }
        if (InWiki) { PressWiki(y); return; }
        if (_arranging) { PressEquipment(y); return; }
        if (clinic.Doctor != null && y < ClinicTop && PressAutoClick(x, y)) return;
        if (clinic.Doctor != null && career.Done && x < MenuButton && y < MenuButton)
        {
            _menu = _menu == MenuPage.Closed ? MenuPage.Root : MenuPage.Closed;
            return;
        }
        if (clinic.Doctor != null && career.Done && BranchChip.Contains(x, y))
        {
            _menu = _menu == MenuPage.Branches ? MenuPage.Closed : MenuPage.Branches;
            return;
        }
        if (clinic.Doctor != null && career.Done && x >= FatigueLeft && y is >= FatigueTop and < ClinicTop) { RestNow(); return; }
        if (clinic.Doctor != null && career.Done && x < FactorRight && y is >= FatigueTop and < ClinicTop)
        {
            _menu = x < InflowRight ? MenuPage.Inflow : MenuPage.Capacity;
            return;
        }
        if (_menu != MenuPage.Closed && y >= ClinicBottom) { PressMenu(x, y); return; }
        if (clinic.Doctor == null)
        {
            if (y is >= PickTop and < PickBottom) clinic.Doctor = x < W / 2 ? Gender.Male : Gender.Female;
            return;
        }
        if (!career.Done) { PressTraining(x, y); return; }
        if (y is >= ClinicTop and < ClinicBottom) { if (!PressEvent(y) && !PressBadge(x, y)) { if (HeroBox.Contains(x, y)) _menu = MenuPage.Profile; else Tap(); } return; }
        if (y >= ClinicBottom && y < ListTop)
        {
            int tab = Math.Clamp((int)(x / (W / Tabs.Length)), 0, Tabs.Length - 1);
            if (tab == BranchTab && !company.Unlocked) { Toast("본점이 50평이 되면 열립니다"); return; }
            _tab = tab;
            _hiring = false;
            _itemSlot = null;
            _branchPage = BranchPage.List;
            _scroll = 0;
            return;
        }
        if (y < ListTop) return;
        if (_tab == ClinicTab && _hiring) { PressHiring(y); return; }
        if (_tab == SkillTab) { PressSkills(x, y); return; }
        if (_tab == ScheduleTab) { PressSchedule(x, y); return; }
        if (_tab == ItemTab) { PressItems(y); return; }
        if (_tab == BranchTab) { PressBranches(y); return; }
        int index = (int)((y - ListTop + _scroll) / RowH);
        var rows = Tabs[_tab].Rows;
        if (index >= rows.Length) return;
        var item = rows[index].Item;
        if (item == Purchase.Dentist) { OpenHiring(); return; }
        if (item == Purchase.Hygienist && clinic.Blocked(item) == null) { OpenStaffHiring(); return; }
        if (clinic.Blocked(item) is { } reason) Toast(reason);
        else if (!clinic.Buy(item)) Toast("돈이 모자랍니다");
    }

    private void PressDecor(float x, float y)
    {
        if (clinic.SpaceIndex >= 1 && y is >= InsuranceY and < InsuranceY + InsuranceH) { clinic.Overclaim = !clinic.Overclaim; return; }
        int color = Math.Clamp((int)((x - SwatchLeft) / SwatchSize), 0, Palette.Colors.Length - 1);
        if (y is >= ChairSwatchY and < ChairSwatchY + SwatchSize) clinic.ChairColor = color;
        else if (y is >= InteriorSwatchY and < InteriorSwatchY + SwatchSize) clinic.InteriorColor = color;
        else return;
        ClearImages();
    }

    private static float SwatchLeft => (W - SwatchSize * Palette.Colors.Length) / 2;

    // ── 그리기 ──

    public void Render(SKCanvas canvas, int width, int height)
    {
        _scale = Math.Min(width / W, height / BaseH);
        _offsetX = (width - W * _scale) / 2;
        _height = height / _scale;
        SetScroll(_scroll);

        canvas.Clear(new SKColor(0x10, 0x2A, 0x30));
        canvas.Save();
        canvas.Translate(_offsetX, 0);
        canvas.Scale(_scale);
        canvas.ClipRect(new SKRect(0, 0, W, _height));
        if (clinic.Doctor == null) DrawPick(canvas);
        else if (PickingFace) DrawFaces(canvas);
        else if (PickingTrait) DrawTrait(canvas);
        else if (!career.Done)
        {
            DrawTraining(canvas);
            DrawFloaters(canvas);
            DrawToast(canvas);
        }
        else
        {
            DrawClinic(canvas);
            DrawEffects(canvas);
            DrawEvent(canvas);
            DrawHeader(canvas);
            DrawList(canvas);
            DrawTabs(canvas);
            if (_menu != MenuPage.Closed) DrawMenu(canvas);
            if (_arranging) DrawArranging(canvas);
            DrawFloaters(canvas);
            DrawToast(canvas);
        }
        if (clinic.Doctor != null && !PickingTrait) DrawAutoClick(canvas);
        if (InWiki) DrawWiki(canvas);
        DrawStory(canvas);
        canvas.Restore();
    }

    private void DrawPick(SKCanvas c)
    {
        var dark = new SKColor(0x13, 0x4E, 0x4A);
        Fill(c, new SKRect(0, 0, W, _height), new SKColor(0xE6, 0xF7, 0xF4));
        Label(c, "치과 키우기", W / 2, 160, 44, dark, SKTextAlign.Center);
        Label(c, "주인공을 고르세요", W / 2, 240, 24, dark, SKTextAlign.Center);
        DrawPickCard(c, new SKRect(36, PickTop, 262, PickBottom), "face_director_m", "남자 원장");
        DrawPickCard(c, new SKRect(278, PickTop, 504, PickBottom), "face_director_f", "여자 원장");
    }

    private void DrawPickCard(SKCanvas c, SKRect box, string image, string name)
    {
        Rounded(c, box, new SKColor(0xFF, 0xFB, 0xF5));
        Icon(c, image, new SKRect(box.Left + 16, box.Top + 30, box.Right - 16, box.Top + 224), name[..2]);
        Label(c, name, box.MidX, box.Bottom - 40, 24, new SKColor(0x13, 0x4E, 0x4A), SKTextAlign.Center);
    }

    private void DrawHeader(SKCanvas c)
    {
        Fill(c, new SKRect(0, 0, W, ClinicTop), new SKColor(0x0F, 0x76, 0x6E));
        for (int i = 0; i < 3; i++) Rounded(c, new SKRect(20, 22 + i * 12, 52, 27 + i * 12), SKColors.White);   // 햄버거 메뉴
        var chip = BranchChip;
        Rounded(c, chip, new SKColor(0x13, 0x4E, 0x4A));
        Label(c, BranchName(company.Selected), chip.Left + 10, chip.MidY + 6, BranchChipText, SKColors.White);
        using (var arrow = new SKPath())   // ▼ — 글꼴에 없을 수 있어 직접 그린다
        {
            float ax = chip.Right - 16, ay = chip.MidY - 2;
            arrow.MoveTo(ax - 5, ay - 3);
            arrow.LineTo(ax + 5, ay - 3);
            arrow.LineTo(ax, ay + 4);
            arrow.Close();
            _paint.Color = new SKColor(0xCC, 0xFB, 0xF1);
            c.DrawPath(arrow, _paint);
        }
        Label(c, KoreanNumber.Format(clinic.Money) + "원", chip.Right + 10, 50, 32, SKColors.White);
        Label(c, $"초당 {KoreanNumber.Format(company.RevenuePerSecond)}원", 524, 44, 20, new SKColor(0xCC, 0xFB, 0xF1), SKTextAlign.Right);
        Label(c, $"{clinic.Space.Name} · {clinic.Space.Pyeong}평 · 평판 ×{clinic.ReputationRate:0.00}", 18, 92, 20, SKColors.White);
        string line = clinic.Building ? $"공사 중 {Math.Ceiling(clinic.BuildLeft):0}초 — 처리량 50%" : clinic.Bottleneck;
        if (clinic.Building) Label(c, line, 18, 126, 17, new SKColor(0xFE, 0xF0, 0x8A));
        else DrawFactorButtons(c);
        DrawClaims(c);
    }

    private const float BranchChipText = 17;

    /// <summary>햄버거 메뉴 오른쪽의 지점 이름. 누르면 지점 목록.</summary>
    private SKRect BranchChip
    {
        get
        {
            _font.Size = BranchChipText;
            float width = _font.MeasureText(BranchName(company.Selected)) + 36;
            return new SKRect(MenuButton + 2, 22, MenuButton + 2 + width, 52);
        }
    }

    private void DrawClinic(SKCanvas c)
    {
        var area = new SKRect(0, ClinicTop, W, ClinicBottom);
        Fill(c, area, SKColors.White);
        Fill(c, area, Palette.Colors[clinic.InteriorColor].Color.WithAlpha(70));
        if (Tinted($"bg_space{clinic.SpaceIndex + 1}", clinic.InteriorColor) is { } bg) DrawCover(c, bg, area);

        int slots = clinic.Space.MaxChairs;
        int busy = (int)Math.Ceiling(clinic.ActiveChairs * Math.Min(1, clinic.ServedPerSecond / Math.Max(1e-9, clinic.Capacity)) - 1e-6);

        for (int i = 0; i < slots; i++)
        {
            var box = ChairBox(i);
            if (i >= clinic.Chairs) { Outline(c, box, new SKColor(0x99, 0xD5, 0xCB)); continue; }
            bool active = i < clinic.ActiveChairs, staffed = i < clinic.StaffedChairs, working = i < busy;
            bool solo = i >= clinic.ActiveChairs && i < clinic.ActiveChairs + clinic.HygieneChairs;
            bool here = working && DentistIsAt(i, busy);
            string name = solo ? "chair_hyg_only" : !working ? "chair_idle" : here ? (staffed ? "chair_busy_hyg" : "chair_busy") : staffed ? "chair_hyg_only" : "chair_wait";
            string dentist = here ? name + DentistSuffix(i) : name;
            var color = Palette.Colors[clinic.ChairColor].Color;
            string patient = name == "chair_idle" ? dentist : dentist + PatientSuffix(ChairCycle(i));
            if ((Tinted(patient, clinic.ChairColor) ?? Tinted(dentist, clinic.ChairColor) ?? Tinted(name, clinic.ChairColor) ?? Tinted("chair_idle", clinic.ChairColor)) is { } chair) c.DrawImage(chair, box, Sampling, _imagePaint);
            else Rounded(c, box, !active ? SKColors.LightGray : working ? color : color.WithAlpha(120));
            if ((solo || staffed) && Image(StaffFace(i)) is { } nurse)
            {
                float face = box.Width * 0.26f;
                c.DrawImage(nurse, SKRect.Create(box.Left + 2, box.Bottom - face - 2, face, face), Sampling, _imagePaint);
            }
            if (solo || (working && !here && staffed)) DrawChairLabel(c, box, clinic.HygieneJob(ChairCycle(i)));
            else if (here) DrawChairLabel(c, box, clinic.TreatmentAt(ChairCycle(i)).Name);
            else if (working) DrawChairLabel(c, box, "의사 대기");
            if (!active && !solo) Badge(c, box, HireText("페이닥터", Purchase.Dentist, box), new SKColor(0xDC, 0x26, 0x26));
            else if (!staffed) Badge(c, box, HireText("위생사", Purchase.Hygienist, box), new SKColor(0xEA, 0x88, 0x0C));
        }

        DrawWaitingRoom(c);
        DrawPlacedEquipment(c);
        float size = 72 * (1 + _bounce * 0.16f);
        var tooth = SKRect.Create(ToothX - size / 2, 448 - size / 2, size, size);
        Icon(c, "tap_tooth", tooth, "진료");
        Icon(c, HeroWearing("director"), HeroBox, "원장");
        Label(c, "눌러서 직접 진료", ToothX + 20, 496, 13, new SKColor(0x13, 0x4E, 0x4A), SKTextAlign.Center);
    }

    /// <summary>진료실 오른쪽 아래 주인공 얼굴. 누르면 내 정보.</summary>
    private static readonly SKRect HeroBox = new(W - 74, 416, W - 6, 484);

    private SKRect ChairBox(int index)
    {
        var grid = GridChairBox(index);
        if (!clinic.ChairPlaces.TryGetValue(index, out var moved)) return grid;
        return SKRect.Create(PlacedX(moved) - grid.Width / 2, PlacedY(moved) - grid.Height / 2, grid.Width, grid.Height);
    }

    private SKRect GridChairBox(int index)
    {
        int slots = clinic.Space.MaxChairs;
        int columns = (int)Math.Ceiling(Math.Sqrt(slots * 2.2));
        int rows = (int)Math.Ceiling(slots / (double)columns);
        float cell = Math.Min((W - 20) / columns, 250f / rows);
        float left = (W - cell * columns) / 2, top = ClinicTop + 6;
        return new SKRect(left + index % columns * cell + 3, top + index / columns * cell + 3,
                          left + (index % columns + 1) * cell - 3, top + (index / columns + 1) * cell - 3);
    }

    private static float BadgeHeight(SKRect box) => Math.Clamp(box.Width * 0.16f, 9, 15) * 1.5f;

    private string HireText(string job, Purchase item, SKRect box) =>
        box.Width < 100 ? $"{job} 고용" : $"{job} 고용 {KoreanNumber.Format(clinic.Cost(item))}";

    /// <summary>체어의 고용 표시를 누르면 바로 고용한다. 눌렀으면 참.</summary>
    private bool PressBadge(float x, float y)
    {
        for (int i = 0; i < clinic.Chairs; i++)
        {
            var box = ChairBox(i);
            if (x < box.Left || x > box.Right || y < box.Bottom - BadgeHeight(box) || y > box.Bottom) continue;
            Purchase? item = i >= clinic.ActiveChairs ? Purchase.Dentist : i >= clinic.StaffedChairs ? Purchase.Hygienist : null;
            if (item is not { } hire || (hire == Purchase.Dentist && i < clinic.ActiveChairs + clinic.HygieneChairs)) return false;
            if (hire == Purchase.Dentist) { OpenHiring(); return true; }
            OpenStaffHiring();
            return true;
        }
        return false;
    }

    /// <summary>체어에 누운 환자. 이 치과의 연령 비율대로 뽑고, 같은 진료 동안은 같은 사람이다. 빈 문자열은 성인 남성 그림이다.</summary>
    private string PatientSuffix(int seed)
    {
        uint hash = (uint)seed * 2654435761u;
        double roll = (hash >> 8) % 1000 / 1000.0;
        bool woman = (hash & 1) == 0;
        var mix = clinic.AgeMix;
        double total = mix.Sum();
        if ((roll -= mix[0] / total) < 0) return "_kid";
        if ((roll -= (mix[1] + mix[2]) / total) < 0) return woman ? "_woman" : "";
        return woman ? "_gran" : "_grandpa";
    }

    /// <summary>의사 1명이 맡은 체어(3대)를 돌아다닌다. 일하는 체어끼리 3초마다 옮겨 간다.</summary>
    private bool DentistIsAt(int chair, int busy)
    {
        int team = chair / 3, first = team * 3;
        int count = Math.Min(first + 3, busy) - first;
        return count > 0 && (chair - first) == ((int)(_time / DentistStaySeconds) + team) % count;
    }

    /// <summary>앞 3대는 주인공이 보고, 그 뒤는 고용한 의사가 차례로 3대씩 본다. 그 의사의 성별 그림을 쓴다.</summary>
    private string DentistSuffix(int chair)
    {
        int team = chair / 3;
        if (team == 0) return Suffix;
        if (team - 1 < clinic.Hired.Count) return clinic.Hired[team - 1].Gender == Gender.Female ? "_f" : "_m";
        return (Suffix == "_f") == (team % 2 == 0) ? "_f" : "_m";
    }

    private void DrawTabs(SKCanvas c)
    {
        float width = W / Tabs.Length;
        for (int i = 0; i < Tabs.Length; i++)
        {
            var box = new SKRect(i * width, ClinicBottom, (i + 1) * width, ListTop);
            Fill(c, box, i == _tab ? new SKColor(0xFF, 0xFB, 0xF5) : new SKColor(0x13, 0x4E, 0x4A));
            Label(c, Tabs[i].Name, box.MidX, box.MidY + 7, 18, i == _tab ? new SKColor(0x13, 0x4E, 0x4A) : SKColors.White, SKTextAlign.Center);
        }
    }

    private void DrawList(SKCanvas c)
    {
        Fill(c, new SKRect(0, ListTop, W, _height), new SKColor(0xFF, 0xFB, 0xF5));
        c.Save();
        c.ClipRect(new SKRect(0, ListTop, W, _height));
        var rows = Tabs[_tab].Rows;
        if (_tab == ClinicTab && _hiring) DrawHiring(c);
        else if (_tab == SkillTab) DrawSkills(c);
        else if (_tab == ScheduleTab) DrawSchedule(c);
        else if (_tab == ItemTab) DrawItems(c);
        else for (int i = 0; i < rows.Length; i++) DrawRow(c, rows[i], ListTop + i * RowH - _scroll);
        if (_tab == BranchTab) DrawBranches(c);
        c.Restore();
    }

    private void DrawDecor(SKCanvas c)
    {
        var dark = new SKColor(0x13, 0x4E, 0x4A);
        Label(c, $"체어 색상 — {Palette.Colors[clinic.ChairColor].Name}", 20, ListTop + 32, 21, dark);
        DrawSwatches(c, ListTop + ChairSwatchY, clinic.ChairColor);
        Label(c, $"인테리어 색상 — {Palette.Colors[clinic.InteriorColor].Name}", 20, ListTop + 152, 21, dark);
        DrawSwatches(c, ListTop + InteriorSwatchY, clinic.InteriorColor);
        DrawInsurance(c);
    }

    private void DrawSwatches(SKCanvas c, float y, int selected)
    {
        for (int i = 0; i < Palette.Colors.Length; i++)
        {
            var (name, color) = Palette.Colors[i];
            var box = SKRect.Create(SwatchLeft + i * SwatchSize + 12, y + 2, 48, 48);
            _paint.Style = SKPaintStyle.Fill;
            _paint.Color = color;
            c.DrawOval(box, _paint);
            if (i == selected)
            {
                _paint.Style = SKPaintStyle.Stroke;
                _paint.StrokeWidth = 4;
                _paint.Color = new SKColor(0x13, 0x4E, 0x4A);
                c.DrawOval(box, _paint);
            }
            Label(c, name, box.MidX, y + 68, 14, new SKColor(0x6B, 0x72, 0x80), SKTextAlign.Center);
        }
    }

    private void DrawRow(SKCanvas c, Row row, float y)
    {
        var dark = new SKColor(0x13, 0x4E, 0x4A);
        var gray = new SKColor(0x6B, 0x72, 0x80);
        Fill(c, new SKRect(0, y + RowH - 1, W, y + RowH), new SKColor(0xE5, 0xE7, 0xEB));
        var iconBox = new SKRect(12, y + 12, 76, y + 76);
        if (row.Image == "chair_idle" && Tinted(row.Image, clinic.ChairColor) is { } chair) c.DrawImage(chair, iconBox, Sampling, _imagePaint);
        else Icon(c, row.Item is Purchase.Hygienist or Purchase.Consultant && Image(HygienistFace(clinic.Hygienists)) != null ? HygienistFace(clinic.Hygienists) : row.Image, iconBox, row.Title[..1]);

        string title = row.Title, note = row.Note;
        if (row.Item == Purchase.Move && clinic.NextSpace is { } next)
        {
            title = $"{next.Name} ({next.Pyeong}평)";
            note = $"체어 {next.MaxChairs}대 · 유입 ×{next.Inflow:0.0} · 공사 {next.BuildSeconds / 60:0.#}분";
        }
        Label(c, title, 88, y + 30, 21, dark);
        Label(c, note, 88, y + 54, 15, gray);

        string? blocked = clinic.Blocked(row.Item);
        double gain = blocked == null ? clinic.Gain(row.Item) : 0;
        string effect = blocked ?? (gain > 0 ? $"매출 +{KoreanNumber.Format(gain)}/s" : GainZeroReason(row.Item));
        Label(c, effect, 88, y + 76, 14, blocked != null || gain <= 0 ? new SKColor(0xC2, 0x41, 0x0C) : new SKColor(0x04, 0x78, 0x57));

        var button = new SKRect(392, y + 18, 528, y + 70);
        bool canBuy = blocked == null && clinic.Money >= clinic.Cost(row.Item);
        Rounded(c, button, canBuy ? new SKColor(0x0D, 0x94, 0x88) : new SKColor(0xD1, 0xD5, 0xDB));
        string price = double.IsInfinity(clinic.Cost(row.Item)) || blocked is "구매 완료" or "고용 완료" ? "—" : KoreanNumber.Format(clinic.Cost(row.Item));
        Label(c, price, button.MidX, button.MidY + 8, 22, canBuy ? SKColors.White : gray, SKTextAlign.Center);
    }

    /// <summary>사도 매출이 그대로일 때 플레이어에게 보여 줄 까닭(기획 02 UI 필수 사항).</summary>
    private string GainZeroReason(Purchase item) => item switch
    {
        Purchase.Chair when clinic.Chairs >= clinic.Dentists * 3 => "위생사를 더 뽑으면 단독 진료를 합니다",
        Purchase.Dentist when clinic.Chairs <= clinic.Dentists * 3 => "지금은 효과 없음 — 체어 부족",
        Purchase.Desk => "지금은 효과 없음 — 처리 부족",
        Purchase.Sterilizer => "지금은 위생사 피로가 없습니다",
        Purchase.Consultant => "비급여 진료가 없어 지금은 클레임 해소만 돕습니다",
        Purchase.Move when clinic.Chairs < clinic.Space.MaxChairs => $"체어를 {clinic.Space.MaxChairs}대 채워야 합니다",
        Purchase.Move => "체어 자리가 늘어납니다 — 체어를 더 사면 매출 상승",
        _ => "지금은 효과 없음 — 유입 부족",
    };

    private void DrawFloaters(SKCanvas c)
    {
        foreach (var f in _floaters)
        {
            byte alpha = (byte)(255 * (1 - f.Age / 0.8f));
            Label(c, f.Text, f.X, f.Y - f.Age * 90, f.Size, new SKColor(0x04, 0x78, 0x57, alpha), SKTextAlign.Center);
        }
    }

    private void DrawToast(SKCanvas c)
    {
        if (_toastLeft <= 0) return;
        var box = new SKRect(40, ClinicTop + 12, W - 40, ClinicTop + 60);
        Rounded(c, box, new SKColor(0x11, 0x18, 0x27, 225));
        Label(c, _toast, W / 2, box.MidY + 7, 19, SKColors.White, SKTextAlign.Center);
    }

    // ── 도구 ──

    private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);

    private SKImage? Image(string name)
    {
        if (_images.TryGetValue(name, out var image)) return image;
        using var stream = openImage(name + ".png");
        return _images[name] = stream == null ? null : SKImage.FromEncodedData(stream);
    }

    /// <summary>바꿀 부분을 고른 색으로 칠한 그림. 색을 바꾸면 <see cref="ClearImages"/> 로 비운다.</summary>
    private SKImage? Tinted(string name, int color)
    {
        string key = name + "#tint";
        if (_images.TryGetValue(key, out var image)) return image;
        using var stream = openImage(name + ".png");
        if (stream == null) return _images[key] = null;
        using var bitmap = SKBitmap.Decode(stream);
        bitmap.Pixels = Palette.Recolor(bitmap.Pixels, Palette.Colors[color].Color);
        return _images[key] = SKImage.FromBitmap(bitmap);
    }

    private void ClearImages()
    {
        foreach (var image in _images.Values) image?.Dispose();
        _images.Clear();
    }

    private void DrawCover(SKCanvas c, SKImage image, SKRect area)
    {
        float scale = Math.Max(area.Width / image.Width, area.Height / image.Height);
        float w = area.Width / scale, h = area.Height / scale;
        var source = SKRect.Create((image.Width - w) / 2, (image.Height - h) / 2, w, h);
        c.DrawImage(image, source, area, Sampling, _imagePaint);
    }

    /// <summary>그림이 있으면 그림, 아직 없으면 글자 든 동그라미.</summary>
    private void Icon(SKCanvas c, string name, SKRect box, string fallback)
    {
        if (Image(name) is { } image) { c.DrawImage(image, box, Sampling, _imagePaint); return; }
        _paint.Style = SKPaintStyle.Fill;
        _paint.Color = new SKColor(0x5E, 0xEA, 0xD4);
        c.DrawOval(box, _paint);
        Label(c, fallback, box.MidX, box.MidY + box.Height * 0.14f, box.Height * 0.36f, new SKColor(0x13, 0x4E, 0x4A), SKTextAlign.Center);
    }

    private void Badge(SKCanvas c, SKRect box, string text, SKColor color)
    {
        float size = Math.Clamp(box.Width * 0.16f, 9, 15);
        var strip = new SKRect(box.Left, box.Bottom - size * 1.5f, box.Right, box.Bottom);
        Fill(c, strip, color);
        Label(c, text, box.MidX, strip.Bottom - size * 0.35f, size, SKColors.White, SKTextAlign.Center);
    }

    private void Fill(SKCanvas c, SKRect box, SKColor color)
    {
        _paint.Style = SKPaintStyle.Fill;
        _paint.Color = color;
        c.DrawRect(box, _paint);
    }

    private void Rounded(SKCanvas c, SKRect box, SKColor color)
    {
        _paint.Style = SKPaintStyle.Fill;
        _paint.Color = color;
        c.DrawRoundRect(box, 10, 10, _paint);
    }

    private void Outline(SKCanvas c, SKRect box, SKColor color)
    {
        _paint.Style = SKPaintStyle.Stroke;
        _paint.StrokeWidth = 2;
        _paint.Color = color;
        c.DrawRoundRect(box, 10, 10, _paint);
    }

    private void Label(SKCanvas c, string text, float x, float y, float size, SKColor color, SKTextAlign align = SKTextAlign.Left)
    {
        _font.Size = size;
        _paint.Style = SKPaintStyle.Fill;
        _paint.Color = color;
        c.DrawText(text.Replace('−', '-'), x, y, align, _font, _paint);   // 맑은 고딕에 − (U+2212) 글자가 없다
    }

    public void Dispose()
    {
        ClearImages();
        _paint.Dispose();
        _imagePaint.Dispose();
        _font.Dispose();
    }
}
