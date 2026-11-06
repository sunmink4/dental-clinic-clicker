using System.Text.Json.Serialization;

namespace Dc.Core;

public enum Purchase { Chair, Dentist, Bundle, Hygienist, Desk, Manager, Orthodontist, Scanner, Ct, Skill, Move, AutoManage, Sterilizer, Consultant }

public enum Gender { Male, Female }

/// <summary>개원 단계 한 지점의 상태와 규칙. 수식은 기획 노트 08 밸런스 공식을 따른다. 저장은 공개 속성을 그대로 JSON 으로.</summary>
public sealed class Clinic
{
    public const double OfflineCap = 8 * 3600, OfflineRate = 0.5;
    private const int ManagerSpace = 2, FullMouthSpace = 4;
    public const double ClaimWarning = 50;
    private const double ClaimGain = 0.03, ClaimDecayRate = 0.002, ClaimDecayFloor = 0.002, MaxClaimPenalty = 0.3, ClaimHandled = 20, ClaimHandleSeconds = 30, MinClaimHandleCost = 1e6;
    public const double RestSeconds = 30;
    public const int SterilizerFrom = 5, DaySlots = 10, ConsultantFrom = 3, MaxConsultants = 3;
    private const double WorkFatigue = 0.05;
    private static readonly int[] AutoSlots = [5, 3, 2];
    private const int HygienistsPerSterilizer = 3, CalmHygienists = 4;
    private const double StaffFatiguePerExtra = 12, StaffFatigueRate = 0.2, MaxStaffPenalty = 0.3;
    public const double SatisfyRecovery = 8;
    private const double SatisfyBase = 0.02;
    private const double MaxFatiguePenalty = 0.7, FatiguePerClick = 0.3, FatigueRecovery = 1, RestRecovery = 5;
    private const double SubjectStep = 0.1, MaxSubjectRate = 1.3, YearRate = 0.02, MaxSalaryShare = 0.5;
    private const int CandidateCount = 3;
    private const double WreathSeconds = 60, InsuredBonus = 1.3, SeasonWeight = 2;

    /// <summary>주인공 성별. 아직 고르지 않았으면 null — 고르기 화면이 나오고 시간이 가지 않는다.</summary>
    public Gender? Doctor { get; set; }
    public int Face { get; set; }   // 0 = 고르지 않음, 1~9 = 주인공 얼굴
    public double Waiting { get; set; }   // 대기실에서 기다리는 환자
    public double LostPatients { get; set; }   // 오래 기다리다 돌아간 환자 누계
    public List<PlacedEquipment> Placed { get; set; } = [];
    public Dictionary<int, PlacedEquipment> ChairPlaces { get; set; } = [];   // 옮긴 체어만 들어 있다
    public int LayoutSpace { get; set; } = -1;   // 배치를 맞춘 공간. 이전하면 다시 맞춘다
    public int ChairColor { get; set; }      // Dc.Ui.Palette 번호
    public int InteriorColor { get; set; }
    public double ReputationBonus { get; set; }   // 국가고시 칭호
    public Major Major { get; set; }              // 수련의 전공
    public double ReviewLeft { get; set; }        // 좋은 리뷰: 유입 ×1.5
    public double WreathLeft { get; set; }        // 개원 축하 화환: 유입 ×2
    public double ComplaintLeft { get; set; }     // 진상 환자 응대 실패: 평판 −5%
    public double BrokenLeft { get; set; }        // 장비 고장: 체어 1대 정지
    public bool Overclaim { get; set; }           // 보험 청구 모드
    public bool ModelClinic { get; set; }         // 모범 치과: 평판 +10%

    /// <summary>시즌 효과를 정하는 달(1~12). 0 이면 시즌 없음. 호스트가 시작할 때 넣는다.</summary>
    [JsonIgnore] public int Month { get; set; }
    /// <summary>돈은 본점 하나로 모은다. 2호점부터는 본점 지갑을 쓴다.</summary>
    public double Money
    {
        get => Head?.Money ?? _money;
        set { if (Head != null) Head.Money = value; else _money = value; }
    }

    private double _money = 5e7;   // 자기자본 2,000만 + 대출 3,000만
    internal Clinic? Head { get; set; }

    /// <summary>과목별 숙련도. 전 지점이 본점 것을 함께 쓴다.</summary>
    public Mastery Mastery
    {
        get => Head?.Mastery ?? _mastery;
        set => _mastery = value;
    }

    private Mastery _mastery = new();

    public double Claims { get; set; }            // 0~100

    /// <summary>원장의 피로(0~100). 지점과 상관없이 한 사람이다.</summary>
    public double Fatigue
    {
        get => Head?.Fatigue ?? _fatigue;
        set { if (Head != null) Head.Fatigue = value; else _fatigue = value; }
    }

    private double _fatigue;

    /// <summary>남은 휴식 시간(원장). 쉬는 동안 직접 진료를 못 하고 피로가 5배 빨리 풀린다.</summary>
    public double RestLeft
    {
        get => Head?.RestLeft ?? _restLeft;
        set { if (Head != null) Head.RestLeft = value; else _restLeft = value; }
    }

