using System.Text.Json.Serialization;

namespace Dc.Core;

/// <summary>대사 한 줄. Picture 를 주면 이 줄부터 뒤 그림(컷씬)이 바뀐다.</summary>
public sealed record StoryLine(string Speaker, string Text, string? Picture = null);

/// <summary>대사 한 장면. Face 는 주인공 그림(student · resident · paydoctor · director), Picture 는 뒤 그림 이름.</summary>
public sealed record StoryScene(string Id, string Face, string Picture, StoryLine[] Lines);

/// <summary>게임 진행에 맞춰 장면을 한 번씩 띄운다. 본 장면은 저장되어 다시 나오지 않는다.</summary>
public sealed class StoryDirector
{
    private const string Me = "나";

    private static readonly (StoryScene Scene, Func<Company, Career, bool> When)[] Scenes =
    [
        (new("childhood", "student", "story_childhood",
        [
            new(Me, "일곱 살 때, 동네 치과 앞에서 나는 엄마 손을 붙잡고 울었다."),
            new("어머니", "하나도 안 아파. 금방 끝나."),
            new(Me, "거짓말이었다. 그런데 그날 선생님은 내가 다 울 때까지 기다려 주셨다.", "story_kind_dentist"),
            new("치과 선생님", "잘 참았어. 다음에는 안 무서울 거야."),
            new(Me, "그 말이 오래 남았다. 나도 저런 어른이 되고 싶었다."),
        ]), (c, r) => r.Stage == CareerStage.Student),
        (new("study", "student", "story_study",
        [
            new(Me, "고등학교 3년은 독서실 불빛으로 기억한다."),
            new(Me, "남들이 잠든 새벽에도 문제집을 넘겼고, 재수할 때는 친구들 대학 사진을 일부러 보지 않았다.", "story_retake"),
            new("어머니", "밥은 먹고 하니. 네가 아프면 다 소용없어."),
            new(Me, "엄마는 야식을 내려놓고 조용히 문을 닫았다. 그 도시락을 나는 평생 못 갚을 것이다.", "story_lunchbox"),
        ]), (c, r) => r.Stage == CareerStage.Student),
        (new("student", "student", "story_letter",
        [
            new(Me, "합격 문자를 받은 날, 엄마는 주방에서 한참 나오지 않았다."),
            new(Me, "치대에 들어와서도 공부는 끝나지 않았다. 해부학, 치아 형태학, 끝없는 시험.", "story_anatomy"),
            new(Me, "그리고 오늘, 처음으로 흰 가운을 입고 병원에 들어섰다.", "story_student"),
            new("선배", "원내생이지? 토마스 앞에 앉아 봐. 손이 기억할 때까지.", "story_phantom"),
            new(Me, "드디어 원내생이 됐다. 이제 시작이다."),
        ]), (c, r) => r.Stage == CareerStage.Student),
        (new("result", "student", "story_result_call",
        [
            new(Me, "국가고시 합격. 엄마에게 전화를 걸자 목소리가 먼저 떨렸다."),
            new("어머니", "우리 애가 이제 선생님이네."),
            new(Me, "이제 정해야 한다. 수련을 더 받을까, 바로 현장으로 나갈까.", "story_crossroads"),
        ]), (c, r) => r.Stage == CareerStage.Choice),
        (new("resident", "resident", "story_resident",
        [
            new(Me, "수련의의 하루는 새벽 차트로 시작해서 밤 당직으로 끝난다."),
            new("교수님", "환자는 네 연습 상대가 아니다. 한 번을 해도 제대로 해.", "story_scold"),
            new(Me, "꾸중을 들을수록, 내 손이 조금씩 믿을 만해지는 것을 느꼈다."),
        ]), (c, r) => r.Stage == CareerStage.Resident),
        (new("paydoctor", "paydoctor", "story_paycheck",
        [
            new(Me, "페이닥터로 첫 월급을 받았다. 절반은 엄마 통장으로 보냈다."),
            new("어머니", "이런 거 안 보내도 돼. 네 병원 차릴 때 써."),
            new(Me, "내 병원. 그 말이 머릿속에서 떠나지 않았다. 2,000만 원만 모으면 대출을 받을 수 있다.", "story_dream_clinic"),
        ]), (c, r) => r.Stage == CareerStage.PayDoctor),
        (new("opening", "director", "story_opening_small",
        [
            new(Me, "상가 2층, 25평. 체어 한 대, 빚 3,000만 원."),
            new(Me, "간판 불을 켜던 밤, 일곱 살의 내가 울던 그 치과가 떠올랐다.", "story_clinic"),
            new("어머니", "간판이 참 예쁘다. 고생했다.", "story_opening_mother"),
            new(Me, "아프지 않게, 기다려 주는 치과. 그게 내 병원이다."),
        ]), (c, r) => r.Done),
        (new("move", "director", "story_waiting_full",
        [
            new(Me, "대기실 의자가 모자라기 시작했다. 옆 칸까지 벽을 텄다."),
            new(Me, "소문을 듣고 온 환자가 처음으로 내 이름을 불렀다."),
        ]), (c, r) => r.Done && c.Head.SpaceIndex >= 1),
        (new("fifty", "director", "story_medical_building",
        [
            new(Me, "메디컬빌딩 한 층. 엘리베이터 문이 열리면 우리 치과다."),
            new("실장", "원장님, 다른 동네에서도 오세요. 분원 얘기가 나옵니다.", "story_meeting"),
            new(Me, "한 곳으로는 모자란 날이 올 줄은 몰랐다."),
        ]), (c, r) => r.Done && c.Head.SpaceIndex >= 2),
        (new("branch", "director", "story_branch_open",
        [
            new(Me, "의료법인 이름으로 두 번째 치과의 문을 열었다."),
            new(Me, "이제 내 손이 닿지 않는 곳에서도 환자가 치료를 받는다. 사람을 믿는 법을 배워야 한다."),
        ]), (c, r) => r.Done && c.Branches.Count >= 2),
        (new("hospital", "director", "story_hospital_ceremony",
        [
            new(Me, "치과병원 개원식. 엄마는 맨 앞줄에 앉아 계셨다."),
            new("어머니", "그때 그 울보가 이렇게 큰 병원 원장님이 됐네."),
            new(Me, "아직도 어려운 환자 앞에서는 손에 땀이 난다. 그걸 잊지 않으려 한다."),
        ]), (c, r) => r.Done && c.Head.SpaceIndex >= 4),
        (new("tower", "director", "story_tower_night",
        [
            new(Me, "치과 전용 빌딩의 불이 켜졌다."),
            new(Me, "1층 소아 진료실에서 아이 하나가 엄마 손을 붙잡고 울고 있다.", "story_tower_child"),
            new(Me, "나는 무릎을 굽혀 눈을 맞췄다. 다 울 때까지 기다려 주기로 했다."),
            new(Me, "잘 참았어. 다음에는 안 무서울 거야."),
        ]), (c, r) => r.Done && c.Head.SpaceIndex >= 5),
    ];

