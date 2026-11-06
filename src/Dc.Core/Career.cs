using System.Numerics;

namespace Dc.Core;

/// <summary>Exam 은 국가고시 연타가 있던 옛 저장용. 지금은 바로 Choice 로 넘어간다.</summary>
public enum CareerStage { Student, Exam, Choice, Resident, PayDoctor, Done }

public enum Major { None, Orthodontics, Surgery, Pediatrics, Prosthodontics, Conservative }

public enum Trait { None, Quick, Careful, Warm, Tough }

public enum PayCase { Gum, Caries, Root }

public enum TrainingItem { Loupe, ModelTeeth, SeniorNotes, AllNighter, Incentive, NightShift, Seminar }

/// <summary>개원 전 수련 단계. 기획 노트 11 수련 단계의 초안 수치(시뮬레이션 미검증).</summary>
public sealed class Career
{
    public const double ExamGoalXp = 300, PassBonus = 0.1, OwnCapital = 2e7, Loan = 3e7;
    public const int ScalingGoal = 50, RootGoal = 20;
    public const int SpotCount = 6, CasesPerStep = 3;
    public const double CallSeconds = 6, CallInterval = 45, CallReward = 15, MaxSkillRate = 1.5;
    private const double CaseBonus = 0.5, SkillStep = 0.05;
    private const double BaseSalary = 15_000, BaseIncentive = 15_000, OfflineCap = 8 * 3600, OfflineRate = 0.5;

    public CareerStage Stage { get; set; } = CareerStage.Student;
    public double Xp { get; set; }
    public double TotalXp { get; set; }
    public double Money { get; set; }
    public double ReputationBonus { get; set; }
    public string Title { get; set; } = "";
    public Major Major { get; set; }
    public Trait Trait { get; set; }
    public int ScalingDone { get; set; }
    public int RootDone { get; set; }
    public List<TrainingItem> Owned { get; set; } = [];
    public PayCase Case { get; set; }
    public int CaseLeft { get; set; } = StepsOf(PayCase.Gum);
    public int Spots { get; set; }                 // 충치 점이 남은 자리(비트)
    public int[] CasesDone { get; set; } = new int[3];
    public double CallLeft { get; set; }           // 옆 체어 마취 호출
    public double NextCall { get; set; } = CallInterval;

    public bool Done => Stage == CareerStage.Done;

    public static string TraitName(Trait trait) => trait switch
    {
        Trait.Quick => "손이 빠른 사람",
        Trait.Careful => "꼼꼼한 사람",
        Trait.Warm => "다정한 사람",
        Trait.Tough => "체력이 강한 사람",
        _ => "",
    };

    /// <summary>손재주 · 꼼꼼함 · 친화력 · 체력. 2 를 넘는 만큼 개원 때 기술 · 진단 · 환자 관리 · 체력 관리 레벨이 된다.</summary>
    public static (int Hand, int Focus, int Care, int Body) StatsOf(Trait trait) => trait switch
    {
        Trait.Quick => (5, 2, 2, 3),
        Trait.Careful => (2, 5, 3, 2),
        Trait.Warm => (2, 3, 5, 2),
        Trait.Tough => (3, 2, 2, 5),
        _ => (2, 2, 2, 2),
    };

    public static string[] TraitFavors(Trait trait) => trait switch
    {
        Trait.Quick => ["resin", "inlay", "crown"],
        Trait.Careful => ["scaling", "root"],
        Trait.Warm => ["scaling", "fluoride", "sealant", "pedo_resin"],
        Trait.Tough => ["extraction", "wisdom", "implant", "fullmouth"],
        _ => [],
    };

    public static string[] MajorFavors(Major major) => major switch
    {
        Major.Orthodontics => ["ortho"],
        Major.Surgery => ["extraction", "wisdom", "implant"],
        Major.Pediatrics => ["fluoride", "sealant", "pedo_resin", "pedo_crown"],
        Major.Prosthodontics => ["inlay", "onlay", "crown", "fullmouth"],
        Major.Conservative => ["resin", "root"],
        _ => [],
    };

    /// <summary>전공 과목은 수련에서 본 환자만큼 숙련도를 안고 시작한다(Lv.3).</summary>
    private const double MajorPatients = 450;

