namespace Dc.Core;

public enum DentistSubject { General, Conservative, Prosthodontics, Surgery, Periodontics, Orthodontics }

/// <summary>사람 이름 — 성 + 두 글자 이름을 무작위로 짓는다.</summary>
public static class KoreanNames
{
    private static readonly string[] Surnames = ["김", "이", "박", "최", "정", "강", "조", "윤", "장", "임", "한", "오", "서", "신", "권", "황", "안", "송", "유", "홍"];
    private static readonly string[] Women = ["민지", "서연", "지은", "수빈", "하은", "예진", "유진", "소영", "지혜", "은비", "다은", "채원", "혜린", "가영", "보람", "수진", "나연", "미래", "주희", "선영"];
    private static readonly string[] Men = ["민준", "지훈", "현우", "도윤", "준호", "성민", "태희", "승현", "재원", "동현", "우진", "시우", "정훈", "상우", "건우"];

    public static string Random(Random random, Gender gender)
    {
        var given = gender == Gender.Female ? Women : Men;
        return Surnames[random.Next(Surnames.Length)] + given[random.Next(given.Length)];
    }
}

/// <summary>고용하는 치과의사(페이닥터). Salary 는 초당 월급, Fee 는 Clinic.Cost(Dentist) 에 곱하는 계약금 배율.</summary>
public sealed record DentistHire(string Name, int Years, DentistSubject Subject, double Salary, double Fee, Gender Gender = Gender.Male)
{
    public const double BaseSalary = 6_000;
    private const double SalaryPerYear = 0.2, SpecialistSalary = 1.2, BaseFee = 0.7, FeePerYear = 0.06;
    private const int MaxYears = 15;

    /// <summary>묶음 구매와 자동 경영이 뽑는 신입 일반의.</summary>
    public static DentistHire Rookie => Make("신입", 1, DentistSubject.General);

    /// <summary>n 번째로 들어오는 신입. 남 · 여 번갈아 온다.</summary>
    public static DentistHire RookieNumber(int n) => Rookie with { Gender = n % 2 == 0 ? Gender.Male : Gender.Female };

    /// <summary>이력서 · 체어에 쓰는 얼굴 그림.</summary>
    public string Face => Gender == Gender.Female ? "face_dentist_f" : "face_dentist";

    public static DentistHire Random(Random random)
    {
        // 저연차가 더 많다
        int years = 1 + (int)(Math.Pow(random.NextDouble(), 2) * MaxYears);
        var subject = (DentistSubject)random.Next(Enum.GetValues<DentistSubject>().Length);
        var gender = random.Next(2) == 0 ? Gender.Male : Gender.Female;
        return Make(KoreanNames.Random(random, gender) + " 선생", years, subject) with { Gender = gender };
    }

    private static DentistHire Make(string name, int years, DentistSubject subject) => new(name, years, subject,
        BaseSalary * (1 + SalaryPerYear * years) * (subject == DentistSubject.General ? 1 : SpecialistSalary),
        BaseFee + FeePerYear * years);

    public string SubjectName => Subject switch
    {
        DentistSubject.Conservative => "보존",
        DentistSubject.Prosthodontics => "보철",
        DentistSubject.Surgery => "구강외과",
        DentistSubject.Periodontics => "치주",
        DentistSubject.Orthodontics => "교정",
        _ => "일반",
    };

    public string Strength => Subject switch
    {
        DentistSubject.Conservative => "레진 · 신경치료 +10%",
        DentistSubject.Prosthodontics => "보철 진료 +10%",
        DentistSubject.Surgery => "발치 · 임플란트 +10%",
        DentistSubject.Periodontics => "스케일링 +10%",
        DentistSubject.Orthodontics => "교정 +10%",
        _ => "진료 속도만",
    };

    public bool Covers(Treatment t) => (Subject, t.Id) is
        (DentistSubject.Conservative, "resin" or "root") or
        (DentistSubject.Prosthodontics, "inlay" or "onlay" or "crown" or "fullmouth") or
        (DentistSubject.Surgery, "extraction" or "wisdom" or "implant") or
        (DentistSubject.Periodontics, "scaling") or
        (DentistSubject.Orthodontics, "ortho");
}

public enum StaffField { None, Prevention, Prosthetic, Consulting, Infection }

/// <summary>고용하는 진료 보조 인력. 치위생사는 전문 분야가 있고 단독 진료를 하며, 간호조무사는 싸지만 체어 효율이 낮다.</summary>
public sealed record StaffHire(string Name, bool Assistant, int Years, StaffField Field, double Salary, int Face)
{
    public const int Faces = 20;
    private const double HygienistSalary = 1_500, AssistantSalary = 800, SalaryPerYear = 0.1;
    private const int MaxYears = 12;

    /// <summary>남자 얼굴 번호 (hyg_16, hyg_17). 나머지는 여자.</summary>
    public static bool IsManFace(int face) => face is 16 or 17;

    public static StaffHire Random(Random random, bool assistant)
    {
        int years = 1 + (int)(Math.Pow(random.NextDouble(), 2) * MaxYears);
        var field = assistant ? StaffField.None : (StaffField)(1 + random.Next(Enum.GetValues<StaffField>().Length - 1));
        int face = 1 + random.Next(Faces);
        string name = KoreanNames.Random(random, IsManFace(face) ? Gender.Male : Gender.Female) + (assistant ? " 조무사" : " 위생사");
        return new(name, assistant, years, field, (assistant ? AssistantSalary : HygienistSalary) * (1 + SalaryPerYear * years), face);
    }

    public string FieldName => Field switch
    {
        StaffField.Prevention => "예방",
        StaffField.Prosthetic => "보철 보조",
        StaffField.Consulting => "상담",
        StaffField.Infection => "감염 관리",
        _ => "진료 보조",
    };

    public string Strength => Field switch
    {
        StaffField.Prevention => "스케일링 · 불소 +5%",
        StaffField.Prosthetic => "인레이 · 온레이 · 크라운 +5%",
        StaffField.Consulting => "비급여 단가 +2%",
        StaffField.Infection => "클레임 발생 -3%",
        _ => "체어 효율 90%, 단독 진료 불가",
    };

    public bool Covers(Treatment t) => (Field, t.Id) is
        (StaffField.Prevention, "scaling" or "fluoride") or
        (StaffField.Prosthetic, "inlay" or "onlay" or "crown");
}