    public List<string> Seen { get; set; } = [];
    public string? CurrentId { get; set; }
    public int LineIndex { get; set; }

    [JsonIgnore] public StoryScene? Current => Scenes.Select(s => s.Scene).FirstOrDefault(s => s.Id == CurrentId);
    [JsonIgnore] public StoryLine? Line => Current?.Lines[Math.Clamp(LineIndex, 0, Current.Lines.Length - 1)];

    /// <summary>지금 대사의 뒤 그림 — 가장 가까운 앞 줄에서 정한 그림, 없으면 장면 그림.</summary>
    [JsonIgnore]
    public string? Picture =>
        Current?.Lines.Take(LineIndex + 1).LastOrDefault(l => l.Picture != null)?.Picture ?? Current?.Picture;

    /// <summary>주인공 아닌 인물의 초상 그림 이름.</summary>
    public static string? Portrait(string speaker) => speaker switch
    {
        "어머니" => "face_mother",
        "치과 선생님" => "face_old_dentist",
        "선배" => "face_senior",
        "교수님" => "face_professor",
        "실장" => "face_manager",
        _ => null,
    };

    /// <summary>볼 장면이 있으면 시작한다.</summary>
    public void Update(Company company, Career career)
    {
        if (CurrentId != null) return;
        foreach (var (scene, when) in Scenes)
        {
            if (Seen.Contains(scene.Id) || !when(company, career)) continue;
            CurrentId = scene.Id;
            LineIndex = 0;
            return;
        }
    }

    public void Advance()
    {
        if (Current is not { } scene) return;
        if (++LineIndex >= scene.Lines.Length) Finish();
    }

    public void Skip() => Finish();

    private void Finish()
    {
        if (CurrentId != null) Seen.Add(CurrentId);
        CurrentId = null;
        LineIndex = 0;
    }
}
