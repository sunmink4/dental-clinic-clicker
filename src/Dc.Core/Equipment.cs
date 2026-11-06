namespace Dc.Core;

/// <summary>사서 진료실 화면에 놓는 장비. 지점마다 종류별로 한 대씩 둔다.</summary>
public sealed record Equipment(string Id, string Name, double Cost, string Note, double Capacity = 0, double Claim = 1)
{
    public static readonly Equipment[] Shop =
    [
        new("compressor", "콤프레셔", 1.5e7, "처리 +2%", Capacity: 0.02),
        new("autoclave", "멸균소독기", 2e7, "처리 +3%", Capacity: 0.03),
        new("xray", "파노라마 엑스레이", 3e7, "클레임 발생 -8%", Claim: 0.92),
        new("printer", "3D 프린터", 5e8, "처리 +5%, 클레임 발생 -5%", Capacity: 0.05, Claim: 0.95),
    ];

    public static Equipment? Find(string id) => Shop.FirstOrDefault(e => e.Id == id);
}

/// <summary>놓인 장비 하나. X · Y 는 진료실 그림 안의 비율(0~1).</summary>
public sealed class PlacedEquipment
{
    public const float Unplaced = -1;

    public string Id { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
}
