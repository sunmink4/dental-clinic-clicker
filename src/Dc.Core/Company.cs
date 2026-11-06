using System.Text.Json.Serialization;

namespace Dc.Core;

public enum Site { NewTown, Province, Seoul, Gangnam }

public enum Specialty { None, Orthodontics, Implant, Pediatric, Aesthetic, Local }

public sealed record SiteInfo(string Name, double Inflow, double Price, double Cost);

/// <summary>모든 지점. 돈은 본점(Branches[0])에 모으고, 체어 · 직원 · 장비는 지점마다 따로다. 기획 노트 10 지점 확장.</summary>
public sealed class Company
{
    public const int UnlockSpace = 2;
    public const double BaseOpenCost = 1e9, OpenCostGrowth = 4, ChangeSpecialtyCost = 1e9, FameStep = 0.02;
    private const double AutoBuySeconds = 5;

    public static readonly SiteInfo[] Sites =
    [
        new("신도시", 1.2, 1.0, 1.0),
        new("지방 소도시", 1.5, 0.7, 0.5),
        new("서울 역세권", 1.0, 1.2, 2.0),
        new("강남", 0.8, 1.6, 4.0),
    ];

    public static readonly (Specialty Kind, string Name, string Note)[] Specialties =
    [
        (Specialty.None, "특화 없음", "효과 없음"),
        (Specialty.Orthodontics, "교정 전문", "교정 ×3, 전문의 −50%, 스케일링 ×0.5"),
        (Specialty.Implant, "임플란트 전문", "임플란트 ×3, CT 무료, 유입 −10%"),
        (Specialty.Pediatric, "소아치과", "불소 · 실란트 진료, 유입 +50%, 평판 2배, 객단가 −30%"),
        (Specialty.Aesthetic, "심미 · 미백", "비급여 +15%, 강남은 +30%"),
        (Specialty.Local, "동네 치과", "이벤트 페널티 −50%"),
    ];

    public List<Clinic> Branches { get; set; } = [];
    public int Selected { get; set; }
    public int Fame { get; set; }

    private double _sinceAutoBuy;

    [JsonConstructor]
    public Company() { }

    public Company(Clinic head)
    {
        Branches.Add(head);
    }

    public Clinic Head => Branches[0];
    public Clinic Current => Branches[Math.Clamp(Selected, 0, Branches.Count - 1)];
    public bool Unlocked => Head.SpaceIndex >= UnlockSpace;
    public double TotalRevenue => Branches.Sum(b => b.Revenue);
    public double RevenuePerSecond => Branches.Sum(b => b.RevenuePerSecond);
    public int PendingFame => (int)Math.Floor(Math.Sqrt(TotalRevenue / 1e7));
    public double FameRate => 1 + FameStep * Fame;

    /// <summary>저장에서 읽은 뒤 지점끼리 다시 잇는다.</summary>
    public void Link(int month)
    {
        Head.Mastery.Major = Head.Major;   // 전공을 넣기 전의 저장도 전공 효과를 받는다
        foreach (var branch in Branches)
        {
            branch.Head = branch == Head ? null : Head;
            branch.FameRate = FameRate;
            branch.Month = month;
        }
    }

    public double OpenCost(Site site) => BaseOpenCost * Math.Pow(OpenCostGrowth, Branches.Count - 1) * Sites[(int)site].Cost;

    public bool CanOpen(Site site) => Unlocked && Head.Money >= OpenCost(site);

    /// <summary>새 지점을 연다. 이때 명성이 확정되어 전 지점에 곱해진다.</summary>
    public bool Open(Site site, Specialty specialty)
    {
        if (!CanOpen(site)) return false;
        Head.Money -= OpenCost(site);
        Fame = Math.Max(Fame, PendingFame);
        Branches.Add(new Clinic { Site = site, Specialty = specialty, Doctor = Head.Doctor, Face = Head.Face, ChairColor = Head.ChairColor, InteriorColor = Head.InteriorColor });
        Link(Head.Month);
        Selected = Branches.Count - 1;
        return true;
    }

    public bool ChangeSpecialty(Clinic branch, Specialty specialty)
    {
        if (branch.Specialty == specialty || Head.Money < ChangeSpecialtyCost) return false;
        Head.Money -= ChangeSpecialtyCost;
        branch.Specialty = specialty;
        return true;
    }

    public void Tick(double seconds)
    {
        foreach (var branch in Branches) branch.Tick(seconds);
        _sinceAutoBuy += seconds;
        if (_sinceAutoBuy < AutoBuySeconds) return;
        _sinceAutoBuy = 0;
        foreach (var branch in Branches.Where(b => b.AutoManage)) AutoBuy(branch);
    }

    /// <summary>자동 경영 — 이전할 수 있으면 이전하고, 아니면 회수 시간이 가장 짧은 것을 산다.</summary>
    public static void AutoBuy(Clinic branch)
    {
        if (branch.Blocked(Purchase.Move) == null && branch.Buy(Purchase.Move)) return;
        Purchase[] items = [Purchase.Bundle, Purchase.Chair, Purchase.Dentist, Purchase.Hygienist, Purchase.Desk, Purchase.Sterilizer, Purchase.Consultant, Purchase.Skill, Purchase.Scanner, Purchase.Ct];
        var options = items.Where(p => branch.Blocked(p) == null && branch.Gain(p) > 0).ToList();
        if (options.Count > 0) branch.Buy(options.MinBy(p => branch.Cost(p) / branch.Gain(p)));
    }

    public double ApplyOffline(double seconds) => Branches.Sum(b => b.ApplyOffline(seconds));
}