    private double _restLeft;
    public bool Resting => RestLeft > 0;

    public Dictionary<string, int> Schedule { get; set; } = [];   // 하루 진료 칸. 비었으면 환자 수요대로
    public int Consultants { get; set; }          // 상담 맡은 위생사
    public int Sterilizers { get; set; }          // 소독 이모님
    public double StaffFatigue { get; set; }      // 치과위생사 피로 0~100
    public bool AutoClick { get; set; }           // 자동 클릭 (본점에 저장)
    public int AutoClickMs { get; set; } = 200;

    public Site Site { get; set; }
    public List<DentistHire> Hired { get; set; } = [];        // 원장 말고 고용한 의사
    public List<DentistHire> Candidates { get; set; } = [];   // 지금 받은 이력서
    public Specialty Specialty { get; set; }
    public bool AutoManage { get; set; }
    [JsonIgnore] public double FameRate { get; set; } = 1;
    public double Patients { get; set; }
    public double Revenue { get; set; }
    public int Chairs { get; set; } = 1;
    public int Dentists { get; set; } = 1;     // 원장 포함
    public int Hygienists { get; set; }
    public int Assistants { get; set; }   // 간호조무사
    public List<StaffHire> Staff { get; set; } = [];   // 이력서로 뽑은 보조 인력. 자동 경영이 뽑은 사람은 없다
    public List<StaffHire> StaffCandidates { get; set; } = [];
    public int Desks { get; set; }
    public int Skill { get; set; }
    public int SpaceIndex { get; set; }
    public double BuildLeft { get; set; }
    public bool Manager { get; set; }
    public bool Orthodontist { get; set; }
    public bool Scanner { get; set; }
    public bool Ct { get; set; }

    public Space Space => Catalog.Spaces[SpaceIndex];
    public Space? NextSpace => SpaceIndex + 1 < Catalog.Spaces.Length ? Catalog.Spaces[SpaceIndex + 1] : null;
    public bool Building => BuildLeft > 0;

    /// <summary>소아 전용 진료를 하는 치과.</summary>
    private bool TreatsChildren => Specialty == Specialty.Pediatric || Major == Major.Pediatrics;

    public bool Unlocked(Treatment t) => t.Id switch
    {
        "scaling" => Specialty != Specialty.Pediatric,
        "fluoride" or "sealant" or "pedo_resin" => TreatsChildren,
        "pedo_crown" => TreatsChildren && Mastery.Level("pedo_resin") >= Mastery.RequiredLevel,
        "extraction" => Mastery.Level("scaling") >= Mastery.RequiredLevel,
        "wisdom" => Mastery.Level("extraction") >= Mastery.RequiredLevel,
        // 비급여 보철은 스캐너 없이도 본을 떠서 한다. 크라운은 처음부터, 인레이 · 온레이는 숙련으로 연다
        "inlay" => Mastery.Level("resin") >= Mastery.RequiredLevel,
        "onlay" => Mastery.Level("inlay") >= Mastery.RequiredLevel,
        "implant" => Ct,
        "ortho" => Orthodontist || Major == Major.Orthodontics,
        "fullmouth" => SpaceIndex >= FullMouthSpace,
        _ => true,
    };

    public int Slots(Treatment t) => Schedule.GetValueOrDefault(t.Id);
    public int ScheduledSlots => Schedule.Count == 0 ? 0 : Catalog.Treatments.Where(Unlocked).Sum(Slots);

    public bool AddSlot(Treatment t)
    {
        if (ScheduledSlots >= DaySlots) return false;
        Schedule[t.Id] = Slots(t) + 1;
        return true;
    }

    public void RemoveSlot(Treatment t)
    {
        if (Slots(t) > 0) Schedule[t.Id] = Slots(t) - 1;
    }

    public double PriceNow(Treatment t) => PriceOf(t);
    public double SecondsNow(Treatment t) => SecondsOf(t) * Math.Pow(0.88, Skill);

    /// <summary>지금 스탯에서 이 진료가 얼마나 남는 장사인가 — 시간당 수가에서 실수 위험과 피로만큼 뺀다.</summary>
    public double Efficiency(Treatment t) => PriceNow(t) / SecondsNow(t) / (1 + 0.2 * Mastery.Risk(t) * (1 + Fatigue / 100));

    /// <summary>효율 좋은 진료부터 5 · 3 · 2 칸을 준다.</summary>
    public void AutoSchedule()
    {
        Schedule = [];
        var best = Catalog.Treatments.Where(Unlocked).OrderByDescending(Efficiency).ToList();
        for (int i = 0; i < AutoSlots.Length; i++) Schedule[best[Math.Min(i, best.Count - 1)].Id] = Slots(best[Math.Min(i, best.Count - 1)]) + AutoSlots[i];
    }

