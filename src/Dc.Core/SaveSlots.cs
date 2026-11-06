using System.Globalization;

namespace Dc.Core;

/// <summary>저장 칸 3개. 1번은 예전 저장 파일(save.json)을 그대로 쓴다. 고른 칸은 slot.txt 에 남긴다.</summary>
public sealed class SaveSlots(string folder)
{
    public const int Count = 3;

    private string SlotFile => Path.Combine(folder, "slot.txt");

    public int Current
    {
        get
        {
            if (!File.Exists(SlotFile)) return 1;
            return int.TryParse(File.ReadAllText(SlotFile), out int slot) ? Math.Clamp(slot, 1, Count) : 1;
        }
    }

    public void Select(int slot)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(SlotFile, slot.ToString(CultureInfo.InvariantCulture));
    }

    public SaveStore Store(int slot) => new(Path.Combine(folder, slot == 1 ? "save.json" : $"save{slot}.json"));

    /// <summary>칸에 든 게임을 한 줄로.</summary>
    public string Summary(int slot)
    {
        if (Store(slot).Load() is not { } save) return "비어 있음";
        string when = save.SavedUtc.ToLocalTime().ToString("MM-dd HH:mm", CultureInfo.InvariantCulture);
        if (save.Career is { Done: false } career) return $"수련 중 · {when}";
        var head = save.Company?.Branches.FirstOrDefault() ?? save.Clinic;
        return $"{head.Space.Pyeong}평 · {KoreanNumber.Format(head.Money)}원 · {when}";
    }
}
