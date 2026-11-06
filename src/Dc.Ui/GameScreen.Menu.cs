using Dc.Core;
using SkiaSharp;

namespace Dc.Ui;

/// <summary>왼쪽 위 원을 누르면 아래쪽에 열리는 메뉴. 꾸미기가 여기에 있다.</summary>
public sealed partial class GameScreen
{
    private const float MenuButton = 64, MenuRowH = 50;
    private enum MenuPage { Closed, Root, Decor, Confirm, Slots, Inflow, Capacity, Profile, Wiki, Branches }

    private MenuPage _menu;

    /// <summary>호스트가 음악을 켜고 끄는 동작. 없으면 메뉴에 나오지 않는다.</summary>
    public Action? ToggleMusic { get; init; }

    /// <summary>호스트가 저장을 치우고 처음부터 다시 띄우는 동작.</summary>
    public Action? NewGame { get; init; }

    /// <summary>저장 칸. 없으면 메뉴에 나오지 않는다.</summary>
    public SaveSlots? Slots { get; init; }

    /// <summary>호스트가 지금 게임을 저장하고 다른 칸으로 옮겨 가는 동작.</summary>
    public Action<int>? SwitchSlot { get; init; }

    private string[] _slotText = [];

    private void OpenSlots()
    {
        _slotText = Enumerable.Range(1, SaveSlots.Count).Select(n => Slots!.Summary(n)).ToArray();
        _menu = MenuPage.Slots;
    }

    private IReadOnlyList<Choice> SlotChoices()
    {
        int current = Slots!.Current;
        return Enumerable.Range(1, SaveSlots.Count).Select(n => new Choice($"저장 {n}", _slotText[n - 1], n == current ? "사용 중" : "열기", n != current, "mn_slots",
            () => { if (n != current) SwitchSlot?.Invoke(n); })).ToList();
    }

    private IReadOnlyList<Choice> MenuChoices()
    {
        if (_menu == MenuPage.Slots) return SlotChoices();
        if (_menu == MenuPage.Branches) return BranchSwitchChoices();
        if (_menu == MenuPage.Confirm)
            return
            [
                new("정말 처음부터 시작할까요?", "지금까지의 진행이 사라집니다", "시작", true, "mn_new", () => NewGame?.Invoke()),
                new("취소", "메뉴로 돌아갑니다", "취소", true, "mn_close", () => _menu = MenuPage.Root),
            ];
        var list = new List<Choice> { new("내 정보", "스탯 · 성향 · 전공 · 잘하는 과목", "열기", true, "mn_profile", () => _menu = MenuPage.Profile), new("꾸미기", "체어 · 인테리어 색상, 보험 청구 모드", "열기", true, "mn_decor", () => _menu = MenuPage.Decor), new("장비 배치", "체어 · 엑스레이 · CT · 멸균소독기를 놓습니다", "열기", true, "eq_ct", () => { _menu = MenuPage.Closed; _arranging = true; _scroll = 0; }) };
        if (ToggleMusic != null) list.Add(new("배경음악", "켜고 끕니다", "전환", true, "mn_music", ToggleMusic));
        if (Slots != null) list.Add(new("저장 슬롯", "칸 3개 중에서 고릅니다. 지금 게임은 저장됩니다", "열기", true, "mn_slots", OpenSlots));
        if (NewGame != null) list.Add(new("새 게임", "처음부터 다시 시작합니다", "열기", true, "mn_new", () => _menu = MenuPage.Confirm));
        list.Add(new("위키", "게임 규칙과 도움말", "열기", true, "mn_wiki", () => { _menu = MenuPage.Wiki; _wikiTopic = -1; _scroll = 0; }));
        list.Add(new("닫기", "게임으로 돌아갑니다", "닫기", true, "mn_close", () => _menu = MenuPage.Closed));
        return list;
    }