    /// <summary>진료 하나를 비중대로 뽑는다. 같은 씨앗이면 같은 진료.</summary>
    public Treatment TreatmentAt(int seed)
    {
        var open = Catalog.Treatments.Where(Unlocked).ToList();
        double roll = ((uint)seed * 2654435761u % 1000) / 1000.0 * WeightSum;
        foreach (var t in open)
        {
            roll -= WeightOf(t);
            if (roll < 0) return t;
        }
        return open[^1];
    }

    private double WeightOf(Treatment t) =>
        (ScheduledSlots > 0 ? Slots(t) : t.Weight * AgeFactor(t.Id) * Mastery.FieldReputation(Mastery.FieldOf(t.Id))) * ((t.Id, Month) is ("ortho", 11) or ("scaling", 12) ? SeasonWeight : 1) * ((t.Id, Specialty) switch
        {
            ("ortho", Specialty.Orthodontics) or ("implant", Specialty.Implant) => 3,
            ("scaling", Specialty.Orthodontics) => 0.5,
            _ => 1,
        });
    private double PriceOf(Treatment t) =>
        t.Price * Mastery.ItemRate(i => i.Price) * Mastery.PriceRate(t.Id) * (1 + 0.04 * Mastery.Level(Ability.Technique))
        * (t.Id is "inlay" or "onlay" or "crown" or "implant" or "ortho" or "fullmouth" ? (1 + 0.05 * Mastery.Level(Ability.Consult)) * (1 + 0.06 * Consultants) : 1) * Company.Sites[(int)Site].Price * (Specialty == Specialty.Pediatric ? 0.7 : 1)
        * Math.Min(MaxSubjectRate, 1 + SubjectStep * Hired.Count(h => h.Covers(t)))
        * (1 + StaffCoverStep * Staff.Count(s => s.Covers(t)))
        * (t.Id is "inlay" or "onlay" or "crown" or "implant" or "ortho" or "fullmouth" ? 1 + StaffConsultStep * Staff.Count(s => s.Field == StaffField.Consulting) : 1)
        * (Overclaim && t.Id is "scaling" or "root" ? InsuredBonus : 1)
        * (Specialty == Specialty.Aesthetic && t.Id is "inlay" or "onlay" or "crown" or "implant" or "ortho" or "fullmouth" ? (Site == Site.Gangnam ? 1.3 : 1.15) : 1);
    /// <summary>이 치과에 오는 환자의 연령대 비율(소아 · 청년 · 성인 · 노년).</summary>
    public double[] AgeMix => Patients_Mix();

    private double[] Patients_Mix() => Dc.Core.Patients.Mix(Site, Specialty, Major);

    private double AgeFactor(string id)
    {
        if (Site == Site.NewTown && Specialty == Specialty.None && Major == Major.None) return 1;
        var key = (Site, Specialty, Major, id);
        lock (AgeFactors)
        {
            if (!AgeFactors.TryGetValue(key, out double factor)) AgeFactors[key] = factor = Dc.Core.Patients.Factor(id, Patients_Mix());
            return factor;
        }
    }

    private static readonly Dictionary<(Site, Specialty, Major, string), double> AgeFactors = [];

    private double WeightSum => Catalog.Treatments.Where(Unlocked).Sum(WeightOf);
    public double AveragePrice => Catalog.Treatments.Where(Unlocked).Sum(t => PriceOf(t) * WeightOf(t)) / WeightSum;
    public double AverageSeconds => Catalog.Treatments.Where(Unlocked).Sum(t => SecondsOf(t) * WeightOf(t)) / WeightSum * Math.Pow(0.88, Skill);
    private double SecondsOf(Treatment t) => (t.Id == "implant" && Major == Major.Surgery ? t.Seconds * 0.8 : t.Seconds) * Mastery.TimeRate(t.Id) * Mastery.ItemRate(i => i.Time)
        * (Scanner && t.Id is "inlay" or "onlay" or "crown" ? ScannerTime : 1);

    /// <summary>구강스캐너가 있으면 인상 채득이 빨라 보철 진료 시간이 준다.</summary>
    public const double ScannerTime = 0.75;

    public int ActiveChairs => Math.Max(0, Math.Min(Chairs, Dentists * 3) - (BrokenLeft > 0 ? 1 : 0));
    public int StaffedChairs => Math.Min(ActiveChairs, Hygienists + Assistants);
    private int AssistantChairs => Math.Max(0, StaffedChairs - Hygienists);   // 위생사가 먼저 들어간다

    /// <summary>의사가 못 보는 체어에서 남는 위생사가 혼자 보는 체어. 스케일링 같은 진료만 해서 효율이 낮다.</summary>
    public int HygieneChairs => Math.Min(Math.Max(0, Chairs - ActiveChairs), Math.Max(0, Hygienists - ActiveChairs));   // 단독 진료는 위생사만

    /// <summary>진료 시간 중 위생사가 맡는 몫. 스케일링 · 불소 · 실란트는 전부, 본뜨기(임프레션)가 있는 진료는 일부.</summary>
    public static double HygieneShare(string id) => id switch
    {
        "scaling" or "fluoride" or "sealant" => 1,
        "crown" or "inlay" or "onlay" or "ortho" or "pedo_crown" => 0.3,
        _ => 0,
    };

