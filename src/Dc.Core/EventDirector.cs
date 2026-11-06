namespace Dc.Core;

public enum EventKind { None, Emergency, Complaint, Review, Breakdown, Resignation, Audit, Salesman, RestAdvice }

/// <summary>화면에 띄울 이벤트 카드.</summary>
public sealed record EventCard(string Title, string Body, string Image, string[] Choices);

/// <summary>
/// 랜덤 이벤트. 기획 노트 12 이벤트. 켜 놓은 동안에만 돌아가므로 자리를 비운 사이에는 벌을 받지 않는다.
/// 카드를 그대로 두면 시간이 지나 무난한 쪽으로 끝난다.
/// </summary>
public sealed class EventDirector
{
    public const int EmergencyGoal = 30, HonestAudits = 5;
    public const double EmergencySeconds = 10, CardSeconds = 30, ReviewSeconds = 300, BreakdownSeconds = 300, ComplaintSeconds = 3600;
    public const double CaughtChance = 0.3;
    private const double EmergencyBonusSeconds = 60, FineSeconds = 3600, FineRate = 0.5, EmergencyPatients = 50, RepairSeconds = 60;

    private static readonly (string Line, string[] Answers, int Right)[] Complaints =
    [
        ("\"왜 이렇게 오래 기다려요?\"", ["사과하고 상황을 설명한다", "원장이 나가서 따진다", "못 들은 척한다"], 0),
        ("\"스케일링했더니 이가 시려요!\"", ["다른 치과로 가시라고 한다", "증상을 설명하고 재진을 잡아 준다", "사과 없이 환불해 보낸다"], 1),
        ("\"리뷰에 별점 1개 줄 거예요.\"", ["고소하겠다고 말한다", "무료 임플란트를 약속한다", "불편한 점을 듣고 해결책을 낸다"], 2),
    ];

    public double[] Timers { get; set; } = Enum.GetValues<EventKind>().Select(Interval).ToArray();
    public EventKind Active { get; set; }
    public double ActiveLeft { get; set; }
    public int EmergencyTaps { get; set; }
    public int ComplaintCase { get; set; }
    public int AuditsPassed { get; set; }
    public double StallSeconds { get; set; }       // 유입이 처리량에 못 미친 시간
    public double SalesmanWait { get; set; }
    public bool RestAdvised { get; set; }          // 이번 피로에 이미 권했는가

    /// <summary>피로가 이만큼 차면 휴식을 권하고, 이 아래로 풀려야 다시 권한다.</summary>
    public const double RestAdviceFatigue = 70, RestAdviceReset = 40;

    /// <summary>유입 정체가 이만큼 이어지면 영업사원이 찾아온다.</summary>
    public const double StallLimit = 120, SalesmanCooldown = 600;

    private static double Interval(EventKind kind) => kind switch
    {
        EventKind.Emergency => 240,
        EventKind.Complaint => 600,
        EventKind.Review => 900,
        EventKind.Breakdown => 1800,
        EventKind.Resignation => 3600,
        EventKind.Audit => 7200,
        _ => 0,   // None 은 쓰지 않는다. 무한대는 JSON 으로 저장되지 않는다
    };

    private static bool Eligible(EventKind kind, Clinic c) => kind switch
    {
        EventKind.Breakdown => c.Chairs >= 5 && c.BrokenLeft <= 0,
        EventKind.Resignation => c.Hygienists > 0 && c.Hygienists + c.Desks + c.Dentists - 1 >= 10,
        EventKind.Audit => c.SpaceIndex >= 1,
        _ => true,
    };

