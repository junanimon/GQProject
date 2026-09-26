using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 수배서 한 장 (프리팹: Assets/Prefabs/Prototype/WantedPoster)
    public class WantedPosterView : MonoBehaviour
    {
        public Image portrait;
        public Text nameText;
        public Text aliasText;
        public Text featuresText;
        public Text bountyText;

        public void Bind(WantedPoster p)
        {
            if (p.look.portrait != null) portrait.sprite = p.look.portrait;
            portrait.color = p.look.tint;
            nameText.text = p.name;
            aliasText.text = $"가명: {p.alias}";
            featuresText.text = p.features;
            bountyText.text = $"현상금 {p.bounty}G";
        }
    }
}
