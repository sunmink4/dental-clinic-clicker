using System.Diagnostics;
using System.Media;
using Dc.Core;
using Dc.Ui;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace Dc.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new GameForm());
    }
}

/// <summary>PC 창 — 틀 돌리기, 마우스 · 키 전달, 자동저장만 맡는다. 게임은 전부 Dc.Core · Dc.Ui.</summary>
internal sealed class GameForm : Form
{
    private const double SaveEvery = 10;

    private readonly SaveSlots _slots;
    private readonly SaveStore _store;
    private readonly Company _company;
    private readonly Career _career;
    private readonly EventDirector _events;
    private readonly StoryDirector _story;
    private readonly GameScreen _screen;
    private readonly SKControl _view = new() { Dock = DockStyle.Fill };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _last, _sinceSave;
    private SoundPlayer? _music;
    private bool _musicOn;

    public GameForm()
    {
        Text = "치과 키우기";
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        ClientSize = new Size(LogicalToDeviceUnits(540), LogicalToDeviceUnits(900));
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        Controls.Add(_view);

        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DentalClinicClicker");
        _slots = new SaveSlots(folder);
        _store = _slots.Store(_slots.Current);
        var save = _store.Load();
        _company = save?.Company ?? new Company(save?.Clinic ?? new Clinic());
        _company.Link(DateTime.Now.Month);
        // Career 없는 옛 저장은 이미 개원한 게임이다
        _career = save?.Career ?? (save != null ? new Career { Stage = CareerStage.Done } : new Career());

        _events = save?.Events ?? new EventDirector();
        _story = save?.Story ?? new StoryDirector();

        string images = Path.Combine(AppContext.BaseDirectory, "assets", "images");
        _screen = new GameScreen(_company, _career, _events, name =>
        {
            string file = Path.Combine(images, name);
            return File.Exists(file) ? File.OpenRead(file) : null;
        }, SKTypeface.FromFamilyName("Malgun Gothic")) { Story = _story, ToggleMusic = SwitchMusic, NewGame = StartOver, Slots = _slots, SwitchSlot = SwitchSlot };

        if (save != null)
        {
            double away = (DateTime.UtcNow - save.SavedUtc).TotalSeconds;
            if (!_career.Done) _career.ApplyOffline(away);
            else if (_company.ApplyOffline(away) is >= 1 and var earned)
                _screen.Toast($"자리 비운 동안 {KoreanNumber.Format(earned)}원을 벌었습니다");
        }

        _view.PaintSurface += (_, e) => _screen.Render(e.Surface.Canvas, e.Info.Width, e.Info.Height);
        _view.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) _screen.PointerDown(e.X, e.Y); };
        _view.MouseMove += (_, e) => _screen.PointerMove(e.X, e.Y);
        _view.MouseUp += (_, e) => { if (e.Button == MouseButtons.Left) _screen.PointerUp(e.X, e.Y); };
        _view.MouseWheel += (_, e) => _screen.Wheel(e.Delta / 120f);
        SwitchMusic();
        _timer.Tick += (_, _) => Frame();
        _timer.Start();
    }

    /// <summary>스페이스 · 방향키는 클릭과 같고, 누르고 있어도 한 번만 친다. M 은 음악 켜고 끄기.</summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        const int WM_KEYDOWN = 0x100, RepeatBit = 1 << 30;
        if (msg.Msg != WM_KEYDOWN) return base.ProcessCmdKey(ref msg, keyData);
        bool repeat = (msg.LParam.ToInt64() & RepeatBit) != 0;
        switch (keyData)
        {
            case Keys.Space or Keys.Left or Keys.Right or Keys.Up or Keys.Down:
                if (!repeat) _screen.Tap();
                return true;
            case Keys.M:
                if (!repeat) SwitchMusic();
                return true;
            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    private void SwitchMusic()
    {
        string file = Path.Combine(AppContext.BaseDirectory, "assets", "audio", "bgm.wav");
        if (!File.Exists(file)) return;
        _music ??= new SoundPlayer(file);
        _musicOn = !_musicOn;
        if (_musicOn) _music.PlayLooping();
        else _music.Stop();
    }

    private void Frame()
    {
        double now = _clock.Elapsed.TotalSeconds, dt = now - _last;
        _last = now;
        _screen.Update(dt);
        _sinceSave += dt;
        if (_sinceSave >= SaveEvery) Save();
        _view.Invalidate();
    }

    private bool _startingOver;

    private void SwitchSlot(int slot)
    {
        Save();
        _startingOver = true;
        _slots.Select(slot);
        Application.Restart();
    }

    private void StartOver()
    {
        _startingOver = true;
        _store.Reset();
        Application.Restart();
    }

    private void Save()
    {
        if (_startingOver) return;
        _sinceSave = 0;
        try { _store.Save(_company, _career, _events, _story, DateTime.UtcNow); }
        catch (IOException e) { _screen.Toast("저장 실패: " + e.Message); }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _music?.Stop();
        Save();
        _screen.Dispose();
        base.OnFormClosed(e);
    }
}
