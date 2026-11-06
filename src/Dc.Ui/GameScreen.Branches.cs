using Dc.Core;
using SkiaSharp;

namespace Dc.Ui;

/// <summary>지점 탭 — 지점 목록, 새 지점 입지 · 특화 고르기, 특화 바꾸기.</summary>
public sealed partial class GameScreen
{
    private const int BranchTab = 6;
    private enum BranchPage { List, Site, OpenSpecialty, ChangeSpecialty }

    private BranchPage _branchPage;
    private Site _pendingSite;

    private void GoTo(BranchPage page)
    {
        _branchPage = page;
        _scroll = 0;
    }

    private static string BranchName(int index) => index == 0 ? "본점" : $"{index + 1}호점";

    private IReadOnlyList<Choice> BranchChoices()
    {
        Choice back = new("뒤로", "지점 목록으로 돌아갑니다", "←", true, "mn_close", () => GoTo(BranchPage.List));
        switch (_branchPage)
        {
            case BranchPage.Site:
                return [back, .. Enum.GetValues<Site>().Select(SiteChoice)];
            case BranchPage.OpenSpecialty:
                return [back, .. Company.Specialties.Select(s => SpecialtyChoice(s, "개원", true, () =>
                {
                    if (!company.Open(_pendingSite, s.Kind)) { Toast("돈이 모자랍니다"); return; }
                    ClearImages();
                    GoTo(BranchPage.List);
                }))];
            case BranchPage.ChangeSpecialty:
                string price = KoreanNumber.Format(Company.ChangeSpecialtyCost);
                return [back, .. Company.Specialties.Where(s => s.Kind != clinic.Specialty).Select(s => SpecialtyChoice(s, price, clinic.Money >= Company.ChangeSpecialtyCost, () =>
                {
                    if (company.ChangeSpecialty(clinic, s.Kind)) GoTo(BranchPage.List);
                }))];
            default:
                return BranchList();
        }
    }

    private IReadOnlyList<Choice> BranchList()
    {
        var list = new List<Choice>
        {
            new($"명성 {company.Fame} · 전 지점 ×{company.FameRate:0.00}", $"새 지점을 열면 명성 {Math.Max(company.Fame, company.PendingFame)}로 확정", "정보", false, "ic_fame", () => { }),
        };
        for (int i = 0; i < company.Branches.Count; i++)
        {
            var branch = company.Branches[i];
            int index = i;
            string specialty = Company.Specialties.First(s => s.Kind == branch.Specialty).Name;
            list.Add(new($"{BranchName(i)} · {branch.Space.Pyeong}평 · 체어 {branch.Chairs}대",
                $"{Company.Sites[(int)branch.Site].Name} · {specialty} · {branch.ShortBottleneck}",
                i == company.Selected ? "운영 중" : "보기", i != company.Selected, "bg_branch", () =>
                {
                    company.Selected = index;
                    ClearImages();
                }));
        }
        list.Add(new("새 지점 개원", "입지와 특화를 고릅니다", "다음", true, "bg_branch", () => GoTo(BranchPage.Site)));
        list.Add(new("이 지점 특화 바꾸기", "비용을 내고 특화를 바꿉니다", "다음", true, "ic_fame", () => GoTo(BranchPage.ChangeSpecialty)));
        return list;
    }

    /// <summary>머리글 지점 이름을 눌렀을 때 나오는 목록 — 고르면 그 지점으로 바꾸고 닫는다.</summary>
    private IReadOnlyList<Choice> BranchSwitchChoices()
    {
        var list = new List<Choice>();
        for (int i = 0; i < company.Branches.Count; i++)
        {
            var branch = company.Branches[i];
            int index = i;
            string specialty = Company.Specialties.First(s => s.Kind == branch.Specialty).Name;
            list.Add(new($"{BranchName(i)} · {branch.Space.Pyeong}평 · 체어 {branch.Chairs}대",
                $"{Company.Sites[(int)branch.Site].Name} · {specialty} · 초당 {KoreanNumber.Format(branch.RevenuePerSecond)}원",
                i == company.Selected ? "운영 중" : "바꾸기", true, "bg_branch", () =>
                {
                    if (index != company.Selected) { company.Selected = index; ClearImages(); }
                    _menu = MenuPage.Closed;
                }));
        }
        list.Add(company.Unlocked
            ? new("새 지점 개원", "지점 탭에서 입지와 특화를 고릅니다", "열기", true, "bg_branch", () => { _menu = MenuPage.Closed; _tab = BranchTab; _hiring = false; _itemSlot = null; GoTo(BranchPage.Site); })
            : new("새 지점 개원", $"본점이 {Catalog.Spaces[Company.UnlockSpace].Pyeong}평이 되면 열립니다", "잠김", false, "bg_branch", () => { }));
        return list;
    }

    private Choice SiteChoice(Site site)
    {
        var info = Company.Sites[(int)site];
        return new(info.Name, $"유입 ×{info.Inflow:0.0} · 객단가 ×{info.Price:0.0}", KoreanNumber.Format(company.OpenCost(site)), company.CanOpen(site), "bg_branch", () =>
        {
            _pendingSite = site;
            GoTo(BranchPage.OpenSpecialty);
        });
    }

    private static Choice SpecialtyChoice((Specialty Kind, string Name, string Note) s, string price, bool enabled, Action pick) =>
        new(s.Name, s.Note, price, enabled, "ic_fame", pick);

    private void PressBranches(float y)
    {
        var choices = BranchChoices();
        int index = (int)((y - ListTop + _scroll) / RowH);
        if (index >= choices.Count) return;
        if (choices[index].Enabled) choices[index].Pick();
        else if (choices[index].Price != "정보" && choices[index].Price != "운영 중") Toast("돈이 모자랍니다");
    }

    private void DrawBranches(SKCanvas c)
    {
        var choices = BranchChoices();
        for (int i = 0; i < choices.Count; i++) DrawChoice(c, choices[i], ListTop + i * RowH - _scroll);
    }
}
