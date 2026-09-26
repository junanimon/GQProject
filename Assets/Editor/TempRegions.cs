using System.Collections.Generic;
using System.Linq;
using GuildProto;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// 임시: 지역 · 길드 등급 · 도감 카테고리 씬 구성과 지역별 몬스터 데이터 (작업이 끝나면 삭제)
public static class TempRegions
{
    const string RegionDir = "Assets/Data/Regions";
    const string MonsterDir = "Assets/Data/Monsters";
    static Font font;

    static GameDatabase Db => AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Data/GameDatabase.asset");
    static MonsterData M(string id) => AssetDatabase.LoadAssetAtPath<MonsterData>($"{MonsterDir}/{id}.asset");
    static RegionData R(string id) => AssetDatabase.LoadAssetAtPath<RegionData>($"{RegionDir}/{id}.asset");

    // ───────── 씬 ─────────

    public static string BuildScene()
    {
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/malgun.ttf");
        var night = EditorSceneManager.OpenScene("Assets/Scenes/Night.unity", OpenSceneMode.Additive);
        var day = EditorSceneManager.OpenScene("Assets/Scenes/Day.unity", OpenSceneMode.Additive);

        // 도감 카테고리 탭: 책 위로 튀어나온 책갈피 6개 + 페이지 맨 위 설명 한 줄
        var nv = night.GetRootGameObjects()[0].GetComponentInChildren<NightView>(true);
        var book = (RectTransform)nv.bestiaryOpen.transform;
        var old = book.Find("CategoryTabs");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var strip = Rect("CategoryTabs", book, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 30), new Vector2(-16, 60));
        strip.pivot = new Vector2(0.5f, 0);
        var hlg = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;
        nv.categoryTabs.Clear();
        nv.categoryLabels.Clear();
        for (int i = 0; i < 6; i++)
        {
            var tab = Rect($"Tab{i + 1}", strip, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var img = tab.gameObject.AddComponent<Image>();
            img.color = nv.tabNormal;
            tab.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.5f);
            var btn = tab.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            tab.gameObject.AddComponent<UIHover>();
            tab.gameObject.AddComponent<ButtonPunch>();
            var label = Label(Stretch("Label", tab, 4, 2, 4, 2), i < 5 ? $"지역 {i + 1}" : "기타", 16, new Color(0.95f, 0.92f, 0.85f));
            nv.categoryTabs.Add(btn);
            nv.categoryLabels.Add(label);
        }
        var page = book.Find("Page");
        var oldInfo = page.Find("CategoryInfo");
        if (oldInfo != null) Object.DestroyImmediate(oldInfo.gameObject);
        var info = Rect("CategoryInfo", page, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(100, 40));
        info.SetSiblingIndex(0);
        var le = info.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = le.minHeight = 44;
        nv.categoryInfo = Label(info, "", 15, new Color(0.42f, 0.36f, 0.30f), TextAnchor.MiddleLeft);
        EditorUtility.SetDirty(nv);

        // 규정집: 인장 견본 한 개 → 관할 지역 마크 견본 줄
        var dv = day.GetRootGameObjects()[0].GetComponentInChildren<DayView>(true);
        var sealSample = dv.officialSealSample.transform;
        var rulePage = sealSample.parent;
        int at = sealSample.GetSiblingIndex();
        var markSprite = dv.cards[0].officialSeal;
        if (sealSample.name != "RegionMarks") Object.DestroyImmediate(sealSample.gameObject);
        else Object.DestroyImmediate(sealSample.gameObject);
        var row = Rect("RegionMarks", rulePage, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(100, 100));
        row.SetSiblingIndex(at);
        var rle = row.gameObject.AddComponent<LayoutElement>();
        rle.preferredHeight = rle.minHeight = 104;
        var rh = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rh.spacing = 6;
        rh.padding = new RectOffset(8, 8, 4, 4);
        rh.childAlignment = TextAnchor.MiddleLeft;
        rh.childControlWidth = rh.childControlHeight = true;
        rh.childForceExpandWidth = false;
        rh.childForceExpandHeight = true;
        dv.regionMarkSamples.Clear();
        dv.regionMarkLabels.Clear();
        for (int i = 0; i < 5; i++)
        {
            var slot = Rect($"Mark{i + 1}", row, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var sle = slot.gameObject.AddComponent<LayoutElement>();
            sle.preferredWidth = sle.minWidth = 120;
            var mark = Rect("Mark", slot, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(60, 60));
            var mi = mark.gameObject.AddComponent<Image>();
            mi.sprite = markSprite;
            mi.preserveAspect = true;
            var lab = Rect("Label", slot, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 16), new Vector2(0, 30));
            var lt = Label(lab, $"지역 {i + 1}", 15, new Color(0.18f, 0.14f, 0.10f));
            dv.regionMarkSamples.Add(mi);
            dv.regionMarkLabels.Add(lt);
        }
        dv.officialSealSample = row.gameObject;
        EditorUtility.SetDirty(dv);

