using Dc.Core;
using SkiaSharp;

namespace Dc.Ui;

/// <summary>페이닥터 지원자 화면 — 연차 · 진료과목 · 계약금 · 월급을 보고 고른다.</summary>
public sealed partial class GameScreen
{
    private const int DeskFaces = 4;
    private bool _hiring, _hiringStaff;

    private void OpenStaffHiring()
    {
        _tab = ClinicTab;
        _hiring = _hiringStaff = true;
        _scroll = 0;
        if (clinic.StaffCandidates.Count == 0) clinic.RefreshStaffCandidates(Random.Shared);
    }

    /// <summary>체어에 보이는 얼굴. 자동 경영이 뽑아 명부에 없는 사람은 번호로 정한다.</summary>
    private string StaffFace(int chair) => chair < clinic.Staff.Count ? $"hyg_{clinic.Staff[chair].Face}" : HygienistFace(chair);

    private IReadOnlyList<Choice> StaffChoices()
    {
        var list = new List<Choice>
        {
            new("뒤로", "진료실 목록으로 돌아갑니다", "←", true, "mn_close", () => { _hiring = _hiringStaff = false; _scroll = 0; }),
            new($"위생사 {clinic.Hygienists} · 조무사 {clinic.Assistants} · 체어 {clinic.Chairs}", $"월급 합계 {KoreanNumber.Format(clinic.TotalSalary)}/s", "정보", false, "face_hygienist", () => { }),
        };
        foreach (var hire in clinic.StaffCandidates.ToList())
            list.Add(new($"{hire.Name} · {hire.Years}년 · {hire.FieldName}", $"월급 {KoreanNumber.Format(hire.Salary)}/s · {hire.Strength}",
                KoreanNumber.Format(clinic.FeeOf(hire)), clinic.Money >= clinic.FeeOf(hire) && clinic.Blocked(Purchase.Hygienist) == null, $"hyg_{hire.Face}", () => clinic.HireStaff(hire)));
        list.Add(new("지원자 다시 받기", "새 이력서 3장을 받습니다", "무료", true, "ic_patient", () => clinic.RefreshStaffCandidates(Random.Shared)));
        return list;
    }

    private void OpenHiring()
    {
        _tab = ClinicTab;
        _hiring = true;
        _hiringStaff = false;
        _scroll = 0;
        if (clinic.Candidates.Count == 0) clinic.RefreshCandidates(Random.Shared);
    }

    private IReadOnlyList<Choice> HiringChoices()
    {
        if (_hiringStaff) return StaffChoices();
        var list = new List<Choice>
        {
            new("뒤로", "진료실 목록으로 돌아갑니다", "←", true, "mn_close", () => { _hiring = false; _scroll = 0; }),
            new($"페이닥터 {clinic.Hired.Count}명 · 월급 합계 {KoreanNumber.Format(clinic.TotalSalary)}/s",
                $"매출 {KoreanNumber.Format(clinic.GrossPerSecond)}/s → 순이익 {KoreanNumber.Format(clinic.RevenuePerSecond)}/s", "정보", false, "face_dentist", () => { }),
        };
        foreach (var hire in clinic.Candidates.ToList())
        {
            double gain = clinic.GainOf(hire);
            string effect = clinic.CanAffordSalary(hire) ? $"순이익 {(gain >= 0 ? "+" : "−")}{KoreanNumber.Format(Math.Abs(gain))}/s" : "월급 부담 큼";
            list.Add(new($"{hire.Name} · {hire.Years}년 · {hire.SubjectName}", $"월급 {KoreanNumber.Format(hire.Salary)}/s · {hire.Strength} · {effect}",
                KoreanNumber.Format(clinic.FeeOf(hire)), clinic.Money >= clinic.FeeOf(hire), hire.Face, () => clinic.Hire(hire)));
        }
        list.Add(new("지원자 다시 받기", "새 이력서 3장을 받습니다", "무료", true, "ic_patient", () => clinic.RefreshCandidates(Random.Shared)));
        return list;
    }

    private void PressHiring(float y)
    {
        var choices = HiringChoices();
        int index = (int)((y - ListTop + _scroll) / RowH);
        if (index >= choices.Count) return;
        if (choices[index].Enabled) choices[index].Pick();
        else if (choices[index].Price != "정보") Toast("돈이 모자랍니다");
    }

    private void DrawHiring(SKCanvas c)
    {
        var choices = HiringChoices();
        for (int i = 0; i < choices.Count; i++) DrawChoice(c, choices[i], ListTop + i * RowH - _scroll);
    }

    private const float WaitingTop = 412, WaitingRight = 372, ToothX = 420;
    private const int WaitingFaces = 6, HygienistFaces = 20;

    /// <summary>위생사마다 다른 얼굴. 20명이 넘으면 처음부터 돌아간다.</summary>
    private static string HygienistFace(int index) => $"hyg_{index % HygienistFaces + 1}";

    /// <summary>왼쪽 아래 대기실 — 데스크 직원과 기다리는 환자. 처리가 밀릴수록 환자가 늘어난다.</summary>
    private void DrawWaitingRoom(SKCanvas c)
    {
        var dark = new SKColor(0x13, 0x4E, 0x4A);
        var wall = new SKColor(0x13, 0x4E, 0x4A);
        var room = new SKRect(0, WaitingTop, WaitingRight, ClinicBottom);
        Fill(c, room, new SKColor(0xFF, 0xFB, 0xF5));
        if (Tinted($"wait_space{clinic.SpaceIndex + 1}", clinic.InteriorColor) is { } art) DrawCover(c, art, room);
        Fill(c, new SKRect(0, WaitingTop - 5, WaitingRight * 0.42f, WaitingTop), wall);   // 가운데가 진료실 문
        Fill(c, new SKRect(WaitingRight * 0.58f, WaitingTop - 5, WaitingRight, WaitingTop), wall);
        Fill(c, new SKRect(WaitingRight, WaitingTop - 5, WaitingRight + 5, ClinicBottom), wall);
        Fill(c, new SKRect(0, WaitingTop, WaitingRight, WaitingTop + 22), new SKColor(0xFF, 0xFB, 0xF5, 215));
        Label(c, "대기실", 10, WaitingTop + 17, 13, dark);

        int shown = Math.Min(clinic.Desks, DeskFaces);
        for (int i = 0; i < shown; i++)
            Icon(c, "face_desk", SKRect.Create(8 + i * 34, 436, 56, 56), "데");
        if (clinic.Desks > DeskFaces) Label(c, $"×{clinic.Desks}", 8 + shown * 34 + 34, 482, 15, dark);
        if (clinic.Desks == 0) Label(c, "데스크 없음", 10, 470, 13, new SKColor(0xC2, 0x41, 0x0C));

        int waiting = (int)Math.Ceiling(clinic.Waiting);
        for (int i = 0; i < Math.Min(waiting, WaitingFaces); i++)
            Icon(c, "ic_patient", SKRect.Create(WaitingRight - 34 - i * 26, 456, 30, 30), "환");
        bool full = clinic.Waiting >= clinic.WaitingLimit * 0.9 && waiting > 0;
        Label(c, $"대기 {waiting}/{clinic.WaitingSeats} · {clinic.WaitSeconds:0}초 · 이탈 {clinic.LostPatients:0}", WaitingRight - 6, WaitingTop + 17, 13,
            full ? new SKColor(0xC2, 0x41, 0x0C) : dark, SKTextAlign.Right);
    }
}
