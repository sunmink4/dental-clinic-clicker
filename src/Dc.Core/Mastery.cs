namespace Dc.Core;

public enum Field { Periodontics, Conservative, Prosthodontics, Surgery, Orthodontics, Pediatrics }

public enum Ability { Diagnosis, Technique, Care, Consult, Leadership, Stamina }

/// <summary>진료 과목별 숙련도. 세미나와 환자 진료로 오르고, 전 지점이 함께 쓴다.</summary>
public sealed class Mastery
{
    public const int MaxLevel = 20, MajorMaxLevel = 40, MaxSeminars = 5, MajorSeminars = 8, RequiredLevel = 3;
    private const double MajorPricePerLevel = 0.05;
    private const double PricePerLevel = 0.03, TimePerLevel = 0.98, PatientsPerStep = 50;
    private const double FavoredRate = 1.5;
    private const double BaseSeminar = 1e7, SeminarGrowth = 2;

    public Dictionary<string, double> Patients { get; set; } = [];
    public Dictionary<string, int> Seminars { get; set; } = [];
    public Dictionary<Ability, int> Abilities { get; set; } = [];
    public Major Major { get; set; }                  // 주인공 전공
    public Dictionary<Field, double> FieldPatients { get; set; } = [];   // 진료과별로 본 환자
    public List<string> Owned { get; set; } = [];     // 산 아이템
    public Dictionary<ItemSlot, string> Equipped { get; set; } = [];
    public List<string> Favored { get; set; } = [];   // 성향 · 전공으로 잘하는 과목

    public bool Owns(Item item) => Owned.Contains(item.Id);
    public Item? Worn(ItemSlot slot) => Equipped.TryGetValue(slot, out string? id) ? Items.All.FirstOrDefault(i => i.Id == id) : null;
    public void PutOn(Item item) => Equipped[item.Slot] = item.Id;
    public void TakeOff(ItemSlot slot) => Equipped.Remove(slot);

    /// <summary>장착한 아이템의 배율을 곱한다.</summary>
    public double ItemRate(Func<Item, double> rate)
    {
        double result = 1;
        foreach (var slot in Equipped.Keys) if (Worn(slot) is { } item) result *= rate(item);
        return result;
    }

    public static Field FieldOf(string id) => id switch
    {
        "scaling" => Field.Periodontics,
        "resin" or "root" => Field.Conservative,
        "inlay" or "onlay" or "crown" or "fullmouth" => Field.Prosthodontics,
        "extraction" or "wisdom" or "implant" => Field.Surgery,
        "ortho" => Field.Orthodontics,
        _ => Field.Pediatrics,
    };

    public static string FieldName(Field field) => field switch
    {
        Field.Periodontics => "치주",
        Field.Conservative => "보존",
        Field.Prosthodontics => "보철",
        Field.Surgery => "구강외과",
        Field.Orthodontics => "교정",
        _ => "소아",
    };

    private const double MajorFieldBonus = 1.3;

    private Field? MajorField => Major == Major.None ? null : FieldOf(Career.MajorFavors(Major)[0]);

    /// <summary>진료과 평판. 그 과 환자를 볼수록 오르고, 전공 과는 처음부터 높다. 높을수록 그 과 환자가 자주 온다.</summary>
    public double FieldReputation(Field field) =>
        (1 + 0.5 * Math.Log10(1 + FieldPatients.GetValueOrDefault(field) / 100)) * (field == MajorField ? MajorFieldBonus : 1);

    public void AddFieldPatients(string id, double count)
    {
        var field = FieldOf(id);
        FieldPatients[field] = FieldPatients.GetValueOrDefault(field) + count;
    }

    /// <summary>전공 과목 — 상한이 높고 레벨당 수입도 높다.</summary>
    public bool IsMajor(string id) => Career.MajorFavors(Major).Contains(id);
    public int LevelCap(string id) => IsMajor(id) ? MajorMaxLevel : MaxLevel;
    public int SeminarCap(string id) => IsMajor(id) ? MajorSeminars : MaxSeminars;

