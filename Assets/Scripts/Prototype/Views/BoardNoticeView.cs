using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 게시판에 붙은 의뢰 쪽지 (프리팹: Assets/Prefabs/Prototype/BoardNotice)
    public class BoardNoticeView : MonoBehaviour
    {
        public Image background;
        public Text titleText;
        public Text detailText;
        public GameObject takenMark;
        public Text takenText;
        public Color openColor = new(0.95f, 0.92f, 0.82f);
        public Color takenColor = new(0.70f, 0.66f, 0.58f);

        public void Bind(Quest q)
        {
            var e = q.Entry;
            titleText.text = q.BySenior ? $"{q.Title} <color=#7a6a5a>(선배)</color>" : q.Title;
            detailText.text =
                $"{Txt.Describe(e.Monster, e.Count)} · 권장 <color={Txt.RankHex(e.Rank)}><b>{Txt.R(e.Rank)}</b></color> · 역할 <b>{Txt.J(e.Role)}</b>\n보상 {q.Reward}G · 기한 {q.Deadline}일";
            background.color = q.IsTaken ? takenColor : openColor;
            takenMark.SetActive(q.IsTaken);
            if (q.IsTaken) takenText.text = "수주됨";
        }
    }
}