        EditorSceneManager.MarkSceneDirty(night);
        EditorSceneManager.MarkSceneDirty(day);
        EditorSceneManager.SaveScene(night);
        EditorSceneManager.SaveScene(day);
        EditorSceneManager.CloseScene(night, true);
        EditorSceneManager.CloseScene(day, true);
        return "scene ok";
    }

    static RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static RectTransform Stretch(string name, Transform parent, float l, float b, float r, float t)
    {
        var rt = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
        return rt;
    }

    static Text Label(RectTransform rt, string text, int size, Color c, TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = c;
        t.alignment = anchor;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    // ───────── 지역 뼈대 (5곳) ─────────

    public static string BuildRegions()
    {
        if (!AssetDatabase.IsValidFolder(RegionDir)) AssetDatabase.CreateFolder("Assets/Data", "Regions");
        var db = Db;
        var mark = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Prototype/seal_official.png").OfType<Sprite>().FirstOrDefault();
        db.regions = new List<RegionData>
        {
            Region("outskirts", "로웰 근교", 1, 0, new Color(0.45f, 0.62f, 0.32f), mark,
                "로웰 마을과 밭, 개울, 숲 가장자리와 옛 채석장. 길드가 처음부터 맡아 온 곳.", ""),
            Region("forest", "속삭임 숲", 2, 3, new Color(0.20f, 0.50f, 0.38f), mark,
                "북쪽의 깊은 숲. 바람이 없어도 나무가 수군거린다고 한다. 버섯과 벌레, 숲의 짐승이 많다.",
                "숲 입구 벌목촌 '파인홀로우'가 길드 관할에 들어왔다."),
            Region("mine", "회색 광산", 3, 3, new Color(0.50f, 0.52f, 0.60f), mark,
                "동쪽 구릉의 폐광과 채석장. 어둠 속 짐승과 돌을 먹는 것들이 산다.",
                "광산 마을 '그레이록'이 의뢰를 맡겨 오기 시작했다."),
            Region("swamp", "안개 늪지", 4, 3, new Color(0.45f, 0.38f, 0.60f), mark,
                "남쪽 강 하구의 늪. 안개가 걷히지 않고, 물속과 안개 속 둘 다 조심해야 한다.",
                "늪가 어촌 '미스트워터'가 관할에 들어왔다."),
            Region("snowfield", "서리봉 설원", 5, 4, new Color(0.50f, 0.75f, 0.90f), mark,
                "북동쪽 설산 기슭. 추위와 큰 짐승, 하늘을 나는 것들의 땅. 가장 위험한 지역.",
                "설산 초소 '프로스트게이트'가 길드 관할에 들어왔다."),
        };

        // 축제 협력 지역: 길드 이름 → 지역 이름 (가짜는 있지도 않은 지역)
        var w2 = db.weeks[1];
        w2.partnerRegions = new List<string> { "안개 늪지", "서리봉 설원" };
        w2.fakeRegions = new List<string> { "황금 모래 사막", "은빛 항구", "동쪽 섬 연합" };
        EditorUtility.SetDirty(w2);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        return "regions " + db.regions.Count;
    }

    static RegionData Region(string id, string name, int rank, int newcomers, Color tint, Sprite mark, string desc, string note)
    {
        string path = $"{RegionDir}/{id}.asset";
        var r = AssetDatabase.LoadAssetAtPath<RegionData>(path);
        if (r == null)
        {
            r = ScriptableObject.CreateInstance<RegionData>();
            AssetDatabase.CreateAsset(r, path);
        }
        r.displayName = name;
        r.unlockRank = rank;
        r.newcomers = newcomers;
        r.markTint = tint;
        r.mark = mark;
        r.description = desc;
        r.unlockNote = note;
        EditorUtility.SetDirty(r);
        return r;
    }

    // ───────── 몬스터 만들기 도우미 ─────────

    static Sprite Art(string archetype) =>
        AssetDatabase.LoadAllAssetsAtPath($"Assets/Art/Monsters/{archetype}.png").OfType<Sprite>().FirstOrDefault();

    // ranks: "BSS" 처럼 수량 구간별 등급 (B=동 S=은 G=금)
    static MonsterData Monster(string id, string name, string talk, string archetype, Color tint, string ranks, Job role,
        string evidence, string iconFrom, string features, string countTrace, string[] clues, string[] titles)
    {
        string path = $"{MonsterDir}/{id}.asset";
        var m = AssetDatabase.LoadAssetAtPath<MonsterData>(path);
        if (m == null)
        {
            m = ScriptableObject.CreateInstance<MonsterData>();
            AssetDatabase.CreateAsset(m, path);
        }
        m.id = id;
        m.displayName = name;
        m.talkName = talk;
        m.features = features;
        m.illustration = Art(archetype);
        m.illustrationTint = tint;
        m.evidenceName = evidence;
        var src = M(iconFrom);
        if (src != null) m.evidenceIcon = src.evidenceIcon;
        m.evidenceTint = tint;
        m.clues = clues;
        m.questTitles = titles;
        m.countTrace = countTrace;
        m.rankByBand = ranks.Select(c => c == 'G' ? Rank.Gold : c == 'S' ? Rank.Silver : Rank.Bronze).ToArray();
        m.role = role;
        m.isMonster = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    static void LookAlike(string a, string b)
    {
        M(a).lookAlike = M(b);
        EditorUtility.SetDirty(M(a));
    }

    static string SetRegion(string regionId, params string[] monsterIds)
    {
        var db = Db;
        var r = R(regionId);
        r.monsters = monsterIds.Select(M).ToList();
        foreach (var m in r.monsters.Where(m => !db.monsters.Contains(m))) db.monsters.Add(m);
        // 'none'(해당 없음)은 목록 맨 끝에 둔다
        var none = db.monsters.FirstOrDefault(m => !m.isMonster);
        if (none != null) { db.monsters.Remove(none); db.monsters.Add(none); }
        EditorUtility.SetDirty(r);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        return $"{r.displayName}: {string.Join(", ", r.monsters.Select(m => m.displayName))} ({r.monsters.Count})";
    }

    // ───────── 1. 로웰 근교 ─────────

    public static string Region1()
    {
        Monster("boar", "돌격 멧돼지", "멧돼지", "beast", new Color(0.55f, 0.38f, 0.25f), "BSS", Job.Warrior,
            "멧돼지 엄니", "dog",
            "밭을 파헤쳐 뿌리와 감자를 먹는다. 화가 나면 머리를 숙이고 곧장 돌진한다. 엄니가 아래턱에서 위로 휘어 있다. " +
            "정면으로 받으면 방패가 쪼개질 정도라, 앞에서 버텨 줄 전사가 필요하다.",
            "갈라진 발굽 자국이 {n}줄로 이어져 있었다",
            new[]
            {
                "감자밭이 밤새 쟁기질한 것처럼 뒤집혀 있어요.",
                "울타리 말뚝이 한쪽으로 쓰러져 있고, 진흙에 뻣뻣한 갈색 털이 붙어 있었어요.",
                "꿀꿀거리는 소리가 나더니 뭔가 무서운 속도로 지나갔어요.",
                "나무 밑동에 뭔가로 긁은 자국이 무릎 높이에 나 있어요.",
            },
            new[] { "감자밭이 엉망이에요", "밭을 갈아엎는 녀석", "울타리를 들이받는 괴물" });
        LookAlike("boar", "dog");
        return SetRegion("outskirts", "dog", "rat", "frog", "toad", "slime", "goblin", "wolf", "kobold", "harpy", "boar");
    }

    // ───────── 2. 속삭임 숲 ─────────
    // 헷갈리는 짝: 흑곰 ↔ 부엉이곰 / 독나방 ↔ 도깨비불(밤에 떠다니는 빛) / 트렌트 묘목 ↔ 마이코니드(걷는 식물) / 숲거미 ↔ 대왕 뱀(독니)

    public static string Region2()
    {
        Monster("spider", "숲거미", "숲거미", "blob", new Color(0.28f, 0.22f, 0.32f), "BSS", Job.Mage,
            "거미 독니", "dog",
            "나무 사이에 사람 키만 한 거미줄을 친다. 몸통이 개만 하고 다리털이 뻣뻣하다. 줄에 걸린 것은 고치로 싸 둔다. " +
            "칼로는 거미줄이 잘 안 끊겨서 불로 태워 길을 여는 마법사가 필요하다.",
            "나무 사이 거미줄이 {n}겹 쳐져 있었다",
            new[]
            {
                "숲길 나무 사이에 하얀 천 같은 게 쳐져 있어요. 끈적해서 손이 안 떨어져요.",
                "나뭇가지에 사람 팔뚝만 한 하얀 뭉치가 매달려 있었어요. 안에서 뭔가 꿈틀했어요.",
                "딸깍딸깍, 나무 위에서 뭔가 이빨 부딪치는 소리가 나요.",
                "사냥개가 숲에 들어가더니 다리를 절면서 나왔어요. 물린 자리가 보랏빛으로 부었어요.",
            },
            new[] { "숲길이 하얀 줄로 막혔어요", "나무 위의 하얀 뭉치", "사냥개가 물려 왔어요" });

        Monster("moth", "독나방", "독나방", "bird", new Color(0.72f, 0.82f, 0.45f), "BBS", Job.Archer,
            "나방 날개", "harpy",
            "손바닥 두 개만 한 날개에 연두색 가루가 묻어 있다. 밤에 불빛을 보고 모여들고, 날개가루가 희미하게 빛난다. " +
            "가루를 마시면 기침과 두드러기가 난다. 높이 날아다녀 활로 떨어뜨려야 한다. 낮에는 나무껍질에 붙어 잔다.",
            "등불 주위에 {n}마리쯤 맴도는 게 보였다",
            new[]
            {
                "밤에 등불을 켜 두면 초록빛 가루를 흘리는 게 잔뜩 몰려와요.",
                "빨래에 연두색 가루가 묻어 있어서 입었더니 온몸이 가려워요.",
                "파닥파닥 소리가 나고, 희미하게 빛나는 게 창문에 부딪혀요.",
                "낮에 보니 나무껍질에 커다란 날개 달린 게 붙어서 꼼짝도 안 하더라고요.",
            },
            new[] { "등불에 몰려드는 초록 불빛", "빨래에 묻은 가루", "밤마다 창문을 두드려요" });

        Monster("wisp", "도깨비불", "도깨비불", "blob", new Color(0.55f, 0.90f, 1.0f), "SSG", Job.Mage,
            "꺼진 불씨 돌", "slime",
            "주먹만 한 푸른 불이 사람 허리 높이에서 떠다닌다. 소리가 전혀 없고, 따라가면 길을 잃는다. 날개도 가루도 없다. " +
            "칼과 화살이 그냥 통과해서 마법으로만 꺼뜨릴 수 있다. 꺼지면 따뜻한 돌멩이가 남는다.",
            "푸른 불빛이 {n}개 떠 있었다",
            new[]
            {
                "숲 속에서 푸른 불빛이 흔들흔들 떠다녀요. 아무 소리도 안 나요.",
                "불빛을 따라갔던 나무꾼이 사흘 만에 엉뚱한 골짜기에서 발견됐어요.",
                "돌을 던졌는데 그냥 불빛을 통과해 버렸어요.",
                "불빛이 지나간 자리 풀잎에 서리가 끼어 있었어요. 여름인데요.",
            },
            new[] { "숲에서 떠도는 푸른 불", "나무꾼이 길을 잃었어요", "소리 없는 불빛" });

        Monster("bear", "흑곰", "흑곰", "beast", new Color(0.22f, 0.20f, 0.20f), "SGG", Job.Warrior,
            "곰 발톱", "troll",
            "검은 털의 큰 곰. 낮에 돌아다니며 벌통과 과실나무를 턴다. 나무에 등을 비비고 발톱 자국을 높게 남긴다. " +
            "울음소리는 낮고 굵다. 힘으로 버텨 줄 전사가 있어야 한다. 깃털은 없다.",
            "진흙에 큰 발바닥 자국이 {n}쌍 찍혀 있었다",
            new[]
            {
                "벌통이 통째로 쓰러져 꿀이 다 없어졌어요. 대낮에요!",
                "사과나무 가지가 부러지고, 나무껍질에 사람 키보다 높이 긁힌 자국이 있어요.",
                "낮게 우르릉 하는 소리를 들었어요. 까만 털이 가시덤불에 걸려 있었고요.",
                "발바닥 자국이 제 얼굴만 해요. 발가락 끝에 긴 발톱 자국도 있어요.",
            },
            new[] { "벌통이 털렸어요", "과수원의 검은 그림자", "대낮의 큰 발자국" });

        Monster("owlbear", "부엉이곰", "부엉이곰", "beast", new Color(0.66f, 0.56f, 0.40f), "SGG", Job.Archer,
            "부엉이곰 깃털", "harpy",
            "곰의 몸에 부엉이 머리를 한 짐승. 밤에만 사냥하고 '부우—' 하고 운다. 털 사이에 갈색 깃털이 섞여 있다. " +
            "눈이 밝아 가까이 가기 전에 들키므로, 멀리서 쏘는 궁수가 필요하다. 흑곰과 달리 벌통엔 관심이 없고 가축을 노린다.",
            "깃털 섞인 털 뭉치가 {n}군데 떨어져 있었다",
            new[]
            {
                "한밤중에 '부우— 부우—' 하는 소리가 나더니 염소가 없어졌어요.",
                "외양간 문에 큰 발톱 자국이 있고, 바닥에 갈색 깃털이 떨어져 있었어요.",
                "달빛에 봤는데, 곰처럼 큰데 머리가 둥글고 눈이 번쩍였어요.",
                "낮에는 아무것도 없어요. 꼭 밤에만 와요.",
            },
            new[] { "한밤중에 가축이 사라져요", "부엉이 소리가 나는 곰", "외양간의 깃털" });

        Monster("hornrabbit", "뿔토끼", "뿔토끼", "rat", new Color(0.92f, 0.90f, 0.85f), "BBS", Job.None,
            "토끼 뿔", "kobold",
            "이마에 작은 뿔이 난 흰 토끼. 떼로 몰려와 채소밭 잎을 갉는다. 겁이 많지만 궁지에 몰리면 뿔로 찌른다. " +
            "쥐와 달리 꼬리가 짧고 뒷다리로 뛴다. 누구나 잡을 수 있지만 수가 많으면 손이 모자란다.",
            "뒷발로 뛴 자국이 {n}줄 나 있었다",
            new[]
            {
                "양배추 잎이 싹 갉아 먹혔어요. 줄기만 남았어요.",
                "하얀 게 깡충깡충 뛰어가는데, 이마에 뭔가 뾰족한 게 있었어요.",
                "울타리 밑에 굴이 여러 개 파여 있어요. 쥐구멍보다 커요.",
                "개가 쫓아갔다가 코를 찔려서 돌아왔어요.",
            },
            new[] { "양배추밭이 사라지고 있어요", "뿔 달린 흰 녀석들", "울타리 밑의 굴" });

        Monster("pixie", "장난꾸러기 픽시", "픽시", "bird", new Color(0.95f, 0.66f, 0.86f), "BSS", Job.None,
            "요정 날개가루", "harpy",
            "손가락만 한 요정. 물건을 숨기고, 우유를 쏟고, 말꼬리를 땋아 놓는다. 킥킥거리는 웃음소리가 들린다. " +
            "위험하진 않지만 잡기가 성가시다. 도깨비불과 달리 소리가 요란하고, 반짝이는 가루가 분홍색이다.",
            "킥킥거리는 목소리가 {n}가지쯤 들렸다",
            new[]
            {
                "아침에 보니 말 꼬리가 전부 땋여 있었어요.",
                "숟가락이랑 단추가 자꾸 없어져요. 가끔 나무 위 새 둥지에서 나와요.",
                "밤에 킥킥거리는 소리가 들리고, 우유 통이 엎어져 있었어요.",
                "창틀에 분홍색 반짝이 가루가 묻어 있어요.",
            },
            new[] { "누가 말꼬리를 땋아 놔요", "숟가락 도둑", "킥킥거리는 밤손님" });

        Monster("sapling", "트렌트 묘목", "트렌트 묘목", "humanoid_big", new Color(0.42f, 0.52f, 0.26f), "SSG", Job.Warrior,
            "트렌트 옹이", "myconid",
            "사람 키만 한 어린 나무가 뿌리로 걸어 다닌다. 벌목한 그루터기 근처에 나타나 나무꾼을 쫓아낸다. " +
            "버섯 냄새나 포자는 없다. 몸이 단단해서 도끼를 제대로 휘두르는 전사가 필요하다.",
            "뿌리가 끌린 자국이 {n}줄 나 있었다",
            new[]
            {
                "어제 벤 그루터기 옆에 못 보던 나무가 서 있었어요. 오늘은 자리가 바뀌었고요.",
                "나무가 가지로 제 도끼를 쳐 냈어요. 진짜예요!",
                "땅에 뿌리를 끌고 간 것 같은 자국이 길게 나 있어요.",
                "삐걱삐걱, 나무 휘는 소리가 바람도 없는데 들려요.",
            },
            new[] { "걸어 다니는 나무", "벌목장이 쫓겨났어요", "자리를 옮기는 나무" });

        Monster("serpent", "대왕 뱀", "대왕 뱀", "amphibian", new Color(0.32f, 0.56f, 0.28f), "BSG", Job.Priest,
            "뱀 허물", "toad",
            "사람 팔뚝보다 굵은 초록 뱀. 나무 위나 덤불에서 기다렸다가 문다. 독이 있어 해독할 성직자가 필요하다. " +
            "허물을 벗어 두고, 기어간 자리가 S자로 남는다. 거미와 달리 줄을 치지 않는다.",
            "S자로 기어간 자국이 {n}줄 있었다",
            new[]
            {
                "덤불 속에 제 키만 한 허물이 걸려 있었어요.",
                "흙길에 구불구불한 자국이 길게 나 있어요.",
                "양이 다리를 물렸는데 이틀 동안 일어나지를 못해요.",
                "나뭇가지인 줄 알았는데 움직였어요. 쉬익 소리를 냈고요.",
            },
            new[] { "덤불 속의 큰 허물", "물린 양이 못 일어나요", "움직이는 나뭇가지" });

        LookAlike("spider", "serpent");
        LookAlike("serpent", "spider");
        LookAlike("moth", "wisp");
        LookAlike("wisp", "moth");
        LookAlike("bear", "owlbear");
        LookAlike("owlbear", "bear");
        LookAlike("hornrabbit", "rat");
        LookAlike("pixie", "wisp");
        LookAlike("sapling", "myconid");
        return SetRegion("forest", "myconid", "spider", "moth", "wisp", "bear", "owlbear", "hornrabbit", "pixie", "sapling", "serpent");
    }

    // ───────── 3. 회색 광산 ─────────
    // 헷갈리는 짝: 오크 ↔ 오우거 / 바위 골렘 ↔ 가고일(돌로 된 것) / 동굴 박쥐 떼 ↔ 가고일(밤의 날갯소리) / 광석 벌레 ↔ 갑주 개미(굴) / 산성 젤리 ↔ 슬라임

    public static string Region3()
    {
        Monster("cavebat", "동굴 박쥐 떼", "동굴 박쥐", "bird", new Color(0.26f, 0.22f, 0.30f), "BSS", Job.Priest,
            "박쥐 날개막", "harpy",
            "해가 지면 갱도에서 한꺼번에 쏟아져 나오는 박쥐 떼. 끽끽거리는 소리가 요란하다. 물린 사람과 가축이 열병을 앓는다. " +
            "병을 막아 줄 성직자가 필요하다. 가고일과 달리 작고, 떼로 다니며, 낮엔 천장에 거꾸로 매달려 잔다.",
            "갱도 천장에 {n}무리쯤 매달려 있었다",
            new[]
            {
                "해 질 녘에 갱도 입구에서 새까만 연기 같은 게 쏟아져 나와요. 끽끽 소리가 나요.",
                "목에 작은 이빨 자국이 두 개 난 염소가 열이 펄펄 끓어요.",
                "갱도 바닥에 까만 똥이 수북해요. 냄새가 지독하고요.",
                "등불을 들고 들어가니까 천장 전체가 꿈틀거렸어요.",
            },
            new[] { "갱도에서 쏟아지는 검은 연기", "열병 걸린 염소", "천장이 움직여요" });

        Monster("golem", "바위 골렘", "바위 골렘", "humanoid_big", new Color(0.56f, 0.56f, 0.60f), "SGG", Job.Warrior,
            "골렘 핵", "slime",
            "바위를 쌓아 만든 거인. 가슴 한가운데 빛나는 돌(핵)이 있다. 느리지만 한 방이 무겁다. 날개는 없다. " +
            "칼날은 튕겨 나가서 망치를 휘두르는 전사가 핵을 깨야 한다. 지나간 자리에 자갈이 떨어져 있다.",
            "무거운 발자국이 {n}개씩 깊게 패어 있었다",
            new[]
            {
                "채석장 바위더미가 일어나서 걸어갔어요. 가슴에 불빛이 있었어요.",
                "쿵, 쿵 하는 소리에 땅이 울려요. 아주 천천히요.",
                "수레가 납작하게 눌려 있고 주위에 자갈이 흩어져 있어요.",
                "곡괭이로 쳤더니 곡괭이가 부러졌어요.",
            },
            new[] { "걸어 다니는 바위더미", "채석장의 쿵쿵 소리", "수레가 납작해졌어요" });

        Monster("gargoyle", "가고일", "가고일", "bird", new Color(0.46f, 0.46f, 0.52f), "SSG", Job.Archer,
            "가고일 뿔 조각", "kobold",
            "낮에는 석상처럼 굳어 있다가 밤에 날개를 펴고 날아다닌다. 돌 피부라 거칠고, 날갯짓 소리가 무겁다. " +
            "높은 곳에 앉아 있어 궁수가 떨어뜨려야 한다. 박쥐처럼 떼로 다니진 않고 한두 마리씩 다닌다. 골렘과 달리 날개가 있다.",
            "지붕 위 석상이 {n}개로 늘어나 있었다",
            new[]
            {
                "폐광 입구 석상이 아침마다 자리가 바뀌어 있어요.",
                "밤에 퍼덕, 퍼덕 하고 무거운 날갯소리가 났어요. 박쥐 소리랑은 달라요.",
                "지붕 위에 못 보던 석상이 앉아 있더니 다음 날 없어졌어요.",
                "양이 공중으로 들려 갔어요. 돌 긁히는 소리가 났고요.",
            },
            new[] { "자리를 바꾸는 석상", "밤의 무거운 날갯소리", "지붕 위의 석상" });

        Monster("rockworm", "광석 벌레", "광석 벌레", "blob", new Color(0.62f, 0.48f, 0.38f), "SSG", Job.Warrior,
            "광석 벌레 턱", "slime",
            "소 한 마리만 한 벌레. 돌과 광석을 갉아 먹으며 땅속에 둥근 굴을 판다. 발밑에서 갑자기 튀어나온다. " +
            "턱이 단단해 앞에서 막아 줄 전사가 필요하다. 개미와 달리 굴이 크고 하나뿐이며, 혼자 다닌다.",
            "사람이 서서 들어갈 만한 굴이 {n}개 뚫려 있었다",
            new[]
            {
                "갱도 벽에 동그란 굴이 새로 뚫렸어요. 사람이 서서 들어갈 만해요.",
                "캐 놓은 철광석 더미가 하룻밤 새 반으로 줄었어요.",
                "발밑에서 드르륵 갉는 소리가 나더니 땅이 꺼졌어요.",
                "굴 안쪽 벽이 매끈하게 갈려 있어요. 이빨 자국 같은 홈이 있고요.",
            },
            new[] { "철광석이 사라져요", "발밑에서 갉는 소리", "새로 뚫린 큰 굴" });

        Monster("ogre", "오우거", "오우거", "humanoid_big", new Color(0.56f, 0.46f, 0.34f), "SGG", Job.Archer,
            "오우거 어금니", "orc",
            "오크보다 두 배는 큰 거인. 혼자 다니며 통나무를 몽둥이로 쓴다. 말이 서툴고 먹을 것에 약하다. " +
            "힘으로는 당해 낼 수 없어 멀리서 쏘는 궁수가 필요하다. 오크처럼 무리 짓거나 창을 쓰지 않는다.",
            "집채만 한 발자국이 {n}쌍 찍혀 있었다",
            new[]
            {
                "지붕보다 머리가 높은 게 통나무를 질질 끌고 갔어요.",
                "소를 통째로 들쳐 메고 갔어요. 혼자서요.",
                "'배고파…' 하고 굵은 목소리로 중얼거렸어요.",
                "발자국 안에 제가 들어가 누울 수 있어요.",
            },
            new[] { "소를 메고 간 거인", "통나무 몽둥이", "지붕보다 큰 그림자" });

        Monster("armorant", "갑주 개미", "갑주 개미", "rat", new Color(0.48f, 0.22f, 0.16f), "BSG", Job.Mage,
            "개미 갑각", "kobold",
            "개만 한 붉은 개미. 줄지어 다니며 곡식과 고기를 날라 간다. 갑각이 단단해 칼이 미끄러지므로 불을 쓰는 마법사가 필요하다. " +
            "개미굴은 작은 구멍이 여러 개이고, 흙더미가 봉긋하게 쌓인다. 수가 많을수록 위험하다.",
            "개미 행렬이 {n}줄로 이어져 있었다",
            new[]
            {
                "붉은 게 줄지어 창고로 들어가요. 하나하나가 개만 해요.",
                "창고 쌀가마니에 구멍이 숭숭 뚫리고 쌀이 한 줄로 흘러나가요.",
                "채석장 옆에 봉긋한 흙더미가 생겼는데 작은 구멍이 여러 개예요.",
                "칼로 내리쳤는데 딱 소리만 나고 미끄러졌어요.",
            },
            new[] { "창고로 들어가는 붉은 행렬", "쌀가마니가 비어요", "봉긋한 흙더미" });

        Monster("basilisk", "석화 도마뱀", "석화 도마뱀", "amphibian", new Color(0.56f, 0.62f, 0.44f), "SGG", Job.Priest,
            "석화 도마뱀 비늘", "toad",
            "다리가 여섯인 도마뱀. 노려본 것을 천천히 돌로 굳게 만든다. 동굴 근처에 돌이 된 쥐 · 새가 널려 있다. " +
            "굳은 것을 풀어 줄 성직자가 꼭 있어야 한다. 눈을 똑바로 보지 말 것.",
            "돌이 된 짐승이 {n}마리 굴러다녔다",
            new[]
            {
                "동굴 앞에 돌로 된 쥐랑 새가 여기저기 굴러다녀요. 진짜 같아요.",
                "광부 한 명이 다리가 점점 굳는다고 해요. 뭔가랑 눈이 마주쳤대요.",
                "다리가 여러 개인 도마뱀이 바위 틈으로 들어갔어요.",
                "바위 틈에서 초록빛 눈이 번쩍했어요. 그다음부터 발이 무거워요.",
            },
            new[] { "돌이 된 짐승들", "다리가 굳어 가요", "바위 틈의 초록 눈" });

        Monster("acidjelly", "산성 젤리", "산성 젤리", "blob", new Color(0.72f, 0.90f, 0.30f), "SSG", Job.Mage,
            "산성 젤리 방울", "slime",
            "갱도 물웅덩이에 사는 연두색 젤리. 닿은 것을 녹인다. 곡괭이 쇠 부분이 녹아 있으면 이 녀석이다. " +
            "슬라임보다 크고 독하며, 지나간 자리에서 연기가 난다. 마법으로 증발시켜야 한다.",
            "녹아 파인 자국이 {n}군데 있었다",
            new[]
            {
                "곡괭이 쇠 부분이 반쯤 녹아 있었어요. 자루는 멀쩡하고요.",
                "갱도 바닥에서 쉬익 하고 연기가 올라와요. 시큼한 냄새가 나요.",
                "연두색 젤리가 레일 위를 기어가요. 레일이 파였어요.",
                "장화가 녹아서 발가락이 나왔어요.",
            },
            new[] { "녹아 버린 곡괭이", "갱도의 시큼한 연기", "레일을 녹이는 젤리" });

        LookAlike("cavebat", "gargoyle");
        LookAlike("gargoyle", "golem");
        LookAlike("golem", "gargoyle");
        LookAlike("rockworm", "armorant");
        LookAlike("armorant", "rockworm");
        LookAlike("ogre", "orc");
        LookAlike("basilisk", "serpent");
        LookAlike("acidjelly", "slime");
        return SetRegion("mine", "orc", "troll", "cavebat", "golem", "gargoyle", "rockworm", "ogre", "armorant", "basilisk", "acidjelly");
    }
}
