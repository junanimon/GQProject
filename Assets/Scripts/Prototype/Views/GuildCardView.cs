using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 모험가 길드 카드 (프리팹: Assets/Prefabs/Prototype/IDCard). 책상 위에서 끌어서 옮길 수 있다.
    public class GuildCardView : MonoBehaviour
    {
        public Image border;
        public Image portrait;
        public Text nameText;
        public Text rankText;
        public Text jobText;
        public Text numberText;
        public Image seal;
        [Tooltip("정식 길드 인장")]
        public Sprite officialSeal;
        [Tooltip("위조 인장")]
        public Sprite fakeSeal;

        public void Bind(GuildCard c)
        {
            gameObject.SetActive(true);
            border.color = c.Border;
            if (c.Portrait.portrait != null) portrait.sprite = c.Portrait.portrait;
            portrait.color = c.Portrait.tint;
            nameText.text = c.Name;
            rankText.text = $"등급   <b>{Txt.R(c.ShownRank)}</b>";
            jobText.text = $"직업   <b>{Txt.J(c.Job)}</b>";
            numberText.text = $"No. {c.Number}";
            seal.sprite = c.FakeSeal ? fakeSeal : officialSeal;
        }
    }
}
