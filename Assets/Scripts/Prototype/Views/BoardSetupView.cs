using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 밤 마무리: 내일 게시판 확인. 내가 쓴 의뢰서 + 선배가 검토한 의뢰가 전부 붙는다 (전부 처리해야 한다).
    // 여기서 밤 상점을 열 수 있다.
    public class BoardSetupView : MonoBehaviour
    {
        public RectTransform list;
        public ListRowView rowPrefab;
        [Tooltip("게시 건수 요약")]
        public Text capText;
        public Text hintText;
        public Button shopButton;
        public Button doneButton;
        public ShopView shop;

        Action done;

        void Awake()
        {
            doneButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                done?.Invoke();
            });
        }

        public void Show(List<Quest> board, Action openShop, Action finished)
        {
            gameObject.SetActive(true);
            done = finished;
            shopButton.onClick.RemoveAllListeners();
            shopButton.onClick.AddListener(() => openShop());
            hintText.text = "오늘 밤 쓴 의뢰서와 선배가 검토해 둔 의뢰(선배)가 모두 게시판에 붙었습니다.\n\n" +
                            "내일 창구에서 전부 처리해야 합니다. 기한이 지난 의뢰는 항의가 들어옵니다 (평판 −3).";
            for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);
            foreach (var q in board.OrderBy(q => q.Deadline))
            {
                var e = q.Entry;
                var row = Instantiate(rowPrefab, list);
                row.Bind((q.BySenior ? "<color=#7a6a5a>(선배)</color> " : "") + q.Title,
                    $"{Txt.Describe(e.Monster, e.Count)} · 권장 {Txt.R(e.Rank)} · 역할 {Txt.J(e.Role)} · 보상 {q.Reward}G",
                    $"기한 {q.Deadline}일", null);
                row.SetState(false, true);
            }
            capText.text = $"게시 {board.Count}건 (선배 {board.Count(q => q.BySenior)}건)";
        }
    }
}