    private void PressMenu(float x, float y)
    {
        if (y < ListTop)
        {
            _menu = _menu is MenuPage.Root or MenuPage.Inflow or MenuPage.Capacity or MenuPage.Branches ? MenuPage.Closed : MenuPage.Root;
            return;
        }
        if (_menu == MenuPage.Decor) { PressDecor(x, y - ListTop); return; }
        if (_menu is MenuPage.Inflow or MenuPage.Capacity) { _menu = MenuPage.Closed; return; }
        if (_menu == MenuPage.Profile) { _menu = MenuPage.Root; return; }
        var choices = MenuChoices();
        int index = (int)((y - ListTop) / (_menu == MenuPage.Root ? MenuRowH : RowH));
        if (index < choices.Count) choices[index].Pick();
    }

    private const float FactorH = 54;

    /// <summary>유입 · 처리가 무엇으로 정해지는지 보여 준다.</summary>
    private void DrawFactors(SKCanvas c)
    {
        var dark = new SKColor(0x13, 0x4E, 0x4A);
        var gray = new SKColor(0x6B, 0x72, 0x80);
        var rows = _menu switch { MenuPage.Inflow => clinic.InflowFactors, MenuPage.Capacity => clinic.CapacityFactors, _ => career.Profile(clinic.Mastery) };
        if (_menu == MenuPage.Profile)
        {
            // 줄이 많아 목록 칸에는 자리가 없으므로 위쪽 진료실 자리에 그린다
            Fill(c, new SKRect(0, ClinicTop, W, ClinicBottom), new SKColor(0xFF, 0xFB, 0xF5));
            float size = ClinicBottom - ClinicTop - 20;
            Icon(c, HeroWearing("director"), SKRect.Create(W / 2 - size / 2, ClinicTop + 10, size, size), "나");
        }
        float height = Math.Min(FactorH, (_height - ListTop) / rows.Count);
        for (int i = 0; i < rows.Count; i++)
        {
            float y = ListTop + i * height;
            Label(c, rows[i].Name, 20, y + 26, 19, dark);
            Label(c, rows[i].Value, W - 20, y + 26, 19, dark, SKTextAlign.Right);
            Label(c, rows[i].How, 20, y + 44, 13, gray);
            Fill(c, new SKRect(0, y + height - 1, W, y + height), new SKColor(0xE5, 0xE7, 0xEB));
        }
    }

    private void DrawMenu(SKCanvas c)
    {
        Fill(c, new SKRect(0, ClinicBottom, W, _height), new SKColor(0xFF, 0xFB, 0xF5));
        Fill(c, new SKRect(0, ClinicBottom, W, ListTop), new SKColor(0x13, 0x4E, 0x4A));
        Label(c, _menu switch { MenuPage.Decor => "←  꾸미기", MenuPage.Confirm => "←  새 게임", MenuPage.Slots => "←  저장 슬롯", MenuPage.Profile => "←  내 정보", MenuPage.Branches => "←  지점", MenuPage.Inflow => $"←  유입 {clinic.Inflow:0.00}명/s", MenuPage.Capacity => $"←  처리 {clinic.Capacity:0.00}명/s", _ => "←  메뉴" }, 20, ClinicBottom + 35, 21, SKColors.White);
        if (_menu == MenuPage.Decor) { DrawDecor(c); return; }
        if (_menu is MenuPage.Inflow or MenuPage.Capacity or MenuPage.Profile) { DrawFactors(c); return; }
        var choices = MenuChoices();
        if (_menu == MenuPage.Root)
        {
            var dark = new SKColor(0x13, 0x4E, 0x4A);
            for (int i = 0; i < choices.Count; i++)
            {
                float y = ListTop + i * MenuRowH;
                Icon(c, choices[i].Image, SKRect.Create(14, y + (MenuRowH - 42) / 2, 42, 42), choices[i].Title[..1]);
                Label(c, choices[i].Title, 68, y + 22, 19, dark);
                Label(c, choices[i].Note, 68, y + 41, 12, new SKColor(0x6B, 0x72, 0x80));
                Label(c, "›", W - 24, y + 32, 22, dark, SKTextAlign.Right);
                Fill(c, new SKRect(0, y + MenuRowH - 1, W, y + MenuRowH), new SKColor(0xE5, 0xE7, 0xEB));
            }
            return;
        }
        for (int i = 0; i < choices.Count; i++) DrawChoice(c, choices[i], ListTop + i * RowH);
    }
}
