using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Views;
using Dc.Core;
using Dc.Ui;
using SkiaSharp;
using SkiaSharp.Views.Android;

[assembly: Application(Icon = "@mipmap/ic_launcher", RoundIcon = "@mipmap/ic_launcher_round")]

namespace Dc.Android;

/// <summary>안드로이드 창 — 틀 돌리기, 터치 전달, 음악, 저장만 맡는다. 게임은 전부 Dc.Core · Dc.Ui.</summary>
[Activity(Label = "치과 키우기", MainLauncher = true, Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
          ScreenOrientation = ScreenOrientation.Portrait, LaunchMode = LaunchMode.SingleTask)]
public class MainActivity : Activity
{
    private const int FrameMilliseconds = 16;
    private const double SaveEvery = 10;

    private SaveSlots _slots = null!;
    private SaveStore _store = null!;
    private Company _company = null!;
    private Career _career = null!;
    private EventDirector _events = null!;
    private StoryDirector _story = null!;
    private GameScreen _screen = null!;
    private SKCanvasView _view = null!;
    private MediaPlayer? _music;
    private readonly Handler _handler = new(Looper.MainLooper!);
    private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
    private double _last, _sinceSave;
    private bool _running;
    private HashSet<string> _images = [];

    protected override void OnCreate(Bundle? state)
    {
        base.OnCreate(state);
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);

        _images = (Assets!.List("images") ?? []).ToHashSet();
        _slots = new SaveSlots(FilesDir!.AbsolutePath);
        _store = _slots.Store(_slots.Current);
        var save = _store.Load();
        _company = save?.Company ?? new Company(save?.Clinic ?? new Clinic());
        _company.Link(DateTime.Now.Month);
        _career = save?.Career ?? (save != null ? new Career { Stage = CareerStage.Done } : new Career());
        _events = save?.Events ?? new EventDirector();
        _story = save?.Story ?? new StoryDirector();
        _screen = new GameScreen(_company, _career, _events, OpenImage, KoreanTypeface()) { Story = _story, ToggleMusic = SwitchMusic, NewGame = StartOver, Slots = _slots, SwitchSlot = SwitchSlot };
        if (save != null) Offline(save.SavedUtc);

        _view = new SKCanvasView(this);
        _view.PaintSurface += (_, e) => _screen.Render(e.Surface.Canvas, e.Info.Width, e.Info.Height);
        _view.Touch += OnTouch;
        SetContentView(_view);
    }

    /// <summary>시스템 글꼴 중 한글을 그릴 수 있는 것.</summary>
    private static SKTypeface KoreanTypeface() =>
        SKFontManager.Default.MatchCharacter(null, SKFontStyle.Normal, ["ko"], '가') ?? SKTypeface.Default;

    private System.IO.Stream? OpenImage(string name) => _images.Contains(name) ? Assets!.Open("images/" + name) : null;

    private void OnTouch(object? sender, global::Android.Views.View.TouchEventArgs e)
    {
        if (e.Event is not { } touch) return;
        switch (touch.ActionMasked)
        {
            case MotionEventActions.Down: _screen.PointerDown(touch.GetX(), touch.GetY()); break;
            case MotionEventActions.Move: _screen.PointerMove(touch.GetX(), touch.GetY()); break;
            case MotionEventActions.Up: _screen.PointerUp(touch.GetX(), touch.GetY()); break;
        }
        e.Handled = true;
    }

    private void Frame()
    {
        if (!_running) return;
        double now = _clock.Elapsed.TotalSeconds, dt = now - _last;
        _last = now;
        _screen.Update(dt);
        _sinceSave += dt;
        if (_sinceSave >= SaveEvery) Save();
        _view.Invalidate();
        _handler.PostDelayed(Frame, FrameMilliseconds);
    }

    private bool _startingOver;

    private void SwitchSlot(int slot)
    {
        Save();
        _startingOver = true;
        _slots.Select(slot);
        Recreate();
    }

    private void StartOver()
    {
        _startingOver = true;
        _store.Reset();
        Recreate();
    }

    private void Save()
    {
        if (_startingOver) return;
        _sinceSave = 0;
        try { _store.Save(_company, _career, _events, _story, DateTime.UtcNow); }
        catch (IOException e) { _screen.Toast("저장 실패: " + e.Message); }
    }

    private void Offline(DateTime savedUtc)
    {
        double away = (DateTime.UtcNow - savedUtc).TotalSeconds;
        if (!_career.Done) _career.ApplyOffline(away);
        else if (_company.ApplyOffline(away) is >= 1 and var earned)
            _screen.Toast($"자리 비운 동안 {KoreanNumber.Format(earned)}원을 벌었습니다");
    }

    private DateTime _pausedUtc;

    protected override void OnResume()
    {
        base.OnResume();
        if (_pausedUtc != default) Offline(_pausedUtc);
        _last = _clock.Elapsed.TotalSeconds;
        _running = true;
        Frame();
        StartMusic();
    }

    protected override void OnPause()
    {
        base.OnPause();
        _running = false;
        _pausedUtc = DateTime.UtcNow;
        Save();
        _music?.Pause();
    }

    private bool _musicOff;

    private void SwitchMusic()
    {
        _musicOff = !_musicOff;
        if (_musicOff) _music?.Pause();
        else StartMusic();
    }

    private void StartMusic()
    {
        if (_musicOff) return;
        if (_music != null) { _music.Start(); return; }
        using var file = Assets!.OpenFd("audio/bgm.wav");
        _music = new MediaPlayer { Looping = true };
        _music.SetDataSource(file.FileDescriptor, file.StartOffset, file.Length);
        _music.Prepare();
        _music.Start();
    }

    protected override void OnDestroy()
    {
        _music?.Release();
        _music = null;
        base.OnDestroy();
    }
}
