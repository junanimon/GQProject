using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 밤 승급 심사: 신청자의 길드 카드와 실적표를 보고 승급 / 보류 도장을 찍는다
    public class PromotionView : MonoBehaviour
    {
        public Text headerText;
        public CharacterPresenter character;
        public Text nameText;
        public GuildCardView card;
        public Text recordText;
        public Text criteriaText;
        public TypewriterText speech;
        public Button approveButton;
        public Button rejectButton;
        public GameObject approvedMark;
        public GameObject rejectedMark;
        public Button nextButton;

        Func<bool, string> decide;
        Action next;

        void Awake()
        {
            approveButton.onClick.AddListener(() => Decide(true));
            rejectButton.onClick.AddListener(() => Decide(false));
            nextButton.onClick.AddListener(() => next?.Invoke());
        }

        public void Show(Adventurer a, PromotionBoard board, int index, int total, Func<bool, string> onDecide, Action onNext)
        {
            gameObject.SetActive(true);
            decide = onDecide;
            next = onNext;
            headerText.text = $"승급 심사 {index + 1} / {total}";
            character.Show(a.Look, a.IsSick);
            nameText.text = a.IsNamed ? $"{a.Name}  <size=22>「{a.Epithet}」</size>" : a.Name;
            card.Bind(GuildCard.Of(a));
            string target = Txt.R(a.Rank + 1);
            int needLv = board.NeedLevel(a), needRec = board.NeedRecord(a);
            recordText.text =
                $"현재 등급  <color={Txt.RankHex(a.Rank)}><b>{Txt.R(a.Rank)}</b></color>  →  신청 <color={Txt.RankHex(a.Rank + 1)}><b>{target}</b></color>\n\n" +
                $"레벨   <b>{a.Level}</b>  {Mark(a.Level >= needLv)}\n" +
                $"완료 실적   <b>{a.Successes}건</b>  {Mark(a.Successes >= needRec)}";
            criteriaText.text = $"<b>{target} 승급 기준</b>\n레벨 {needLv} 이상 <b>그리고</b> 완료 실적 {needRec}건 이상\n\n<color=#7a6a5a>기준 미달인데 승급시키면 카드 등급만 오르고 실력은 그대로다.</color>";
            speech.Play(a.Line(LineKind.Greeting, new[] { "저… 승급 심사 부탁드려요. 이번엔 자신 있어요!" }));
            approvedMark.SetActive(false);
            rejectedMark.SetActive(false);
            approveButton.interactable = rejectButton.interactable = true;
            nextButton.gameObject.SetActive(false);
        }

        static string Mark(bool ok) => ok ? "<color=#2f7a3e>✔ 충족</color>" : "<color=#b02c28>✘ 미달</color>";

        void Decide(bool approve)
        {
            approveButton.interactable = rejectButton.interactable = false;
            approvedMark.SetActive(approve);
            rejectedMark.SetActive(!approve);
            speech.Play(decide(approve));
            if (approve) character.Hop();
            else character.Shake();
            nextButton.gameObject.SetActive(true);
        }
    }
}