    /// <summary>시간을 보낸다. 플레이어에게 알릴 말이 있으면 돌려준다.</summary>
    public string? Tick(double seconds, Clinic clinic, Random random)
    {
        if (Active != EventKind.None)
        {
            ActiveLeft -= seconds;
            if (ActiveLeft > 0) return null;
            return Expire(clinic);
        }
        if (clinic.Fatigue < RestAdviceReset) RestAdvised = false;
        if (clinic.Fatigue >= RestAdviceFatigue && !RestAdvised && !clinic.Resting)
        {
            RestAdvised = true;
            Active = EventKind.RestAdvice;
            ActiveLeft = CardSeconds;
            return null;
        }
        SalesmanWait = Math.Max(0, SalesmanWait - seconds);
        StallSeconds = clinic.Inflow < clinic.Capacity ? StallSeconds + seconds : 0;
        if (StallSeconds >= StallLimit && SalesmanWait <= 0)
        {
            Active = EventKind.Salesman;
            ActiveLeft = CardSeconds;
            StallSeconds = 0;
            SalesmanWait = SalesmanCooldown;
            return null;
        }
        foreach (var kind in Enum.GetValues<EventKind>().Where(k => k is not (EventKind.None or EventKind.Salesman or EventKind.RestAdvice)))
        {
            // 클레임이 쌓일수록 진상 환자가 자주 온다
            Timers[(int)kind] -= kind == EventKind.Complaint ? seconds * (1 + clinic.Claims / 25) : seconds;
            if (Timers[(int)kind] > 0) continue;
            Timers[(int)kind] = Interval(kind) * (0.8 + random.NextDouble() * 0.4);
            if (!Eligible(kind, clinic)) continue;
            if (kind == EventKind.Audit) return Audit(clinic, random);
            Start(kind, clinic, random);
            return null;
        }
        return null;
    }

    private void Start(EventKind kind, Clinic clinic, Random random)
    {
        Active = kind;
        ActiveLeft = kind == EventKind.Emergency ? EmergencySeconds : CardSeconds;
        EmergencyTaps = 0;
        if (kind == EventKind.Complaint) ComplaintCase = random.Next(Complaints.Length);
        if (kind == EventKind.Breakdown) clinic.BrokenLeft = BreakdownSeconds * Penalty(clinic);
    }

    private string? Expire(Clinic clinic)
    {
        var kind = Active;
        Active = EventKind.None;
        return kind switch
        {
            EventKind.Emergency => "응급 환자를 놓쳤습니다",
            EventKind.Resignation => Leave(clinic),
            _ => null,
        };
    }

    /// <summary>동네 치과는 페널티가 절반.</summary>
    private static double Penalty(Clinic clinic) => clinic.Specialty == Specialty.Local ? 0.5 : 1;

    private static string Leave(Clinic clinic)
    {
        clinic.LoseHygienist();
        return "치위생사 1명이 떠났습니다";
    }

    private string Audit(Clinic clinic, Random random)
    {
        if (clinic.Overclaim && random.NextDouble() < CaughtChance)
        {
            double fine = Math.Min(clinic.Money, clinic.RevenuePerSecond * FineSeconds * FineRate * Penalty(clinic));
            clinic.Money -= fine;
            return $"보험 실사에 적발 — 벌금 {KoreanNumber.Format(fine)}원";
        }
        if (clinic.Overclaim) return "보험 실사를 무사히 넘겼습니다";
        AuditsPassed++;
        if (AuditsPassed < HonestAudits || clinic.ModelClinic) return $"보험 실사 통과 ({Math.Min(AuditsPassed, HonestAudits)}/{HonestAudits})";
        clinic.ModelClinic = true;
        return "모범 치과 선정 — 평판 +10%";
    }

    /// <summary>응급 환자 연타. 목표를 채우면 보상하고 끝낸다.</summary>
    public string? Tap(Clinic clinic)
    {
        if (Active != EventKind.Emergency || ++EmergencyTaps < EmergencyGoal) return null;
        double reward = clinic.RevenuePerSecond * EmergencyBonusSeconds;
        clinic.Money += reward;
        clinic.Revenue += reward;
        clinic.Patients += EmergencyPatients;
        Active = EventKind.None;
        return $"응급 처치 성공 +{KoreanNumber.Format(reward)}원";
    }

