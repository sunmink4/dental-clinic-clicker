using Dc.Core;
using SkiaSharp;

namespace Dc.Ui;

/// <summary>장비 배치 — 위쪽 진료실 그림에서 체어와 장비를 끌어 옮기고, 아래 목록에서 산다. 장비를 아래로 끌어 내리면 절반 값에 판다.</summary>
public sealed partial class GameScreen
{
    private const float EquipmentSize = 72, SellLine = 1.05f, OverlapInset = 8, SpotStep = 12;
    private const double SellRate = 0.5;
    private bool _arranging;
    private PlacedEquipment? _moving;
    private float _movingFromX, _movingFromY;

    private static float ClinicHeight => ClinicBottom - ClinicTop;
    private static float PlacedX(PlacedEquipment p) => p.X * W;
    private static float PlacedY(PlacedEquipment p) => ClinicTop + p.Y * ClinicHeight;

    private IReadOnlyList<Choice> EquipmentChoices()
    {
        var list = new List<Choice> { new("완료", "게임으로 돌아갑니다", "닫기", true, "mn_close", () => { _arranging = false; _scroll = 0; }) };
        foreach (var e in Equipment.Shop)
        {
            bool owned = clinic.Placed.Any(p => p.Id == e.Id);
            list.Add(new(e.Name, e.Note, owned ? "보유" : KoreanNumber.Format(e.Cost), !owned && clinic.Money >= e.Cost, "eq_" + e.Id, () =>
            {
                if (owned) return;
                if (FreeSpot(new PlacedEquipment()) == null) Toast("놓을 자리가 없습니다");
                else if (!clinic.BuyEquipment(e)) Toast("돈이 모자랍니다");
            }));
        }
        return list;
    }

    private void PressEquipment(float y)
    {
        if (y < ListTop) return;
        var choices = EquipmentChoices();
        int index = (int)((y - ListTop + _scroll) / RowH);
        if (index < choices.Count) choices[index].Pick();
    }

    /// <summary>누른 장비를 잡는다. 장비가 없으면 그 자리의 체어를 잡는다.</summary>
    private void GrabEquipment(float x, float y)
    {
        _moving = null;
        if (!_arranging || y is < ClinicTop or >= ClinicBottom) return;
        _moving = clinic.Placed.LastOrDefault(p => Math.Abs(PlacedX(p) - x) < EquipmentSize / 2 && Math.Abs(PlacedY(p) - y) < EquipmentSize / 2);
        if (_moving != null) { RememberStart(); return; }
        for (int i = 0; i < clinic.Chairs; i++)
        {
            var box = ChairBox(i);
            if (!box.Contains(x, y)) continue;
            if (!clinic.ChairPlaces.TryGetValue(i, out _moving))
                clinic.ChairPlaces[i] = _moving = new PlacedEquipment { X = box.MidX / W, Y = (box.MidY - ClinicTop) / ClinicHeight };
            RememberStart();
            return;
        }
    }

    private void MoveEquipment(float x, float y)
    {
        _moving!.X = Math.Clamp(x / W, 0, 1);
        _moving.Y = Math.Clamp((y - ClinicTop) / ClinicHeight, 0, 1.15f);
    }

    private void DropEquipment()
    {
        if (_moving!.Y > SellLine && clinic.SellEquipment(_moving, SellRate)) Toast("장비를 팔았습니다");
        else if (Blocked(_moving))
        {
            _moving.X = _movingFromX;
            _moving.Y = _movingFromY;
            Toast("겹쳐서 놓을 수 없습니다");
        }
        _moving = null;
    }

    private void RememberStart() => (_movingFromX, _movingFromY) = (_moving!.X, _moving.Y);

    private SKRect BoxOf(PlacedEquipment p)
    {
        foreach (var (index, chair) in clinic.ChairPlaces)
            if (chair == p) return ChairBox(index);
        return SKRect.Create(PlacedX(p) - EquipmentSize / 2, PlacedY(p) - EquipmentSize / 2, EquipmentSize, EquipmentSize);
    }