    /// <summary>위생사 단독 체어의 효율 — 지금 오는 환자의 진료 시간 중 위생사가 할 수 있는 비율.</summary>
    public double HygieneChairRate
    {
        get
        {
            var open = Catalog.Treatments.Where(Unlocked).ToList();
            return open.Sum(t => HygieneShare(t.Id) * t.Seconds * WeightOf(t)) / open.Sum(t => t.Seconds * WeightOf(t));
        }
    }

    /// <summary>위생사가 혼자 하는 일을 하나 고른다. 같은 씨앗이면 같은 일.</summary>
    public string HygieneJob(int seed)
    {
        var jobs = Catalog.Treatments.Where(t => Unlocked(t) && HygieneShare(t.Id) >= 1).Select(t => t.Name).ToList();
        if (Catalog.Treatments.Any(t => Unlocked(t) && HygieneShare(t.Id) is > 0 and < 1)) jobs.Add("임프레션");
        return jobs.Count == 0 ? "구강 위생 교육" : jobs[(int)((uint)seed * 2654435761u % (uint)jobs.Count)];
    }
    public double Capacity => (StaffedChairs - AssistantChairs * (1 - AssistantRate) + HygieneChairs * HygieneChairRate + (ActiveChairs - StaffedChairs) * Math.Min(0.8, 0.5 + 0.03 * Mastery.Level(Ability.Leadership))) / AverageSeconds * ExperienceRate * StaffRate * EquipmentCapacity * StaffYearRate;
    private double EquipmentCapacity => 1 + Placed.Sum(p => Equipment.Find(p.Id)?.Capacity ?? 0);
    private double EquipmentClaim => Placed.Aggregate(1.0, (rate, p) => rate * (Equipment.Find(p.Id)?.Claim ?? 1));
    private const double AssistantRate = 0.9, StaffYearStep = 0.01, StaffCoverStep = 0.05, StaffConsultStep = 0.02, StaffClaimStep = 0.97;
    private double StaffYearRate => 1 + StaffYearStep * Staff.Sum(s => s.Years) / Math.Max(1, Hygienists + Assistants);
    private double StaffClaim => Math.Pow(StaffClaimStep, Staff.Count(s => s.Field == StaffField.Infection));
    private double StaffRate => 1 - MaxStaffPenalty * StaffFatigue / 100;

    /// <summary>위생사가 많아질수록 소독 일이 밀려 피로가 찬다. 이모님 1명이 3명 몫을 던다.</summary>
    private double StaffFatigueGoal => Math.Clamp((Hygienists - HygienistsPerSterilizer * Sterilizers - CalmHygienists) * StaffFatiguePerExtra, 0, 100);
    private double ExperienceRate => 1 + YearRate * Hired.Sum(h => h.Years) / Math.Max(1, Dentists);
    public double TotalSalary => Hired.Sum(h => h.Salary) + Staff.Sum(s => s.Salary);
    public double ReputationRate => (1 + 0.5 * Math.Log10(1 + Patients * (Specialty == Specialty.Pediatric ? 2 : 1) / 100)) * (1 + ReputationBonus) * (ModelClinic ? 1.1 : 1) * (1 - MaxClaimPenalty * Claims / 100) * (ComplaintLeft > 0 ? (Specialty == Specialty.Local ? 0.975 : 0.95) : 1);
    public double Inflow => 0.35 * (1 + 0.4 * Desks) * Space.Inflow * ReputationRate * (Major == Major.Pediatrics ? 1.3 : 1)
                          * (1 + 0.03 * Mastery.Level(Ability.Care)) * Mastery.ItemRate(i => i.Inflow)
                          * (ReviewLeft > 0 ? 1.5 : 1) * (WreathLeft > 0 ? 2 : 1)
                          * (Site == Site.NewTown ? 1 : Company.Sites[(int)Site].Inflow / Company.Sites[(int)Site.NewTown].Inflow)
                          * Specialty switch { Specialty.Implant => 0.9, Specialty.Pediatric => 1.5, _ => 1 };
    public double ServedPerSecond => Math.Min(Capacity, Inflow) * (Building ? 0.5 : 1);

    public const double PatienceSeconds = 45, LostClaim = 0.02;

    /// <summary>대기실 좌석. 넓은 곳으로 이전할수록 늘어난다.</summary>
    public int WaitingSeats => 3 + Space.MaxChairs * 2;

    /// <summary>이만큼 넘게 밀리면 환자가 돌아간다. 좌석과 인내심 중 먼저 닿는 쪽.</summary>
    public double WaitingLimit => Math.Min(WaitingSeats, Capacity * PatienceSeconds);
    public double WaitSeconds => Capacity > 0 ? Waiting / Capacity : 0;

