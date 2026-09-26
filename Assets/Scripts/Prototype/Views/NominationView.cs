using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 길드 홀 (낮 팝업): 게시된 의뢰를 고르고, 홀에서 쉬고 있는 모험가를 직접 지명한다.
    // 지명 수당을 흥정할 수 있고, 한 번 지명할 때마다 영업 시간이 흐른다.
    public class NominationView : MonoBehaviour
    {
        public GameObject panel;
        public RectTransform questList;
        public RectTransform adventurerList;
        public ListRowView rowPrefab;

        [Header("제안")]
        public GameObject offerBox;
        public CharacterPresenter offerCharacter;
        public Text offerName;
        public Text offerText;
        public Button acceptButton;
        public Text acceptLabel;
        public Button haggleButton;
        public Button closeButton;
        public Text hintText;

        [Tooltip("홀에 보이는 모험가 최대 수")]
        public int maxCandidates = 8;

        Counter counter;
        Quest quest;
        Nomination current;

        void Awake()
        {
            acceptButton.onClick.AddListener(Accept);
            haggleButton.onClick.AddListener(Haggle);
            closeButton.onClick.AddListener(Close);
        }

        public void Open(Counter c)
        {
            if (c == null || !c.IsOpen) return;
            counter = c;
            quest = null;
            current = null;
            panel.SetActive(true);
            RefreshHint();
            RefreshQuests();
            Clear(adventurerList);
            ShowOffer(null);
        }

        void RefreshHint() =>
            hintText.text = $"오늘 홀에 나와 있는 모험가만 지명할 수 있습니다.  남은 지명 <b>{Mathf.Max(0, counter.NominationsLeft)}</b> / {counter.Config.nominationsPerDay}회" +
                            $"  ·  지명 한 번에 영업 시간 {counter.Config.nominationSeconds:0}초, 수당은 바로 지급";

        public void Close()
        {
            panel.SetActive(false);
            counter = null;
        }

        void RefreshQuests()
        {
            Clear(questList);
            foreach (var q in counter.NominableQuests)
            {
                var row = Instantiate(rowPrefab, questList);
                var e = q.Entry;
                row.Bind((q.IsUrgent ? "<color=#b02c28>[긴급]</color> " : "") + q.Title,
                    $"{Txt.Describe(e.Monster, e.Count)} · 권장 {Txt.R(e.Rank)} · 역할 {Txt.J(e.Role)}",
                    $"{q.Reward}G", () => SelectQuest(q));
                row.SetState(q == quest);
            }
        }

        void SelectQuest(Quest q)
        {
            quest = q;
            RefreshQuests();
            RefreshAdventurers();
            ShowOffer(null);
        }

        void RefreshAdventurers()
        {
            Clear(adventurerList);
            if (quest == null) return;
            foreach (var a in counter.NominationCandidates(maxCandidates))
            {
                var row = Instantiate(rowPrefab, adventurerList);
                string hearts = new string('♥', a.Hearts) + new string('♡', 5 - a.Hearts);
                row.Bind(a.IsNamed ? $"{a.Name} <color=#7a6a5a>「{a.Epithet}」</color>" : a.Name,
                    $"<color={Txt.RankHex(a.Rank)}><b>{Txt.R(a.Rank)}</b></color> {Txt.J(a.Job)} · Lv.{a.Level} · 실적 {a.Successes}",
                    hearts, () => Offer(a), a.Look);
                row.SetState(current != null && current.Who == a);
            }
        }

        void Offer(Adventurer a)
        {
            current = new Nomination(a, quest);
            RefreshAdventurers();
            ShowOffer(current.Offer);
        }

        void ShowOffer(string line)
        {
            offerBox.SetActive(current != null);
            if (current == null) return;
            offerCharacter.Show(current.Who.Look, current.Who.IsSick);
            offerName.text = current.Who.Name;
            offerText.text = line;
            bool can = counter.NominationsLeft > 0 && counter.CanAfford(current);
            acceptLabel.text = counter.NominationsLeft <= 0 ? "오늘 지명 끝" : !counter.CanAfford(current) ? "자금 부족" : current.Ask > 0 ? $"수락 (수당 {current.Ask}G)" : "지명";
            acceptButton.interactable = !current.WalkedOff && can;
            haggleButton.interactable = !current.WalkedOff && !current.Haggled && current.Ask > 0;
        }

        void Accept()
        {
            if (current == null || current.WalkedOff || counter == null || !counter.IsOpen) return;
            string line = counter.AcceptNomination(current);
            if (line == null) return;
            offerCharacter.Hop();
            RefreshHint();
            var done = current;
            quest = null;
            RefreshQuests();
            Clear(adventurerList);
            current = done;
            ShowOffer(line);
            acceptButton.interactable = haggleButton.interactable = false;
            current = null;
        }

        void Haggle()
        {
            if (current == null || counter == null || !counter.IsOpen) return;
            string line = counter.Haggle(current);
            if (current.WalkedOff) offerCharacter.Shake();
            ShowOffer(line);
            if (current.WalkedOff) RefreshAdventurers();
        }

        static void Clear(RectTransform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
        }
    }
}
