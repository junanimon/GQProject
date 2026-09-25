using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    public class TopBarView : MonoBehaviour
    {
        public Text titleText;
        public GameObject clockBox;
        public Text clockText;
        public Image clockFill;
        public Text fundsText;
        public Text dangerText;
        public Text reputationText;

        public void Set(string title, int funds, int danger, int reputation)
        {
            titleText.text = title;
            fundsText.text = $"자금 <b>{funds}G</b>";
            dangerText.text = $"위험도 <b>{danger}</b>";
            reputationText.text = $"평판 <b>{reputation}</b>";
        }

        public void SetStats(int funds, int danger, int reputation) => Set(titleText.text, funds, danger, reputation);

        // fraction: 남은 영업 시간 비율 (1 → 0). 음수면 시계를 숨긴다.
        public void SetClock(string clock, float fraction)
        {
            clockBox.SetActive(fraction >= 0);
            clockText.text = clock;
            clockFill.fillAmount = Mathf.Clamp01(fraction);
            clockFill.color = fraction < 0.2f ? new Color(0.85f, 0.30f, 0.25f) : new Color(0.85f, 0.70f, 0.40f);
        }
    }
}