    /// <summary>유입이 처리보다 많으면 대기열이 늘고, 한도를 넘으면 돌아간다. 수입은 처리량으로 이미 정해져 있다.</summary>
    private void TickWaiting(double seconds, double rate)
    {
        Waiting = Math.Max(0, Waiting + (Inflow - Capacity) * seconds * rate);
        double lost = Math.Max(0, Waiting - WaitingLimit);
        if (lost <= 0) return;
        Waiting -= lost;
        LostPatients += lost;
        if (rate >= 1) Claims += lost * LostClaim;   // 기다리다 간 환자는 불만을 남긴다
    }
    public double GrossPerSecond => ServedPerSecond * AveragePrice * (Manager ? 1.2 : 1) * FameRate;

    /// <summary>월급을 뺀 순이익. 월급은 매출의 절반까지만 나가므로 음수가 되지 않는다.</summary>
    public double RevenuePerSecond => Math.Max(GrossPerSecond * (1 - MaxSalaryShare), GrossPerSecond - TotalSalary);
    public double ClickValue => (AveragePrice * 0.5 + RevenuePerSecond * 0.02) * (1 - MaxFatiguePenalty * Fatigue / 100);

    /// <summary>환자 평균 난이도. 어려운 진료가 많을수록 클릭 한 번이 더 지친다.</summary>
    public double AverageDifficulty => Catalog.Treatments.Where(Unlocked).Sum(t => t.Difficulty * WeightOf(t)) / WeightSum;
    private double AverageRisk => Catalog.Treatments.Where(Unlocked).Sum(t => Mastery.Risk(t) * WeightOf(t)) / WeightSum;

    /// <summary>지점 목록에 쓰는 짧은 병목 표시.</summary>
    public string ShortBottleneck =>
        Capacity >= Inflow ? "유입 부족" : Chairs > Dentists * 3 ? "의사 부족" : Hygienists + Assistants < Chairs ? "위생사 부족" : "체어 부족";

    /// <summary>유입을 이루는 요소 — 이름, 지금 값, 올리는 법.</summary>
    public IReadOnlyList<(string Name, string Value, string How)> InflowFactors
    {
        get
        {
            var list = new List<(string, string, string)>
            {
                ("기본 유입", "0.35명/s", ""),
                ("환자층", string.Join(" ", Dc.Core.Patients.Ages.Zip(AgeMix, (a, m) => $"{a} {m * 100:0}%")), "전공 · 특화 · 입지가 정하고, 찾는 진료가 달라집니다"),
                ($"데스크 {Desks}명", $"×{1 + 0.4 * Desks:0.0}", "진료실 탭에서 고용"),
                ($"공간 {Space.Pyeong}평", $"×{Space.Inflow:0.0}", "이전 탭"),
                ("평판", $"×{ReputationRate:0.00}", "환자를 볼수록 상승, 클레임이 깎음"),
                ("진료과 평판", TopField, FieldList),
                ($"환자 관리 Lv.{Mastery.Level(Ability.Care)}", $"×{1 + 0.03 * Mastery.Level(Ability.Care):0.00}", "스킬 탭"),
            };
            if (Site != Site.NewTown) list.Add(($"입지 {Company.Sites[(int)Site].Name}", $"×{Company.Sites[(int)Site].Inflow / Company.Sites[(int)Site.NewTown].Inflow:0.00}", ""));
            if (ReviewLeft > 0) list.Add(("좋은 리뷰", "×1.5", "이벤트"));
            if (WreathLeft > 0) list.Add(("개원 축하 화환", "×2", "이전 직후"));
            return list;
        }
    }

    private string TopField
    {
        get
        {
            var best = Enum.GetValues<Field>().MaxBy(Mastery.FieldReputation);
            return $"{Mastery.FieldName(best)} ×{Mastery.FieldReputation(best):0.00}";
        }
    }

    /// <summary>진료과 평판이 높을수록 그 과 환자가 더 온다.</summary>
    private string FieldList => string.Join(" · ", Enum.GetValues<Field>().Select(f => $"{Mastery.FieldName(f)} {Mastery.FieldReputation(f):0.0}"));

    /// <summary>처리 용량을 이루는 요소 — 이름, 지금 값, 올리는 법.</summary>
    public IReadOnlyList<(string Name, string Value, string How)> CapacityFactors =>
    [
        ($"가동 체어 {ActiveChairs}/{Chairs}대", $"의사 {Dentists}명 × 3대", "체어 · 페이닥터"),
        (Assistants > 0 ? $"위생사 {Hygienists}명 · 조무사 {Assistants}명" : $"위생사 {Hygienists}명", ActiveChairs <= StaffedChairs ? "모든 체어 배치" : $"위생사 없는 체어 {ActiveChairs - StaffedChairs}대 효율 {Math.Min(0.8, 0.5 + 0.03 * Mastery.Level(Ability.Leadership)) * 100:0}%", "진료실 탭에서 고용"),
        ($"위생사 단독 체어 {HygieneChairs}대", $"효율 {HygieneChairRate * 100:0}%", "의사 없는 체어 + 남는 위생사"),
        ("평균 진료 시간", $"{AverageSeconds:0.0}초", "손 빠르기, 과목 숙련도"),
        ($"페이닥터 연차", $"×{ExperienceRate:0.00}", "연차 높은 지원자"),
        ($"위생사 피로 {StaffFatigue:0}%", $"×{StaffRate:0.00}", "소독 이모님 고용"),
    ];