    private void ApplyTalent(Clinic clinic)
    {
        var mastery = clinic.Mastery;
        mastery.Major = Major;
        var stats = StatsOf(Trait);
        mastery.Abilities[Ability.Technique] = stats.Hand - 2;
        mastery.Abilities[Ability.Diagnosis] = stats.Focus - 2;
        mastery.Abilities[Ability.Care] = stats.Care - 2;
        mastery.Abilities[Ability.Stamina] = stats.Body - 2;
        mastery.Favored = [.. TraitFavors(Trait).Concat(MajorFavors(Major)).Distinct()];
        foreach (string id in MajorFavors(Major)) mastery.Patients[id] = MajorPatients;
    }

    public static string MajorName(Major major) => major switch
    {
        Major.Orthodontics => "교정과",
        Major.Surgery => "구강악안면외과",
        Major.Pediatrics => "소아치과",
        Major.Prosthodontics => "보철과",
        Major.Conservative => "보존과",
        _ => "없음",
    };

    /// <summary>주인공 정보 — 이름, 값, 설명.</summary>
    public IReadOnlyList<(string Name, string Value, string How)> Profile(Mastery mastery)
    {
        var stats = StatsOf(Trait);
        string good = string.Join(" · ", mastery.Favored.Select(id => Catalog.Treatments.First(t => t.Id == id).Name));
        return
        [
            ("성향", Trait == Trait.None ? "없음" : TraitName(Trait), Title.Length > 0 ? $"국가고시 {Title}" : ""),
            ("전공", MajorName(Major), "전공 과목은 Lv.3으로 시작"),
            ("손재주", stats.Hand.ToString(), "2를 넘는 만큼 기술 레벨로 시작"),
            ("꼼꼼함", stats.Focus.ToString(), "2를 넘는 만큼 정확한 진단 레벨로 시작"),
            ("친화력", stats.Care.ToString(), "2를 넘는 만큼 환자 관리 레벨로 시작"),
            ("체력", stats.Body.ToString(), "2를 넘는 만큼 체력 관리 레벨로 시작"),
            ("잘하는 과목", mastery.Favored.Count.ToString() + "개", good.Length > 0 ? good : "환자 경험 1.5배"),
        ];
    }

    /// <summary>전공에 맞는 수련 과제 이름 두 가지.</summary>
    public (string First, string Second) TaskNames => Major switch
    {
        Major.Orthodontics => ("브라켓 부착", "와이어 교체"),
        Major.Surgery => ("발치", "사랑니 발치"),
        Major.Pediatrics => ("불소 도포", "실란트"),
        Major.Prosthodontics => ("프렙", "임시치아"),
        Major.Conservative => ("와동 형성", "근관 충전"),
        _ => ("스케일링", "신경치료"),
    };
    public bool Has(TrainingItem item) => Owned.Contains(item);

    public double XpPerTap => (Has(TrainingItem.Loupe) ? 2 : 1) * (Has(TrainingItem.AllNighter) ? 2 : 1);
    public double XpPerSecond => !Has(TrainingItem.ModelTeeth) ? 0 : Has(TrainingItem.SeniorNotes) ? 1 : 0.5;
    public double Salary => BaseSalary * (Has(TrainingItem.NightShift) ? 1.5 : 1);
    public double Incentive => BaseIncentive * (Has(TrainingItem.Incentive) ? 2 : 1) * SkillRate;

    /// <summary>세 과목을 고르게 해야 는다. 가장 적게 한 과목 기준.</summary>
    public double SkillRate => Math.Min(MaxSkillRate, 1 + SkillStep * (CasesDone.Min() / CasesPerStep));

    public static string CaseName(PayCase kind) => kind switch
    {
        PayCase.Gum => "잇몸 치료",
        PayCase.Caries => "충치 치료",
        _ => "신경치료",
    };

    private static int StepsOf(PayCase kind) => kind switch { PayCase.Gum => 12, PayCase.Caries => SpotCount, _ => 20 };

    public static double Cost(TrainingItem item) => item switch
    {
        TrainingItem.Loupe => 20,
        TrainingItem.ModelTeeth => 40,
        TrainingItem.SeniorNotes => 80,
        TrainingItem.AllNighter => 150,
        TrainingItem.Incentive => 1e6,
        TrainingItem.NightShift => 3e6,
        TrainingItem.Seminar => 5e6,
        _ => throw new ArgumentOutOfRangeException(nameof(item)),
    };

    private static bool PaysWithXp(TrainingItem item) => item <= TrainingItem.AllNighter;

    public bool CanBuy(TrainingItem item) =>
        !Has(item) && (PaysWithXp(item)
            ? Stage == CareerStage.Student && Xp >= Cost(item)
            : Stage == CareerStage.PayDoctor && Money >= Cost(item));

