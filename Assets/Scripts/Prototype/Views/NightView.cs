using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 밤 책상: 의뢰서 + 길드 기입 양식 + 몬스터 도감(덮인 책을 클릭하면 펼쳐짐)
    // 도감은 지역별 카테고리(탭)로 나뉜다. 대상 몬스터는 도감을 넘겨 보다가 '이 몬스터로 기입'을 눌러 정한다.
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

        [Header("도감 카테고리 (지역 탭 + 마지막 '기타')")]
        public List<Button> categoryTabs = new();
        public List<Text> categoryLabels = new();
        public Text categoryInfo;
        public Color tabNormal = new(0.55f, 0.40f, 0.28f);
        public Color tabCurrent = new(0.72f, 0.18f, 0.16f);
        public Color tabLocked = new(0.35f, 0.30f, 0.28f);

        public event Action Posted;

        // 카테고리 한 칸: 지역(없으면 '기타') + 몬스터 목록
        class Category
        {
            public RegionData Region;
            public List<MonsterData> Monsters;
            public bool Managed;
        }

        readonly List<Category> categories = new();
        readonly List<BestiaryIndexEntry> index = new();
        int category, dexPage;
        MonsterData selected;

        List<MonsterData> Monsters => categories.Count == 0 ? new List<MonsterData>() : categories[category].Monsters;

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
            dexSelect.onClick.AddListener(() => Select(Monsters[dexPage]));
            for (int i = 0; i < categoryTabs.Count; i++)
            {
                int c = i;
                categoryTabs[i].onClick.AddListener(() => ShowCategory(c, 0));
            }
        }

        OptionGroupView[] Groups => new[] { countGroup, rankGroup, roleGroup };

        // regions: 전체 지역 (관할 밖도 도감엔 실려 있다), allMonsters: 지역에 없는 것(해당 없음 등)은 '기타'로
        public void ShowSheet(Quest q, int sheetIndex, int total, IReadOnlyList<RegionData> regions, int guildRank,
            IReadOnlyList<MonsterData> allMonsters)
        {
            gameObject.SetActive(true);
            BuildCategories(regions, guildRank, allMonsters);
            sheetCounterText.text = $"오늘 밤 의뢰서 {sheetIndex + 1} / {total}";
            requestTitle.text = q.Title;
            requestMeta.text = $"의뢰인: {q.Data.client}\n지역: {q.Data.region}\n기한: {q.Deadline}일      제시 보상: {q.Reward}G";
            requestDescription.text = q.Data.description;
            foreach (var g in Groups) g.Clear();
            Select(null);
            postStampMark.SetActive(false);
            ShowCategory(Mathf.Min(category, categories.Count - 1), dexPage);
        }

        void BuildCategories(IReadOnlyList<RegionData> regions, int guildRank, IReadOnlyList<MonsterData> allMonsters)
        {
            categories.Clear();
            foreach (var r in regions)
                categories.Add(new Category { Region = r, Monsters = r.monsters.Where(m => m != null).ToList(), Managed = r.unlockRank <= guildRank });
            var listed = new HashSet<MonsterData>(categories.SelectMany(c => c.Monsters));
            categories.Add(new Category { Monsters = allMonsters.Where(m => m != null && !listed.Contains(m)).ToList(), Managed = true });

            for (int i = 0; i < categoryTabs.Count; i++)
            {
                bool on = i < categories.Count;
                categoryTabs[i].gameObject.SetActive(on);
                if (!on) continue;
                var c = categories[i];
                categoryLabels[i].text = c.Region == null ? "기타" : c.Managed ? c.Region.displayName : $"{c.Region.displayName}\n<size=13>(관할 밖)</size>";
            }
        }

        void ShowCategory(int c, int page)
        {
            category = Mathf.Clamp(c, 0, categories.Count - 1);
            for (int i = 0; i < categoryTabs.Count && i < categories.Count; i++)
                categoryTabs[i].image.color = i == category ? tabCurrent : categories[i].Managed ? tabNormal : tabLocked;
            var cat = categories[category];
            categoryInfo.text = cat.Region == null
                ? "몬스터가 없는 잡무 · 채집 의뢰"
                : (cat.Managed ? "" : "<color=#b02c28>관할 밖 지역</color> — ") + cat.Region.description;
            BuildIndex();
            ShowDexPage(page);
        }

        void BuildIndex()
        {
            for (int i = indexContent.childCount - 1; i >= 0; i--) Destroy(indexContent.GetChild(i).gameObject);
            index.Clear();
            var list = Monsters;
            for (int i = 0; i < list.Count; i++)
            {
                int page = i;
                var e = Instantiate(indexPrefab, indexContent);
                e.Bind(list[i].displayName, () => ShowDexPage(page));
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
            var list = Monsters;
            int n = list.Count;
            dexSelect.interactable = n > 0;
            if (n == 0)
            {
                dexName.text = "기록 없음";
                dexFeatures.text = "이 지역의 몬스터 기록이 아직 없다.";
                dexTable.text = dexRole.text = dexEvidence.text = "";
                dexPicture.color = new Color(1, 1, 1, 0.15f);
                dexPageText.text = "0 / 0";
                return;
            }
            dexPage = (page % n + n) % n;
            var m = list[dexPage];
            if (m.illustration != null) dexPicture.sprite = m.illustration;
            dexPicture.color = m.illustrationTint;
            dexName.text = m.displayName;
            dexFeatures.text = m.features;
            dexTable.text = !m.isMonster ? "" : "수량별 권장 등급\n" + string.Join("\n", Enumerable.Range(0, 3).Select(i =>
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
