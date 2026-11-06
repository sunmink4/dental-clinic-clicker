namespace Dc.Core;

public enum ItemSlot { FaceShield, Glasses, Mask, Loupe, Gown, Scrubs, Handpiece, Chair }

/// <summary>원장이 쓰는 물건. 배율은 곱해지고 1 이면 효과 없음. Time · Claim · Fatigue 는 낮을수록 좋다.</summary>
public sealed record Item(string Id, string Name, ItemSlot Slot, double Cost, string Note,
    double Time = 1, double Price = 1, double Claim = 1, double Fatigue = 1, double Inflow = 1);

public static class Items
{
    public static readonly Item[] All =
    [
        new("fs1", "일회용 페이스쉴드", ItemSlot.FaceShield, 5e6, "클레임 발생 -5%", Claim: 0.95),
        new("fs2", "김서림 방지 쉴드", ItemSlot.FaceShield, 3e7, "클레임 발생 -12%", Claim: 0.88),
        new("gl1", "보호 안경", ItemSlot.Glasses, 3e6, "피로 누적 -3%", Fatigue: 0.97),
        new("gl2", "눈이 편한 안경", ItemSlot.Glasses, 2e7, "피로 누적 -10%", Fatigue: 0.9),
        new("ms1", "KF94 마스크", ItemSlot.Mask, 2e6, "클레임 발생 -3%", Claim: 0.97),
        new("ms2", "숨쉬기 편한 마스크", ItemSlot.Mask, 1.5e7, "피로 누적 -7%", Fatigue: 0.93),
        new("lp1", "2.5배 루페", ItemSlot.Loupe, 3e7, "진료 시간 -5%", Time: 0.95),
        new("lp2", "4배 LED 루페", ItemSlot.Loupe, 3e8, "진료 시간 -10%, 수가 +3%", Time: 0.9, Price: 1.03),
        new("gw1", "깔끔한 가운", ItemSlot.Gown, 1e7, "환자 유입 +3%", Inflow: 1.03),
        new("gw2", "맞춤 가운", ItemSlot.Gown, 1e8, "환자 유입 +8%", Inflow: 1.08),
        new("sc1", "면 진료복", ItemSlot.Scrubs, 5e6, "피로 누적 -3%", Fatigue: 0.97),
        new("sc2", "기능성 진료복", ItemSlot.Scrubs, 5e7, "피로 누적 -10%, 진료 시간 -2%", Time: 0.98, Fatigue: 0.9),
        new("hp1", "신형 핸드피스", ItemSlot.Handpiece, 5e7, "진료 시간 -6%", Time: 0.94),
        new("hp2", "무음 핸드피스", ItemSlot.Handpiece, 5e8, "진료 시간 -10%, 클레임 발생 -7%", Time: 0.9, Claim: 0.93),
        new("ch1", "인체공학 의자", ItemSlot.Chair, 2e7, "피로 누적 -5%", Fatigue: 0.95),
        new("ch2", "전동 술자 의자", ItemSlot.Chair, 2e8, "피로 누적 -12%, 환자 유입 +2%", Fatigue: 0.88, Inflow: 1.02),
    ];

    public static string SlotName(ItemSlot slot) => slot switch
    {
        ItemSlot.FaceShield => "페이스쉴드",
        ItemSlot.Glasses => "안경",
        ItemSlot.Mask => "마스크",
        ItemSlot.Loupe => "루페",
        ItemSlot.Gown => "가운",
        ItemSlot.Scrubs => "진료복",
        ItemSlot.Handpiece => "핸드피스",
        _ => "의자",
    };
}
