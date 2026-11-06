using System.Text.Json;

namespace Dc.Core;

public sealed record SaveFile(Clinic Clinic, DateTime SavedUtc, Career? Career = null, EventDirector? Events = null, Company? Company = null, StoryDirector? Story = null);

/// <summary>JSON 저장. 쓰다 꺼져도 이전 저장이 남도록 임시 파일에 쓴 뒤 바꿔치기한다.</summary>
public sealed class SaveStore(string path)
{
    public SaveFile? Load()
    {
        if (!File.Exists(path)) return null;
        try
        {
            return JsonSerializer.Deserialize<SaveFile>(File.ReadAllText(path));
        }
        catch (JsonException)
        {
            File.Move(path, path + ".bad", overwrite: true);   // 망가진 저장은 지우지 않고 옆에 둔다
            return null;
        }
    }

    /// <summary>새 게임 — 지금 저장은 지우지 않고 .old 로 옆에 둔다.</summary>
    public void Reset()
    {
        if (File.Exists(path)) File.Move(path, path + ".old", overwrite: true);
    }

    public void Save(Clinic clinic, Career career, EventDirector events, DateTime utcNow) =>
        Save(new Company(clinic), career, events, utcNow);

    public void Save(Company company, Career career, EventDirector events, DateTime utcNow) =>
        Save(company, career, events, new StoryDirector(), utcNow);

    public void Save(Company company, Career career, EventDirector events, StoryDirector story, DateTime utcNow)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(new SaveFile(company.Head, utcNow, career, events, company, story)));
        File.Move(temp, path, overwrite: true);
    }
}
