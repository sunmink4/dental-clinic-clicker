namespace Dc.Core;

public sealed record Treatment(string Id, string Name, double Price, double Seconds, double Weight, int Difficulty);

public sealed record Space(string Name, int Pyeong, int MaxChairs, double MoveCost, double Inflow, double BuildSeconds);

/// <summary>기획 노트 03 진료 메뉴 · 04 공간 이전의 표.</summary>
public static class Catalog
{
    public static readonly Treatment[] Treatments =
    [
        new("scaling", "스케일링", 15_000, 3, 40, 1),
        new("fluoride", "불소 도포", 15_000, 3, 40, 1),      // 소아치과 특화: 스케일링 대신
        new("sealant", "실란트", 40_000, 4, 15, 1),          // 소아치과 특화
        new("pedo_resin", "소아 충치치료", 80_000, 5, 25, 2),   // 소아치과: 유치 충치
        new("pedo_crown", "소아 크라운", 200_000, 7, 8, 3),      // 소아치과: 유치 기성금관
        new("resin", "레진", 100_000, 5, 25, 2),
        new("extraction", "발치", 50_000, 4, 20, 2),
        new("wisdom", "사랑니 발치", 150_000, 10, 8, 3),
        new("inlay", "인레이", 350_000, 9, 8, 3),
        new("onlay", "온레이", 450_000, 10, 6, 3),
        new("root", "신경치료", 150_000, 8, 15, 3),
        new("crown", "크라운", 500_000, 12, 10, 3),
        new("implant", "임플란트", 1_200_000, 20, 7, 4),
        new("ortho", "교정", 4_000_000, 30, 3, 4),
        new("fullmouth", "전악 재건", 25_000_000, 60, 1, 5),
    ];

    public static readonly Space[] Spaces =
    [
        new("상가 2층", 25, 3, 0, 1.0, 0),
        new("상가 확장", 35, 5, 1.5e8, 1.3, 30),
        new("메디컬빌딩 한 층", 50, 8, 1e9, 1.7, 120),
        new("메디컬빌딩 한 층+", 80, 12, 6e9, 2.2, 300),
        new("치과병원", 150, 20, 4e10, 3.0, 600),
        new("치과 전용 빌딩", 300, 35, 4e11, 4.0, 1200),
    ];
}
