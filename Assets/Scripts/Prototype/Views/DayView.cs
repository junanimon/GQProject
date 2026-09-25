using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 낮 창구
    //  왼쪽: 크게 선 모험가 + 이름표(칭호 · 호감도) + 대화창
    //  오른쪽: 책상 (길드 카드 · 의뢰서/보고서 · 증거물은 끌어서 옮김, 규정집 · 게시판은 클릭해서 펼침, 도장)
    public class DayView : MonoBehaviour
    {
        public enum Mode { QuestReview, ReportReview, Confirm, Waiting }

        [Header("모험가")]
        public CharacterPresenter character;
        public Text characterName;
        public Text epithetText;
        public AffinityHearts hearts;
        public TypewriterText speech;
        public Text visitTypeText;
        public Text queueCountText;
        public Text boardCountText;

        [Header("책상: 놓이는 위치 (빈 오브젝트를 옮기면 서류가 나오는 자리가 바뀜)")]
        public List<RectTransform> cardSpawns = new();
        public RectTransform docSpawn;
        public RectTransform evidenceSpawn;

        [Header("책상: 길드 카드 (파티원 수만큼 켜짐)")]
        public List<GuildCardView> cards = new();

        [Header("책상: 의뢰서")]
        public DraggableDocument questDoc;
        public Text questTitle;
        public Text questEntries;
        public Text questPartyText;
        public GameObject questApprovedMark;
        public GameObject questRejectedMark;

        [Header("책상: 완료 보고서")]
        public DraggableDocument reportDoc;
        public Text reportTitle;
        public Text reportClaim;
        public Text reportPosted;
        public GameObject reportApprovedMark;
        public GameObject reportRejectedMark;

        [Header("책상: 증거물")]
        public DraggableDocument evidenceItem;
        public Image evidenceImage;
        public Text evidenceLabel;

        [Header("규정집 (펼친 책)")]
        public Text rulesText;
        public GameObject officialSealSample;

        [Header("게시판 (팝업)")]
        public RectTransform boardContent;
        public BoardNoticeView noticePrefab;

        [Header("도장 / 버튼")]
        public Button approveButton;
        public Text approveLabel;
        public Button rejectButton;
        public Text rejectLabel;
        public Button confirmButton;
        public Text confirmLabel;
        public Button closeDayButton;
        public Text statsText;

        public void SetRules(IReadOnlyList<IForgery> unlocked, IForgery newest, IReadOnlyList<MonsterData> monsters)
        {
            var lines = new List<string>(Regulations.BaseLines);
            foreach (var f in unlocked)
                lines.Add(f.RuleLine + (f == newest ? "  <color=#b02c28><b>[신규]</b></color>" : ""));
            lines.Add(Regulations.CompletionLine);
            string table = "<b>증거 부위표</b>";
            foreach (var m in monsters) if (m.isMonster) table += $"\n  {m.displayName} → {m.evidenceName}";
            lines.Add(table);
            rulesText.text = string.Join("\n\n", lines);
            officialSealSample.SetActive(unlocked.Any(f => f is SealForgery));
        }

        public void ShowVisitor(GuildCard leader, string line, string visitType)
        {
            var person = leader.Holder;
            character.Show(person.Look);
            characterName.text = leader.Name;
            epithetText.text = person.IsNamed ? $"「{person.Epithet}」" : $"{Txt.R(person.Rank)}급 {Txt.J(person.Job)}";
            hearts.Set(person.Affinity);
            visitTypeText.text = visitType;
            Say(line);
        }

        public void Say(string line) => speech.Play(line);

        public void CharacterHappy() => character.Hop();
        public void CharacterUpset() => character.Shake();

        public void SetQueue(int count) => queueCountText.text = count > 0 ? $"대기 {count}명" : "대기 없음";

        public void RefreshBoard(IReadOnlyList<Quest> quests)
        {
            for (int i = boardContent.childCount - 1; i >= 0; i--) Destroy(boardContent.GetChild(i).gameObject);
            foreach (var q in quests) Instantiate(noticePrefab, boardContent).Bind(q);
            boardCountText.text = $"남은 의뢰 {quests.Count(q => !q.IsTaken)}건";
        }

        public void SetCards(IReadOnlyList<GuildCard> list)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                bool on = i < list.Count;
                cards[i].gameObject.SetActive(on);
                if (!on) continue;
                cards[i].Bind(list[i]);
                cards[i].GetComponent<DraggableDocument>().Present(cardSpawns[Mathf.Min(i, cardSpawns.Count - 1)]);
            }
        }

        public void ShowQuestDoc(Quest q, int partySize)
        {
            reportDoc.gameObject.SetActive(false);
            evidenceItem.gameObject.SetActive(false);
            var e = q.Entry;
            questTitle.text = q.Title;
            string monster = e.Monster.isMonster ? $"{e.Monster.displayName}  ({Txt.C(e.Count)})" : "몬스터 없음";
            questEntries.text =
                $"대상: {monster}\n권장 등급: <color={Txt.RankHex(e.Rank)}><b>{Txt.R(e.Rank)}</b></color>\n필수 역할: <b>{Txt.J(e.Role)}</b>\n보상 {q.Reward}G · 기한 {q.Deadline}일";
            questPartyText.text = $"신청 인원: {partySize}명";
            questApprovedMark.SetActive(false);
            questRejectedMark.SetActive(false);
            questDoc.Present(docSpawn);
        }

        public void StampQuest(bool approved)
        {
            questApprovedMark.SetActive(approved);
            questRejectedMark.SetActive(!approved);
        }

        // 귀환 보고: 보고서(주장 + 게시본 기입) + 책상 위 증거물
        public void ShowReport(Assignment a, Sprite proofIcon)
        {
            questDoc.gameObject.SetActive(false);
            var q = a.Quest;
            var ev = a.Evidence;
            reportTitle.text = q.Title;
            reportClaim.text = a.ClaimsSuccess
                ? $"보고: <b>의뢰 완료</b>\n제출 증거: {(ev == null ? "없음" : ev.ToString())}"
                : "보고: <color=#b02c28><b>의뢰 실패</b></color>\n제출 증거: 없음";
            reportPosted.text = q.Entry.Monster.isMonster
                ? $"게시본 기입: {Txt.Describe(q.Entry.Monster, q.Entry.Count)}"
                : $"게시본 기입: 몬스터 없음 · 확인 물품 '{q.Proof}'";
            reportApprovedMark.SetActive(false);
            reportRejectedMark.SetActive(false);
            reportDoc.Present(docSpawn);

            bool has = a.ClaimsSuccess && ev != null;
            evidenceItem.gameObject.SetActive(has);
            if (!has) return;
            var icon = ev.Source != null ? ev.Source.evidenceIcon : proofIcon;
            if (icon != null) evidenceImage.sprite = icon;
            evidenceImage.color = ev.Source != null ? ev.Source.evidenceTint : Color.white;
            evidenceLabel.text = $"{ev.Item}\n× {ev.Count}";
            evidenceItem.Present(evidenceSpawn);
        }

        public void StampReport(bool approved)
        {
            reportApprovedMark.SetActive(approved);
            reportRejectedMark.SetActive(!approved);
        }

        public void HideDocuments()
        {
            questDoc.gameObject.SetActive(false);
            reportDoc.gameObject.SetActive(false);
            evidenceItem.gameObject.SetActive(false);
        }

        public void SetMode(Mode mode, string confirmText = "")
        {
            bool confirm = mode == Mode.Confirm;
            approveButton.gameObject.SetActive(!confirm);
            rejectButton.gameObject.SetActive(!confirm);
            approveButton.interactable = rejectButton.interactable = mode is Mode.QuestReview or Mode.ReportReview;
            if (mode == Mode.QuestReview) { approveLabel.text = "수주\n승인"; rejectLabel.text = "수주\n거절"; }
            if (mode == Mode.ReportReview) { approveLabel.text = "완료\n승인"; rejectLabel.text = "보고\n반려"; }
            confirmButton.gameObject.SetActive(confirm);
            confirmButton.interactable = true;
            confirmLabel.text = confirmText;
        }

        public void SetStats(int approved, int rejected, int returnsWaiting) =>
            statsText.text = $"승인 {approved} · 반려 {rejected} · 귀환 대기 {returnsWaiting}";
    }
}
