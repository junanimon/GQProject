using UnityEngine;

namespace GuildProto
{
    // 모험가 대사 풀
    public static class Lines
    {
        public static string Pick(string[] lines) => lines[Random.Range(0, lines.Length)];

        public static readonly string[] Accept =
        {
            "이거 받을게요. 금방 끝내고 올게요!", "이 의뢰로 부탁해요.", "보수 괜찮네요. 이걸로 할게요!",
            "…이거. (말수가 적다)", "오늘은 이거다! 접수 부탁해요!", "이 정도면 할 만하겠죠?",
        };
        public static readonly string[] Party =
        {
            "우리 파티는 이걸로 갈게요.", "다 같이 이거 받으려고요!", "파티 전원 카드 여기요.",
        };
        public static readonly string[] Greedy =
        {
            "등급이요? 에이, 괜찮아요 괜찮아.", "이번엔 좀 큰 거 한번 해보려고요!", "보수가 좋길래요. 되죠?",
        };
        public static readonly string[] Forger =
        {
            "카드요? 여기요. 빨리 좀 부탁해요.", "(시선을 피한다) …이 의뢰로요.", "제 카드 문제없죠? 당연하죠, 하하.",
        };
        public static readonly string[] Thanks =
        {
            "감사합니다! 다녀올게요!", "좋아, 가자!", "금방 올게요~", "(고개를 꾸벅 숙인다)",
        };
        public static readonly string[] Caught =
        {
            "쳇… 역시 안 되나.", "아, 들켰네. 다음에 올게요.", "…규정이 그렇다면야.",
        };
        public static readonly string[] WrongReject =
        {
            "네? 규정상 문제없을 텐데요…", "…왜요? 저 뭐 잘못했어요?", "다른 길드로 가야 하나…",
        };
        public static readonly string[] Lie =
        {
            "다녀왔어요! 완벽하게 처리했죠. 증거물 여기요.", "후, 힘들었다. 전부 해치웠어요. 확인해 보세요.", "문제없이 끝났어요. 수수료 처리 부탁해요.",
        };
        public static readonly string[] Chat =
        {
            "게시판이 텅 비었네요. 오늘은 수다나 떨다 갈게요. 어제 선술집에서요…",
            "일 없는 날도 있어야죠. 접수원님, 이 마을엔 적응 좀 됐어요?",
            "요즘 숲이 좀 조용하지 않아요? 괜히 기분이 이상하네.",
        };
    }
}
