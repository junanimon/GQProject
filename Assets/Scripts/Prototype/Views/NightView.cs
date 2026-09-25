using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 밤 책상: 의뢰서 + 길드 기입 양식 + 몬스터 도감(덮인 책을 클릭하면 펼쳐짐)
    // 대상 몬스터는 도감을 넘겨 보다가 '이 몬스터로 기입'을 눌러 정한다.
    public class NightView : MonoBehaviour
    {
        [Header("상단")]
        public Text sheetCounterText;

        [Header("의뢰서 (의뢰인 작성)")]
        public Text requestTitle;
        public Text requestMeta;
        public Text requestDescription;

        [Header("길드 기입란")]
        public Text selectedMonsterText;
        public OptionGroupView countGroup;
        public OptionGroupView rankGroup;
        public OptionGroupView roleGroup;
        public Button postButton;
        public GameObject postStampMark;

        [Header("몬스터 도감 (펼친 책)")]
        public GameObject bestiaryOpen;
        public RectTransform indexContent;
        public BestiaryIndexEntry indexPrefab;
        public Image dexPicture;
        public Text dexName;
        public Text dexFeatures;
        public Text dexTable;
        public Text dexRole;
        public Text dexEvidence;
        public Text dexPageText;
        public Button dexPrev;
        public Button dexNext;
        public Button dexSelect;

        public event Action Posted;

        int dexPage;
        IReadOnlyList<MonsterData> monsters;
        MonsterData selected;
        readonly List<BestiaryIndexEntry> index = new();

        void Awake()
        {
            foreach (var g in Groups) g.Changed += _ => Validate();
            postButton.onClick.AddListener(() =>
            {
                postButton.interactable = false;
                postStampMark.SetActive(true);
                Posted?.Invoke();
            });
            dexPrev.onClick.AddListener(() => ShowDexPage(dexPage - 1));
            dexNext.onClick.AddListener(() => ShowDexPage(dexPage + 1));
            dexSelect.onClick.AddListener(() => Select(monsters[dexPage]));
        }

        OptionGroupView[] Groups => new[] { countGroup, rankGroup, roleGroup };

        public void ShowSheet(Quest q, int sheetIndex, int total, IReadOnlyList<MonsterData> monsterList)
        {
            gameObject.SetActive(true);
            if (monsters != monsterList)
            {
                monsters = monsterList;
                BuildIndex();
            }
            sheetCounterText.text = $"오늘 밤 의뢰서 {sheetIndex + 1} / {total}";
            requestTitle.text = q.Title;
            requestMeta.text = $"의뢰인: {q.Data.client}\n지역: {q.Data.region}\n기한: {q.Deadline}일      제시 보상: {q.Reward}G";
            requestDescription.text = q.Data.description;
            foreach (var g in Groups) g.Clear();
            Select(null);
            postStampMark.SetActive(false);
            ShowDexPage(dexPage);
        }

        void BuildIndex()
        {
            for (int i = indexContent.childCount - 1; i >= 0; i--) Destroy(indexContent.GetChild(i).gameObject);
            index.Clear();
            for (int i = 0; i < monsters.Count; i++)
            {
                int page = i;
                var e = Instantiate(indexPrefab, indexContent);
                e.Bind(monsters[i].displayName, () => ShowDexPage(page));
                index.Add(e);
            }
        }

        void Select(MonsterData m)
        {
            selected = m;
            selectedMonsterText.text = m == null ? "<color=#8a7a66>(도감에서 골라 '이 몬스터로 기입')</color>" : $"<b>{m.displayName}</b>";
            Validate();
        }

        // 도장 찍힌 기입란을 읽는다
        public QuestEntry ReadEntry() => new(selected, (CountBand)countGroup.Selected, (Rank)rankGroup.Selected, (Job)roleGroup.Selected);

        void Validate() => postButton.interactable = selected != null && Groups.All(g => g.Selected >= 0);

        void ShowDexPage(int page)
        {
            int n = monsters.Count;
            dexPage = (page % n + n) % n;
            var m = monsters[dexPage];
            if (m.illustration != null) dexPicture.sprite = m.illustration;
            dexPicture.color = m.illustrationTint;
            dexName.text = m.displayName;
            dexFeatures.text = m.features;
            dexTable.text = "수량별 권장 등급\n" + string.Join("\n", Enumerable.Range(0, 3).Select(i =>
            {
                var r = m.rankByBand[i];
                return $"  {Txt.C((CountBand)i),-6}  <color={Txt.RankHex(r)}><b>{Txt.R(r)}</b></color>";
            }));
            dexRole.text = $"필요 역할: <b>{Txt.J(m.role)}</b>";
            dexEvidence.text = m.isMonster ? $"증거 부위: <b>{m.evidenceName}</b> (1마리당 1개)" : "증거: 의뢰서에 적힌 물품";
            dexPageText.text = $"{dexPage + 1} / {n}";
            for (int i = 0; i < index.Count; i++) index[i].SetCurrent(i == dexPage);
        }
    }
}
