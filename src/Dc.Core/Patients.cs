namespace Dc.Core;

/// <summary>
/// 환자 연령대. 전공 · 특화 · 입지가 환자층을 정하고, 환자층이 진료 비중을 바꾼다.
/// 평범한 치과(신도시, 전공 · 특화 없음)의 비중이 기획 표와 같도록 그 환자층을 기준 1 로 맞춘다.
/// </summary>
public static class Patients
{
    public static readonly string[] Ages = ["소아", "청년", "성인", "노년"];

    private static readonly double[] Standard = [0.10, 0.25, 0.40, 0.25];

    /// <summary>연령대별로 이 진료를 얼마나 찾는가.</summary>
    private static double[] Demand(string id) => id switch
    {
        "scaling" => [0, 1, 1.2, 1.2],
        "fluoride" or "sealant" or "pedo_resin" or "pedo_crown" => [2, 0.2, 0, 0],
        "resin" => [1.2, 1.2, 1, 0.8],
        "extraction" => [0.6, 0.4, 0.8, 1.6],
        "wisdom" => [0, 1.8, 0.8, 0.1],
        "inlay" or "onlay" => [0.1, 1, 1.2, 0.8],
        "root" => [0.4, 0.8, 1.2, 1.2],
        "crown" => [0, 0.4, 1.2, 1.4],
        "implant" => [0, 0.1, 1, 2],
        "ortho" => [0.6, 2, 0.6, 0.05],
        _ => [0, 0, 0.6, 2],
    };

    public static double[] Mix(Site site, Specialty specialty, Major major)
    {
        double[] mix = [.. Standard];
        if (major == Major.Pediatrics) mix[0] += 0.25;
        if (specialty == Specialty.Pediatric) mix[0] += 0.8;
        if (specialty == Specialty.Orthodontics) mix[1] += 0.3;
        if (specialty == Specialty.Implant) mix[3] += 0.3;
        switch (site)
        {
            case Site.Province: mix[3] += 0.2; break;
            case Site.Seoul: mix[1] += 0.15; break;
            case Site.Gangnam: mix[2] += 0.2; break;
        }
        double total = mix.Sum();
        return mix.Select(m => m / total).ToArray();
    }

    private static double Fit(string id, double[] mix) => Demand(id).Zip(mix, (d, m) => d * m).Sum();

    /// <summary>평범한 치과와 견준 이 진료의 수요 배율.</summary>
    public static double Factor(string id, double[] mix) => Fit(id, mix) / Fit(id, Standard);
}
