using Dc.Core;
using SkiaSharp;

namespace Dc.Ui;

/// <summary>주인공 얼굴 고르기 — 성별마다 9종. 얼굴 그림이 없으면 건너뛴다.</summary>
public sealed partial class GameScreen
{
    private const int FaceCount = 9, FaceColumns = 3;
    private const float FaceTop = 150, FaceCell = 168, FaceLeft = (W - FaceCell * FaceColumns) / 2;
    private static float FaceBottom => FaceTop + FaceCell * (FaceCount / FaceColumns);

    private string FaceImage(int face) => $"hero{Suffix}{face}";

    private bool PickingFace => PickingTrait && clinic.Face == 0 && Image(FaceImage(1)) != null;

    /// <summary>주인공 그림. 얼굴을 고르지 않은 저장은 단계별 기본 그림을 쓴다.</summary>
    private string Hero(string stage)
    {
        int face = company.Head.Face;
        return face > 0 && Image(FaceImage(face)) != null ? FaceImage(face) : $"face_{stage}{Suffix}";
    }

    /// <summary>아이템을 반영한 주인공 그림. 마스크를 쓰면 마스크 쓴 얼굴이 나온다.</summary>
    private string HeroWearing(string stage)
    {
        string hero = Hero(stage);
        return clinic.Mastery.Worn(ItemSlot.Mask) != null && Image(hero + "_mask") != null ? hero + "_mask" : hero;
    }

    private void PressFace(float x, float y)
    {
        if (y >= FaceBottom) { clinic.Doctor = null; return; }
        if (y < FaceTop || x < FaceLeft || x >= W - FaceLeft) return;
        int column = (int)((x - FaceLeft) / FaceCell), row = (int)((y - FaceTop) / FaceCell);
        clinic.Face = row * FaceColumns + column + 1;
    }

    private void DrawFaces(SKCanvas c)
    {
        var dark = new SKColor(0x13, 0x4E, 0x4A);
        Fill(c, new SKRect(0, 0, W, _height), new SKColor(0xE6, 0xF7, 0xF4));
        Label(c, "얼굴을 고르세요", W / 2, 100, 30, dark, SKTextAlign.Center);
        for (int i = 0; i < FaceCount; i++)
        {
            var box = SKRect.Create(FaceLeft + i % FaceColumns * FaceCell + 5, FaceTop + i / FaceColumns * FaceCell + 5, FaceCell - 10, FaceCell - 10);
            Rounded(c, box, new SKColor(0xFF, 0xFB, 0xF5));
            Icon(c, FaceImage(i + 1), SKRect.Create(box.Left + 6, box.Top + 6, box.Width - 12, box.Height - 12), $"{i + 1}");
        }
        Label(c, "← 성별 다시 고르기", W / 2, FaceBottom + 50, 20, dark, SKTextAlign.Center);
    }
}
