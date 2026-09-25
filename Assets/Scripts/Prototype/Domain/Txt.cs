using UnityEngine;

namespace GuildProto
{
    // 화면에 쓰는 이름·색·조사 도우미
    public static class Txt
    {
        static readonly string[] ranks = { "동", "은", "금" };
        static readonly string[] jobs = { "없음", "전사", "궁수", "마법사", "성직자" };
        static readonly string[] counts = { "1~3", "4~7", "8 이상" };
        static readonly string[] results = { "대성공", "성공", "부상 성공", "실패" };

        public static string R(Rank r) => ranks[(int)r];
        public static string J(Job j) => jobs[(int)j];
        public static string C(CountBand c) => counts[(int)c];
        public static string Res(Result r) => results[(int)r];
        public static char RankCode(Rank r) => "BSG"[(int)r];

        public static Color RankColor(Rank r) => r switch
        {
            Rank.Bronze => new Color(0.72f, 0.45f, 0.22f),
            Rank.Silver => new Color(0.70f, 0.72f, 0.78f),
            _ => new Color(0.93f, 0.76f, 0.20f),
        };

        public static string RankHex(Rank r) => "#" + ColorUtility.ToHtmlStringRGB(RankColor(r) * 0.8f);

        public static int BandMin(CountBand b) => b switch { CountBand.Few => 1, CountBand.Pack => 4, _ => 8 };
        public static int BandMax(CountBand b) => b switch { CountBand.Few => 3, CountBand.Pack => 7, _ => 12 };

        public static string Describe(MonsterData m, CountBand c) => m.isMonster ? $"{m.displayName} {C(c)}" : "몬스터 없음";

        // 받침 유무에 따라 조사 선택 (예: 슬라임이라고 / 늑대라고)
        public static string Josa(string word, string withBatchim, string withoutBatchim)
        {
            if (string.IsNullOrEmpty(word)) return withoutBatchim;
            char c = word[word.Length - 1];
            if (c < 0xAC00 || c > 0xD7A3) return withoutBatchim;
            return (c - 0xAC00) % 28 != 0 ? withBatchim : withoutBatchim;
        }
    }
}