    public string Bottleneck
    {
        get
        {
            string verdict = Capacity < Inflow ? "처리 부족" : Capacity > Inflow ? "유입 부족" : "균형";
            return $"유입 {Inflow:0.00}명/s · 처리 {Capacity:0.00}명/s → {verdict}";
        }
    }

    public void Tick(double seconds, double rate = 1)
    {
        double served = ServedPerSecond * seconds * rate;
        // 자리를 비운 동안(rate < 1)에는 클레임이 쌓이지 않는다
        if (rate >= 1)
            Claims += ServedPerSecond * seconds * ClaimGain * AverageRisk * (1 + 0.1 * Skill) * (1 + Fatigue / 100)
                      * Math.Pow(0.88, Mastery.Level(Ability.Diagnosis)) * Mastery.ItemRate(i => i.Claim) * EquipmentClaim * StaffClaim;
        TickWaiting(seconds, rate);
        // 쌓인 만큼 조금씩 풀린다 — 진료를 하는 동안은 0 으로 떨어지지 않고 어느 선에서 머문다
        Claims = Math.Clamp(Claims - seconds * (ClaimDecayRate * Claims + ClaimDecayFloor) * (1 + 0.25 * Desks + 0.5 * Consultants) * (1 + 0.3 * Mastery.Level(Ability.Care)), 0, 100);
        double earned = RevenuePerSecond * seconds * rate;
        Money += earned;
        Revenue += earned;
        Patients += served;
        StaffFatigue += Math.Clamp(StaffFatigueGoal - StaffFatigue, -StaffFatigueRate * seconds, StaffFatigueRate * seconds);
        if (rate >= 1) Fatigue = Math.Min(100, Fatigue + served * AverageDifficulty * WorkFatigue * FatigueGainRate);
        if (Head == null)
        {
            Fatigue = Math.Max(0, Fatigue - seconds * FatigueRecovery * (Resting ? RestRecovery : 1) * (1 + 0.2 * Mastery.Level(Ability.Stamina)));
            RestLeft = Math.Max(0, RestLeft - seconds);
        }
        double weights = WeightSum;
        foreach (var t in Catalog.Treatments.Where(Unlocked)) 
        {
            Mastery.AddPatients(t.Id, served * WeightOf(t) / weights);
            Mastery.AddFieldPatients(t.Id, served * WeightOf(t) / weights);
        }
        if (Building) BuildLeft = Math.Max(0, BuildLeft - seconds);
        ReviewLeft = Math.Max(0, ReviewLeft - seconds);
        WreathLeft = Math.Max(0, WreathLeft - seconds);
        ComplaintLeft = Math.Max(0, ComplaintLeft - seconds);
        BrokenLeft = Math.Max(0, BrokenLeft - seconds);
    }

    /// <summary>클릭은 돈만 번다 — 기획 시뮬레이션과 같이 누적 환자(평판)에는 들어가지 않는다.</summary>
    public double Click()
    {
        if (Resting) return 0;
        double value = ClickValue;
        Fatigue = Math.Min(100, Fatigue + FatiguePerClick * AverageDifficulty * FatigueGainRate);
        Money += value;
        Revenue += value;
        return value;
    }

    /// <summary>자리를 비운 시간만큼 번다. 최대 8시간, 효율 50%. 시계가 거꾸로면 0.</summary>
    public double ApplyOffline(double seconds)
    {
        double left = Math.Min(Math.Max(0, seconds), OfflineCap), before = Money;
        double step = Math.Max(10, left / 2000);
        for (; left > 0; left -= step) Tick(Math.Min(step, left), OfflineRate);
        return Money - before;
    }

    public double Cost(Purchase p) => p switch
    {
        Purchase.Chair => 3e7 * Math.Pow(1.22, Chairs - 1),
        Purchase.Dentist => 1.2e8 * Math.Pow(1.55, Dentists - 1),
        Purchase.Bundle => Cost(Purchase.Chair) + Cost(Purchase.Dentist),
        Purchase.Hygienist => 8e6 * Math.Pow(1.25, Hygienists + Assistants),
        Purchase.Desk => 2e7 * Math.Pow(1.45, Desks),
        Purchase.Manager => 5e8,
        Purchase.Orthodontist => Specialty == Specialty.Orthodontics ? 2.5e9 : 5e9,
        Purchase.Scanner => 1e8,
        Purchase.Ct => Specialty == Specialty.Implant ? 0 : 6e8,
        Purchase.Skill => 2e7 * Math.Pow(2.4, Skill),
        Purchase.Move => NextSpace?.MoveCost ?? double.PositiveInfinity,
        Purchase.AutoManage => 1e10,
        Purchase.Sterilizer => 1.5e7 * Math.Pow(1.4, Sterilizers),
        Purchase.Consultant => 2e7 * Math.Pow(1.8, Consultants),
        _ => throw new ArgumentOutOfRangeException(nameof(p)),
    };

