using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using GuildProto;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// 임시: 자동 플레이 테스트 (실행 후 삭제)
public static class TempAutoPlay
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static readonly StringBuilder log = new();
    public static string stopAt = "";
    static int outsideSeen, outsideRejected;

    static object Get(object o, string name) => o.GetType().GetField(name, F).GetValue(o);
    static void Call(object o, string name, params object[] args) => o.GetType().GetMethod(name, F).Invoke(o, args);
    static void Click(Button b) => b.onClick.Invoke();

    public static string Run(double seconds, string stop)
    {
        stopAt = stop;
        var game = Object.FindFirstObjectByType<GuildGame>();
        if (game == null) return "no game";
        Time.timeScale = 12f;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        string result = null;
        while (sw.Elapsed.TotalSeconds < seconds && result == null)
        {
            try { result = Act(game); }
            catch (Exception e) { log.AppendLine("EXC " + (e.InnerException ?? e)); result = "exception"; }
            for (int i = 0; i < 3; i++) EditorApplication.Step();
        }
        var o = log.ToString();
        log.Clear();
        return (result ?? "running") + $" (관할 밖 방문 {outsideSeen}, 거절 {outsideRejected})\n" + o;
    }

    static string Act(GuildGame game)
    {
        if (game.fader.IsPlaying || game.dayView == null) return null;
        if (game.titleView.gameObject.activeInHierarchy) { Click(game.titleView.newGameButton); return null; }
        var story = game.storyView;
        if (story.gameObject.activeInHierarchy)
        {
            if (story.choiceBox.activeSelf) Click(story.choiceButtons[0]); else Click(story.skipButton);
            return null;
        }
        if (game.promotionView.gameObject.activeInHierarchy)
        {
            var pv = game.promotionView;
            if (pv.nextButton.gameObject.activeSelf) Click(pv.nextButton); else if (pv.approveButton.interactable) Click(pv.rejectButton);
            return null;
        }
        if (game.nightView.gameObject.activeInHierarchy)
        {
            if ((bool)Get(game, "posting")) return null;
            var nv = game.nightView;
            var q = ((NightShift)Get(game, "night")).Current;
            // 도감 탭을 실제로 눌러 몬스터가 있는 카테고리를 찾는다
            var cats = (System.Collections.IList)Get(nv, "categories");
            int found = -1;
            for (int c = 0; c < cats.Count; c++)
            {
                var list = (List<MonsterData>)Get(cats[c], "Monsters");
                if (list.Contains(q.Truth)) { found = c; break; }
            }
            if (found < 0) { log.AppendLine("도감에 없는 몬스터: " + q.Truth.displayName); return "missing"; }
            Click(nv.categoryTabs[found]);
            var mons = (List<MonsterData>)Get(cats[found], "Monsters");
            Call(nv, "ShowDexPage", mons.IndexOf(q.Truth));
            Click(nv.dexSelect);
            nv.countGroup.Select((int)q.TruthCount);
            nv.rankGroup.Select((int)q.TruthRank);
            nv.roleGroup.Select((int)q.Truth.role);
            Click(nv.postButton);
            return null;
        }
        if (game.boardSetup.gameObject.activeInHierarchy)
        {
            log.Append($"[{game.boardSetup.capText.text}] ");
            Click(game.boardSetup.doneButton);
            return null;
        }
        if (game.dayView.gameObject.activeInHierarchy)
        {
            var c = (Counter)Get(game, "counter");
            if (c == null || !c.IsOpen || (bool)Get(c, "busy") || c.Current == null) return null;
            switch (c.Current)
            {
                case ApplicationVisit av:
                    string why = c.Check(av.Cards, av.Quest);
                    bool outside = why != null && why.StartsWith("관할 밖");
                    if (outside) { outsideSeen++; outsideRejected++; }
                    if (why == null) c.Approve(); else c.Reject();
                    break;
                case ReturnVisit rv:
                    var a = (Assignment)Get(rv, "assignment");
                    if (!a.ClaimsSuccess) c.Confirm(); else if (a.IsLying) c.Reject(); else c.Approve();
                    break;
                case ChatVisit:
                    if (c.NominableQuests.Count == 0 && game.dayView.statsText.text.EndsWith("귀환 대기 0")) Click(game.dayView.closeDayButton); else c.Confirm();
                    break;
                default:
                    c.Confirm();
                    break;
            }
            return null;
        }
        if (game.textPage.gameObject.activeInHierarchy)
        {
            var tp = game.textPage;
            var gd = (Guild)Get(game, "guild");
            if (tp.headingText.text.Contains("영업 결과"))
                log.AppendLine($"{tp.headingText.text}: 평판 {gd.Reputation} · 위반 {tp.bodyText.text.Split('\n').Count(l => l.Contains("규정 위반"))}");
            if (tp.headingText.text.Contains("주차를 넘겼다"))
                log.AppendLine("  " + string.Join("\n  ", tp.bodyText.text.Split('\n').Take(6)));
            if (tp.headingText.text.Contains("다섯 번의 주말")) { log.AppendLine(tp.bodyText.text.Split('\n')[0]); Click(tp.button); return "ending"; }
            if (stopAt.Length > 0 && tp.headingText.text.Contains(stopAt)) { Click(tp.button); return "stop"; }
            Click(tp.button);
        }
        return null;
    }
}