    private static SKRect Inner(SKRect box) => SKRect.Inflate(box, -OverlapInset, -OverlapInset);

    /// <summary>체어가 들어올 빈 칸까지 포함해 다른 물건과 겹치거나, 대기실 · 치아가 있는 아래쪽을 침범하면 참.</summary>
    private bool Blocked(PlacedEquipment target)
    {
        var box = Inner(BoxOf(target));
        if (box.Top < ClinicTop || box.Bottom > WaitingTop || box.Left < 0 || box.Right > W) return true;
        for (int i = 0; i < clinic.Space.MaxChairs; i++)
        {
            bool self = clinic.ChairPlaces.TryGetValue(i, out var chair) && chair == target;
            if (!self && box.IntersectsWith(Inner(ChairBox(i)))) return true;
        }
        return clinic.Placed.Any(p => p != target && p.Y >= 0 && box.IntersectsWith(Inner(BoxOf(p))));
    }

    /// <summary>아래쪽부터 훑어 비어 있는 첫 자리를 찾는다.</summary>
    private (float X, float Y)? FreeSpot(PlacedEquipment target)
    {
        float keepX = target.X, keepY = target.Y;
        try
        {
            for (float y = WaitingTop - EquipmentSize / 2 + OverlapInset; y >= ClinicTop + EquipmentSize / 2 - OverlapInset; y -= SpotStep)
                for (float x = EquipmentSize / 2; x <= W - EquipmentSize / 2; x += SpotStep)
                {
                    target.X = x / W;
                    target.Y = (y - ClinicTop) / ClinicHeight;
                    if (!Blocked(target)) return (target.X, target.Y);
                }
            return null;
        }
        finally { (target.X, target.Y) = (keepX, keepY); }
    }

    /// <summary>새 장비에 자리를 주고, 이전으로 공간이 바뀌면 배치를 다시 맞춘다.</summary>
    private void SettleEquipment()
    {
        clinic.PlaceOwnedEquipment();
        if (clinic.LayoutSpace != clinic.SpaceIndex)
        {
            if (clinic.LayoutSpace >= 0)
            {
                clinic.ChairPlaces.Clear();
                foreach (var p in clinic.Placed) p.Y = PlacedEquipment.Unplaced;
            }
            clinic.LayoutSpace = clinic.SpaceIndex;
        }
        foreach (var p in clinic.Placed.Where(p => p.Y < 0))
        {
            var spot = FreeSpot(p) ?? (0.5f, 0.5f);   // 스캐너 · CT 는 이미 샀으니 자리가 없어도 보여 준다
            (p.X, p.Y) = spot;
        }
    }

    private void DrawPlacedEquipment(SKCanvas c)
    {
        SettleEquipment();
        foreach (var p in clinic.Placed.OrderBy(p => p.Y))
        {
            var box = SKRect.Create(PlacedX(p) - EquipmentSize / 2, PlacedY(p) - EquipmentSize / 2, EquipmentSize, EquipmentSize);
            Icon(c, "eq_" + p.Id, box, p.Id[..1].ToUpperInvariant());
        }
        if (_moving != null) Outline(c, BoxOf(_moving), Blocked(_moving) ? new SKColor(0xDC, 0x26, 0x26) : new SKColor(0xEA, 0x88, 0x0C));
    }

    private void DrawArranging(SKCanvas c)
    {
        Fill(c, new SKRect(0, ClinicBottom, W, _height), new SKColor(0xFF, 0xFB, 0xF5));
        Fill(c, new SKRect(0, ClinicBottom, W, ListTop), new SKColor(0x13, 0x4E, 0x4A));
        Label(c, "장비 배치 · 끌어서 옮기고, 아래로 내리면 판매", 16, ClinicBottom + 34, 18, SKColors.White);
        c.Save();
        c.ClipRect(new SKRect(0, ListTop, W, _height));
        var choices = EquipmentChoices();
        for (int i = 0; i < choices.Count; i++) DrawChoice(c, choices[i], ListTop + i * RowH - _scroll);
        c.Restore();
    }
}