    /// <summary>돈 말고 살 수 없는 까닭. 살 수 있으면 null.</summary>
    public string? Blocked(Purchase p) => p switch
    {
        Purchase.Chair or Purchase.Bundle when Chairs >= Space.MaxChairs => "공간이 꽉 찼습니다 — 이전 필요",
        Purchase.Hygienist when Hygienists + Assistants >= Chairs => "체어 수만큼 고용했습니다",
        Purchase.Manager when Manager => "고용 완료",
        Purchase.Manager when SpaceIndex < ManagerSpace => "50평부터 고용할 수 있습니다",
        Purchase.Orthodontist when Orthodontist => "고용 완료",
        Purchase.Orthodontist when Major == Major.Orthodontics => "전공으로 해금됨",
        Purchase.Scanner when Scanner => "구매 완료",
        Purchase.Ct when Ct => "구매 완료",
        Purchase.Consultant when Hygienists < ConsultantFrom => $"위생사 {ConsultantFrom}명부터 맡길 수 있습니다",
        Purchase.Consultant when Consultants >= MaxConsultants => $"{MaxConsultants}명까지 맡길 수 있습니다",
        Purchase.Sterilizer when Hygienists < SterilizerFrom => $"위생사 {SterilizerFrom}명부터 필요합니다",
        Purchase.AutoManage when AutoManage => "구매 완료",
        Purchase.AutoManage when !Manager => "실장이 있어야 위임할 수 있습니다",
        Purchase.Move when NextSpace == null => "마지막 공간입니다",
        Purchase.Move when Building => "공사 중입니다",
        Purchase.Move when Chairs < Space.MaxChairs => "체어를 꽉 채워야 이전할 수 있습니다",
        _ => null,
    };

    public bool Buy(Purchase p)
    {
        if (Blocked(p) != null || Money < Cost(p)) return false;
        Money -= Cost(p);
        Apply(p);
        if (p == Purchase.Move) WreathLeft = WreathSeconds;
        return true;
    }

    /// <summary>이걸 사면 늘어나는 초당 매출. 공사로 잠깐 줄어드는 몫은 뺀 값.</summary>
    public double Gain(Purchase p)
    {
        var before = Copy();
        before.BuildLeft = 0;
        var after = before.Copy();
        after.Apply(p);
        after.BuildLeft = 0;
        return after.RevenuePerSecond - before.RevenuePerSecond;
    }

    private double FatigueGainRate => Math.Pow(0.92, Mastery.Level(Ability.Stamina)) * Mastery.ItemRate(i => i.Fatigue);

    /// <summary>아이템을 사서 바로 장착한다.</summary>
    /// <summary>장비를 사서 진료실 아래쪽에 놓는다. 겹치지 않게 조금씩 비껴 놓는다.</summary>
    public bool BuyEquipment(Equipment equipment)
    {
        if (Money < equipment.Cost || Placed.Any(p => p.Id == equipment.Id)) return false;
        Money -= equipment.Cost;
        AddPlaced(equipment.Id);
        return true;
    }

    public bool SellEquipment(PlacedEquipment placed, double rate)
    {
        if (Equipment.Find(placed.Id) is not { } equipment || !Placed.Remove(placed)) return false;
        Money += equipment.Cost * rate;
        return true;
    }

    /// <summary>장비 탭에서 산 구강스캐너 · CT 도 진료실에 보이게 한다.</summary>
    public void PlaceOwnedEquipment()
    {
        if (Scanner && Placed.All(p => p.Id != "scanner")) AddPlaced("scanner");
        if (Ct && Placed.All(p => p.Id != "ct")) AddPlaced("ct");
    }

    /// <summary>자리는 화면이 빈 바닥을 찾아 정한다.</summary>
    private void AddPlaced(string id) => Placed.Add(new PlacedEquipment { Id = id, Y = PlacedEquipment.Unplaced });

    public bool BuyItem(Item item)
    {
        if (Mastery.Owns(item) || Money < item.Cost) return false;
        Money -= item.Cost;
        Mastery.Owned.Add(item.Id);
        Mastery.PutOn(item);
        return true;
    }

    /// <summary>환자 한 명 또는 직접 진료 한 번이 만족스러울 확률. 환자 관리가 높을수록 잦다.</summary>
    public double SatisfyChance => SatisfyBase * (1 + 0.1 * Mastery.Level(Ability.Care));

    /// <summary>주어진 환자 수만큼 만족 판정을 한다. 만족스러운 진료가 나오면 피로가 풀린다. 피로가 없으면 판정하지 않는다.</summary>
    public bool TrySatisfy(Random random, double patients)
    {
        if (Fatigue <= 0 || random.NextDouble() >= patients * SatisfyChance) return false;
        Fatigue = Math.Max(0, Fatigue - SatisfyRecovery);
        return true;
    }

