using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 타이틀 화면: 이어하기 · 새 게임 · 엔딩 도감 · 종료
    public class TitleView : MonoBehaviour
    {
        public Button continueButton;
        public Text continueInfo;
        public Button newGameButton;
        public Button endingBookButton;
        public Button quitButton;

        [Header("엔딩 도감 (팝업)")]
        public GameObject bookPanel;
        public RectTransform bookList;
        public ListRowView rowPrefab;
        public Text bookCountText;
        public Text bookDetail;
        public Button bookCloseButton;

        Action onContinue, onNewGame;
        IReadOnlyList<EndingData> endings;

        void Awake()
        {
            continueButton.onClick.AddListener(() => onContinue?.Invoke());
            newGameButton.onClick.AddListener(() => onNewGame?.Invoke());
            endingBookButton.onClick.AddListener(OpenBook);
            bookCloseButton.onClick.AddListener(() => bookPanel.SetActive(false));
            quitButton.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }

        public void Show(SaveData save, IReadOnlyList<EndingData> allEndings, Action continueGame, Action newGame)
        {
            gameObject.SetActive(true);
            bookPanel.SetActive(false);
            endings = allEndings;
            onContinue = continueGame;
            onNewGame = newGame;
            continueButton.interactable = save != null;
            continueInfo.text = save == null ? "저장된 기록 없음" : $"{(save.day - 1) / 4 + 1}주차 시작 · 자금 {save.funds}G · 평판 {save.reputation}\n<size=18>{save.savedAt}</size>";
        }

        public void Hide() => gameObject.SetActive(false);

        void OpenBook()
        {
            bookPanel.SetActive(true);
            for (int i = bookList.childCount - 1; i >= 0; i--) Destroy(bookList.GetChild(i).gameObject);
            int seen = 0;
            foreach (var e in endings)
            {
                bool has = SaveSystem.HasEnding(e.id);
                if (has) seen++;
                var row = Instantiate(rowPrefab, bookList);
                string kind = e.kind switch { EndingKind.Good => "굿 엔딩", EndingKind.Bitter => "씁쓸한 엔딩", _ => $"배드 엔딩 · {e.week}주차" };
                var ending = e;
                row.Bind(has ? e.title : "？？？", kind, has ? "<color=#2f7a3e>달성</color>" : "", has ? () => ShowEnding(ending) : null);
            }
            bookCountText.text = $"달성 {seen} / {endings.Count}";
            bookDetail.text = "달성한 엔딩을 누르면 마지막 장면을 다시 읽을 수 있습니다.";
        }

        void ShowEnding(EndingData e)
        {
            var lines = new List<string>();
            foreach (var l in e.lines) lines.Add(string.IsNullOrEmpty(l.speaker) ? l.text : $"<b>{l.speaker}</b>  {l.text}");
            bookDetail.text = $"<b>{e.title}</b>\n\n" + string.Join("\n\n", lines);
        }
    }
}