    public bool Buy(TrainingItem item)
    {
        if (!CanBuy(item)) return false;
        if (PaysWithXp(item)) Xp -= Cost(item);
        else Money -= Cost(item);
        Owned.Add(item);
        return true;
    }

    public void Tick(double seconds)
    {
        switch (Stage)
        {
            case CareerStage.Student:
                AddXp(XpPerSecond * seconds);
                break;
            case CareerStage.Exam:
                PassExam();
                break;
            case CareerStage.PayDoctor:
                Money += Salary * seconds;
                if (CallLeft > 0) CallLeft = Math.Max(0, CallLeft - seconds);
                else if ((NextCall -= seconds) <= 0) { CallLeft = CallSeconds; NextCall = CallInterval; }
                break;
        }
    }

    /// <summary>자리를 비운 동안. 최대 8시간, 효율 50%.</summary>
    public void ApplyOffline(double seconds) => Tick(Math.Min(Math.Max(0, seconds), OfflineCap) * OfflineRate);

    /// <summary>화면을 한 번 눌렀을 때. 번 값을 돌려준다(없으면 0).</summary>
    public double Tap()
    {
        switch (Stage)
        {
            case CareerStage.Student:
                AddXp(XpPerTap);
                return XpPerTap;
            case CareerStage.Resident:
                if (ScalingDone < ScalingGoal) ScalingDone++;
                else RootDone++;
                if (RootDone >= RootGoal) Stage = CareerStage.PayDoctor;
                return 0;
            case CareerStage.PayDoctor:
                return TreatTap();
            default:
                return 0;
        }
    }

    private double TreatTap()
    {
        if (CallLeft > 0)
        {
            CallLeft = 0;
            double reward = Incentive * CallReward;
            Money += reward;
            return reward;
        }
        if (Case == PayCase.Caries)
        {
            if (Spots == 0) Spots = (1 << SpotCount) - 1;
            Spots &= Spots - 1;
            CaseLeft = BitOperations.PopCount((uint)Spots);
        }
        else CaseLeft--;
        return FinishStep();
    }

    /// <summary>충치 점 하나를 직접 눌렀을 때. 호출 중이 아니고 그 자리에 점이 있어야 한다.</summary>
    public double TapSpot(int index)
    {
        if (Stage != CareerStage.PayDoctor || Case != PayCase.Caries || CallLeft > 0 || (Spots >> index & 1) == 0) return 0;
        Spots &= ~(1 << index);
        CaseLeft = BitOperations.PopCount((uint)Spots);
        return FinishStep();
    }

    private double FinishStep()
    {
        double earned = Incentive;
        if (CaseLeft <= 0)
        {
            earned += Incentive * StepsOf(Case) * CaseBonus;
            CasesDone[(int)Case]++;
            Case = (PayCase)(((int)Case + 1) % 3);
            CaseLeft = StepsOf(Case);
            Spots = Case == PayCase.Caries ? (1 << SpotCount) - 1 : 0;
        }
        Money += earned;
        return earned;
    }

    /// <summary>국가고시 뒤 — 전공을 고르면 수련, <see cref="Major.None"/> 이면 바로 페이닥터.</summary>
    public void Choose(Major major)
    {
        if (Stage != CareerStage.Choice) return;
        Major = major;
        Stage = major == Major.None ? CareerStage.PayDoctor : CareerStage.Resident;
    }

    public bool CanOpen => Stage == CareerStage.PayDoctor && Money >= OwnCapital;

    /// <summary>개원. 모은 돈에 대출을 더해 개원 단계로 넘어간다.</summary>
    public bool Open(Clinic clinic)
    {
        if (!CanOpen) return false;
        clinic.Money = Money + Loan;
        clinic.Skill = Has(TrainingItem.Seminar) ? 1 : 0;
        clinic.ReputationBonus = ReputationBonus;
        clinic.Major = Major;
        ApplyTalent(clinic);
        Stage = CareerStage.Done;
        return true;
    }

    private void AddXp(double amount)
    {
        Xp += amount;
        TotalXp += amount;
        if (TotalXp >= ExamGoalXp) PassExam();
    }

    /// <summary>누적 XP 를 채우면 국가고시는 그냥 합격이다. 연타 시험은 없다.</summary>
    private void PassExam()
    {
        (Title, ReputationBonus) = ("합격", PassBonus);
        Stage = CareerStage.Choice;
    }
}