    public bool IsFavored(string id) => Favored.Contains(id) || IsMajor(id);

    public int Level(Ability ability) => Abilities.GetValueOrDefault(ability);
    public double AbilityCost(Ability ability) => 3e7 * Math.Pow(2.2, Level(ability));
    public void Raise(Ability ability) => Abilities[ability] = Level(ability) + 1;

    public static string AbilityName(Ability ability) => ability switch
    {
        Ability.Diagnosis => "정확한 진단",
        Ability.Technique => "기술",
        Ability.Care => "환자 관리",
        Ability.Consult => "상담",
        Ability.Leadership => "직원 관리",
        _ => "체력 관리",
    };

    public static string AbilityNote(Ability ability) => ability switch
    {
        Ability.Diagnosis => "클레임 발생 −12%",
        Ability.Technique => "전체 단가 +4%",
        Ability.Care => "클레임 해소 +30%, 유입 +3%",
        Ability.Consult => "크라운 이상 비급여 단가 +5%",
        Ability.Leadership => "위생사 없는 체어 효율 +3% (최대 80%)",
        _ => "피로 누적 −8%, 회복 +20%",
    };

    /// <summary>스킬트리의 선행 과목. 없으면 뿌리.</summary>
    public static string? Prerequisite(string id) => id switch
    {
        "resin" => "scaling",
        "sealant" or "pedo_resin" => "fluoride",
        "pedo_crown" => "pedo_resin",
        "extraction" => "scaling",
        "wisdom" => "extraction",
        "inlay" => "resin",
        "onlay" => "inlay",
        "root" or "crown" => "resin",
        "implant" or "ortho" => "crown",
        "fullmouth" => "implant",
        _ => null,
    };

    public static int Depth(string id) => Prerequisite(id) is { } parent ? Depth(parent) + 1 : 0;

    private double PatientsOf(string id) => Patients.GetValueOrDefault(id);
    public int SeminarsOf(string id) => Seminars.GetValueOrDefault(id);
    private int PatientLevel(string id) => (int)Math.Sqrt(PatientsOf(id) / PatientsPerStep);

    public int Level(string id) => Math.Min(LevelCap(id), SeminarsOf(id) + PatientLevel(id));

    public double PriceRate(string id) => 1 + (IsMajor(id) ? MajorPricePerLevel : PricePerLevel) * Level(id);
    public double TimeRate(string id) => Math.Pow(TimePerLevel, Level(id));

    /// <summary>다음 레벨까지 볼 환자 수.</summary>
    public int PatientsToNext(string id)
    {
        double need = Math.Pow(PatientLevel(id) + 1, 2) * PatientsPerStep;
        return (int)Math.Ceiling(need - PatientsOf(id));
    }

    /// <summary>숙련될수록 같은 난이도도 실수가 준다.</summary>
    public double Risk(Treatment t) => t.Difficulty * Math.Pow(0.97, Level(t.Id));

    /// <summary>잘하는 과목은 같은 환자를 봐도 더 빨리 는다.</summary>
    public void AddPatients(string id, double count) => Patients[id] = PatientsOf(id) + count * (IsFavored(id) ? FavoredRate : 1);

    public double SeminarCost(Treatment t) =>
        BaseSeminar * Math.Sqrt(t.Price / Catalog.Treatments[0].Price) * Math.Pow(SeminarGrowth, SeminarsOf(t.Id));

    /// <summary>세미나를 들을 수 없는 까닭. 들을 수 있으면 null.</summary>
    public string? StudyBlocked(Treatment t)
    {
        if (SeminarsOf(t.Id) >= SeminarCap(t.Id)) return "세미나를 모두 들었습니다";
        if (Level(t.Id) >= LevelCap(t.Id)) return "최고 숙련도입니다";
        if (Prerequisite(t.Id) is not { } parent || Level(parent) >= RequiredLevel) return null;
        return $"{Catalog.Treatments.First(x => x.Id == parent).Name} Lv.{RequiredLevel} 필요";
    }

    public void Study(Treatment t) => Seminars[t.Id] = SeminarsOf(t.Id) + 1;
}
