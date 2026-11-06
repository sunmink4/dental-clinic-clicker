using Dc.Core;
using SkiaSharp;

namespace Dc.Ui;

/// <summary>위키 — 화면 전체를 덮는 도움말. 주제 목록과 본문.</summary>
public sealed partial class GameScreen
{
    private const float WikiHeader = 60, WikiRow = 52, WikiLine = 28, WikiParagraph = 12, WikiText = 19;

    private int _wikiTopic = -1;
    private bool Reading => _wikiTopic >= 0;
    private bool InWiki => _menu == MenuPage.Wiki;

    private float WikiHeight
    {
        get
        {
            float total = 0;
            foreach (string line in Wiki.Topics[_wikiTopic].Lines)
                total += Wrap(line, W - 48, WikiText).Count * WikiLine + WikiParagraph;
            return total;
        }
    }

    private void PressWiki(float y)
    {
        if (y < WikiHeader)
        {
            if (Reading) _wikiTopic = -1;
            else _menu = MenuPage.Root;
            _scroll = 0;
            return;
        }
        if (Reading) return;
        int index = (int)((y - WikiHeader) / WikiRow);
        if (index < Wiki.Topics.Length) { _wikiTopic = index; _scroll = 0; }
    }

    private void DrawWiki(SKCanvas c)
    {
        var dark = new SKColor(0x13, 0x4E, 0x4A);
        Fill(c, new SKRect(0, 0, W, _height), new SKColor(0xFF, 0xFB, 0xF5));
        if (Reading)
        {
            float y = WikiHeader + 34 - _scroll;
            foreach (string line in Wiki.Topics[_wikiTopic].Lines)
            {
                foreach (string row in Wrap(line, W - 48, WikiText))
                {
                    Label(c, row, 24, y, WikiText, dark);
                    y += WikiLine;
                }
                y += WikiParagraph;
            }
        }
        else
        {
            for (int i = 0; i < Wiki.Topics.Length; i++)
            {
                float y = WikiHeader + i * WikiRow;
                Label(c, Wiki.Topics[i].Title, 24, y + 34, 21, dark);
                Label(c, "›", W - 24, y + 34, 22, dark, SKTextAlign.Right);
                Fill(c, new SKRect(0, y + WikiRow - 1, W, y + WikiRow), new SKColor(0xE5, 0xE7, 0xEB));
            }
        }
        Fill(c, new SKRect(0, 0, W, WikiHeader), new SKColor(0x13, 0x4E, 0x4A));
        Label(c, Reading ? "←  " + Wiki.Topics[_wikiTopic].Title : "←  위키", 20, 39, 23, SKColors.White);
    }
}