    public EventCard? Card(Clinic c) => Active switch
    {
        EventKind.Emergency => new("응급 환자!", $"연타하세요 {EmergencyTaps}/{EmergencyGoal}", "ev_emergency", []),
        EventKind.Complaint => new("진상 환자", Complaints[ComplaintCase].Line, "ev_complaint", Complaints[ComplaintCase].Answers),
        EventKind.Review => new("좋은 리뷰가 올라왔습니다", "5분 동안 유입 ×1.5", "ev_review", ["받기"]),
        EventKind.Breakdown => new("장비 고장", "체어 1대가 5분 동안 멈춥니다", "ev_broken",
            [$"수리한다 ({KoreanNumber.Format(RepairCost(c))}원)", "기다린다"]),
        EventKind.Resignation => new("직원 이직", "치위생사가 이직을 고민합니다", "ic_warn",
            [$"붙잡는다 ({KoreanNumber.Format(RetainCost(c))}원)", "보낸다"]),
        EventKind.RestAdvice => new(c.Hygienists > 0 ? "치과위생사" : "데스크 직원", "\"원장님, 안색이 안 좋으세요. 잠깐 쉬고 오세요.\"",
            c.Hygienists > 0 ? "face_hygienist" : "face_desk", ["쉰다 (30초, 피로 회복 5배)", "조금만 더 한다"]),
        EventKind.Salesman => new("치과 재료 영업사원", SalesmanAdvice(c), "ic_patient", ["고맙습니다"]),
        _ => null,
    };

    /// <summary>지금 유입을 늘릴 가장 쉬운 방법.</summary>
    public static string SalesmanAdvice(Clinic c)
    {
        if (c.Desks == 0) return "\"원장님, 데스크 직원부터 두시죠. 전화만 받아도 환자가 늘어요.\"";
        if (c.Mastery.Level(Ability.Care) < 2) return "\"환자 관리에 신경 쓰시면 소개 환자가 늘어요. 스킬 탭을 보세요.\"";
        if (c.Claims >= 20) return "\"클레임이 평판을 깎아요. 정확한 진단을 키우세요.\"";
        if (c.NextSpace != null && c.Chairs >= c.Space.MaxChairs) return "\"큰 곳으로 옮기면 간판이 잘 보여서 환자가 더 와요.\"";
        return "\"데스크를 더 뽑으면 들어오는 환자가 확 늘어요.\"";
    }

    private static double RepairCost(Clinic c) => c.RevenuePerSecond * RepairSeconds;
    private static double RetainCost(Clinic c) => c.Cost(Purchase.Hygienist);

    /// <summary>카드의 선택지를 고른다. 플레이어에게 알릴 말을 돌려준다.</summary>
    public string? Choose(int index, Clinic clinic)
    {
        switch (Active)
        {
            case EventKind.Complaint:
                Active = EventKind.None;
                if (index == Complaints[ComplaintCase].Right) return "환자가 진정했습니다";
                clinic.ComplaintLeft = ComplaintSeconds;
                return "평판이 1시간 동안 5% 떨어졌습니다";
            case EventKind.RestAdvice:
                Active = EventKind.None;
                return index == 0 && clinic.Rest() ? "30초 휴식 — 피로가 5배 빨리 풀립니다" : null;
            case EventKind.Salesman:
                Active = EventKind.None;
                return null;
            case EventKind.Review:
                Active = EventKind.None;
                clinic.ReviewLeft = ReviewSeconds;
                return null;
            case EventKind.Breakdown:
                if (index == 1) { Active = EventKind.None; return null; }
                if (!Pay(clinic, RepairCost(clinic))) return "돈이 모자랍니다";
                clinic.BrokenLeft = 0;
                Active = EventKind.None;
                return "체어를 고쳤습니다";
            case EventKind.Resignation:
                if (index == 1) { Active = EventKind.None; return Leave(clinic); }
                if (!Pay(clinic, RetainCost(clinic))) return "돈이 모자랍니다";
                Active = EventKind.None;
                return "치위생사가 남기로 했습니다";
            default:
                return null;
        }
    }

    private static bool Pay(Clinic clinic, double cost)
    {
        if (clinic.Money < cost) return false;
        clinic.Money -= cost;
        return true;
    }
}