    /// <summary>원장이 쉰다.</summary>
    public bool Rest()
    {
        if (Resting || Fatigue <= 0) return false;
        RestLeft = RestSeconds;
        return true;
    }

    public bool BuyAbility(Ability ability)
    {
        if (Money < Mastery.AbilityCost(ability)) return false;
        Money -= Mastery.AbilityCost(ability);
        Mastery.Raise(ability);
        return true;
    }

    public double ClaimHandleCost => Math.Max(MinClaimHandleCost, RevenuePerSecond * ClaimHandleSeconds);

    /// <summary>원장이 직접 클레임을 응대한다.</summary>
    public bool HandleClaims()
    {
        if (Claims <= 0 || Money < ClaimHandleCost) return false;
        Money -= ClaimHandleCost;
        Claims = Math.Max(0, Claims - ClaimHandled);
        return true;
    }

    /// <summary>세미나를 듣는다.</summary>
    public bool Study(Treatment t)
    {
        if (Mastery.StudyBlocked(t) != null || Money < Mastery.SeminarCost(t)) return false;
        Money -= Mastery.SeminarCost(t);
        Mastery.Study(t);
        return true;
    }

    /// <summary>이력서를 새로 받는다.</summary>
    public void RefreshCandidates(Random random) =>
        Candidates = Enumerable.Range(0, CandidateCount).Select(_ => DentistHire.Random(random)).ToList();

    public double FeeOf(DentistHire hire) => Cost(Purchase.Dentist) * hire.Fee;

    public bool CanAffordSalary(DentistHire hire) => TotalSalary + hire.Salary <= GrossPerSecond * MaxSalaryShare;

    /// <summary>지원자를 고용하면 늘어나는 순이익(고용 뒤 체어가 더 돌아가는 몫 포함).</summary>
    public double GainOf(DentistHire hire)
    {
        var before = Copy();
        before.BuildLeft = 0;
        var after = before.Copy();
        after.AddDentist(hire);
        return after.RevenuePerSecond - before.RevenuePerSecond;
    }

    /// <summary>위생사 2장, 조무사 1장의 새 이력서를 받는다.</summary>
    public void RefreshStaffCandidates(Random random) =>
        StaffCandidates = [StaffHire.Random(random, false), StaffHire.Random(random, false), StaffHire.Random(random, true)];

    private const double AssistantFee = 0.6, StaffFeePerYear = 0.05;

    public double FeeOf(StaffHire hire) => Cost(Purchase.Hygienist) * (hire.Assistant ? AssistantFee : 1) * (1 + StaffFeePerYear * hire.Years);

    public bool HireStaff(StaffHire hire)
    {
        if (Blocked(Purchase.Hygienist) != null || Money < FeeOf(hire)) return false;
        Money -= FeeOf(hire);
        if (hire.Assistant) Assistants++;
        else Hygienists++;
        Staff = [.. Staff, hire];
        StaffCandidates = [.. StaffCandidates.Where(c => c != hire)];
        return true;
    }

    /// <summary>위생사가 그만두면 명부에서도 뺀다.</summary>
    public void LoseHygienist()
    {
        Hygienists--;
        if (Staff.FindLastIndex(s => !s.Assistant) is >= 0 and var index) Staff.RemoveAt(index);
    }

    public bool Hire(DentistHire hire)
    {
        if (Money < FeeOf(hire)) return false;
        Money -= FeeOf(hire);
        AddDentist(hire);
        Candidates = Candidates.Where(c => c != hire).ToList();
        return true;
    }

    // 미리보기 사본과 목록을 나누지 않도록 새 목록을 만든다
    private void AddDentist(DentistHire hire)
    {
        Dentists++;
        Hired = [.. Hired, hire];
    }

    private void Apply(Purchase p)
    {
        switch (p)
        {
            case Purchase.Chair: Chairs++; break;
            case Purchase.Dentist: AddDentist(DentistHire.RookieNumber(Hired.Count)); break;
            case Purchase.Bundle: Chairs++; AddDentist(DentistHire.RookieNumber(Hired.Count)); break;
            case Purchase.Hygienist: Hygienists++; break;
            case Purchase.Desk: Desks++; break;
            case Purchase.Manager: Manager = true; break;
            case Purchase.Orthodontist: Orthodontist = true; break;
            case Purchase.Scanner: Scanner = true; break;
            case Purchase.Ct: Ct = true; break;
            case Purchase.Skill: Skill++; break;
            case Purchase.AutoManage: AutoManage = true; break;
            case Purchase.Consultant: Consultants++; break;
            case Purchase.Sterilizer: Sterilizers++; StaffFatigue = Math.Min(StaffFatigue, StaffFatigueGoal); break;
            case Purchase.Move: SpaceIndex++; BuildLeft = Space.BuildSeconds; break;
        }
    }

    private Clinic Copy() => (Clinic)MemberwiseClone();
}
