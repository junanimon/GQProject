using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuildProto
{
    // 숨겨진 정답 기준으로 수주 결과를 판정하고, 다음 날 귀환 보고 내용(주장 · 증거물 · 대사)을 정한다.
    public class QuestResolver
    {
        readonly GameConfig config;

        public QuestResolver(GameConfig config) => this.config = config;

        public void Resolve(Assignment a)
        {
            var q = a.Quest;
            var truth = q.Truth;
            var entry = q.Entry.Monster;
            int capability = (int)a.Members.Min(m => m.Rank) + (a.Members.Count >= 3 ? 1 : 0);

            int rankGap = Mathf.Max(0, (int)q.TruthRank - capability);
            bool roleMiss = truth.role != Job.None && a.Members.All(m => m.Job != truth.role);
            int mismatches = rankGap + (roleMiss ? 1 : 0);
            bool tooEasy = capability - (int)q.TruthRank >= 2;
            float avgAffinity = (float)a.Members.Average(m => m.Affinity);

            var result = mismatches == 0
                ? (avgAffinity >= 60 && Random.value < 0.35f ? Result.Great : Result.Success)
                : mismatches == 1 ? Result.Injured : Result.Fail;

            var causes = new List<string>();
            if (roleMiss) causes.Add($"{Txt.J(truth.role)} 없이는 상대가 안 됐어요.");
            if (rankGap > 0) causes.Add("우리 실력으론 버거운 상대였어요.");

            string report;
            bool claims, lying = false;
            Evidence evidence = null;

            if (result != Result.Fail)
            {
                // 성공: 게시본 기입대로 증거물을 가져온다 (기입이 틀렸어도 창구에선 멀쩡해 보인다)
                claims = true;
                evidence = entry.isMonster
                    ? Evidence.PartsOf(entry, Random.Range(Txt.BandMin(q.Entry.Count), Txt.BandMax(q.Entry.Count) + 1))
                    : Evidence.ProofItem(q.Proof);
                var leader = a.Members[0];
                if (result == Result.Injured)
                {
                    var hurt = a.Members[Random.Range(0, a.Members.Count)];
                    hurt.Injure(2);
                    report = string.Join(" ", causes) + $" 겨우 해치웠지만 {hurt.Name}{Txt.Josa(hurt.Name, "이", "가")} 다쳐서 며칠 쉬어야 해요. 증거물은 챙겨 왔어요.";
                }
                else if (result == Result.Great)
                    report = "대성공! " + leader.Line(LineKind.ReturnSuccess, SuccessLines);
                else
                    report = leader.Line(LineKind.ReturnSuccess, SuccessLines) + (tooEasy ? " …근데 솔직히 너무 쉬웠어요." : "");
            }
            else if (Random.value < config.lieChanceOnFail)
            {
                // 허위 보고: 실패했지만 성공이라 우긴다
                claims = lying = true;
                if (!entry.isMonster) evidence = null;                                                     // 증거물 없음
                else if (q.Entry.Count != CountBand.Few && Random.value < 0.5f)
                    evidence = Evidence.PartsOf(entry, Random.Range(1, Txt.BandMin(q.Entry.Count)));        // 수량 부족
                else
                    evidence = Evidence.PartsOf(LookAlike(entry),                                           // 엉뚱한 부위
                        Random.Range(Txt.BandMin(q.Entry.Count), Txt.BandMax(q.Entry.Count) + 1));
                report = evidence == null ? "다녀왔어요! 증거물은… 오다가 잃어버렸어요. 그래도 처리했어요, 진짜로요." : Lines.Pick(Lines.Lie);
            }
            else
            {
                claims = false;
                if (entry != truth)
                    causes.Insert(0, $"의뢰서엔 {entry.talkName}{Txt.Josa(entry.talkName, "이라고", "라고")} 적혀 있었는데… 실제로는 {truth.talkName}{Txt.Josa(truth.talkName, "이었어요", "였어요")}!");
                else if (truth.isMonster && q.Entry.Count != q.TruthCount)
                    causes.Insert(0, $"수가 적혀 있던 거랑 달랐어요. 실제로는 {q.TruthNumber}마리였다고요.");
                report = string.Join(" ", causes) + " " + a.Members[0].Line(LineKind.ReturnFail, FailLines);
            }

            int affinityDelta = result switch
            {
                Result.Great => 12,
                Result.Success => tooEasy ? -3 : 8,
                Result.Injured => -5,
                _ => -12,
            };
            foreach (var m in a.Members) m.ChangeAffinity(affinityDelta);

            int dangerDelta = !truth.isMonster ? 0 : result switch
            {
                Result.Great => -8, Result.Success => -6, Result.Injured => -3, _ => 4,
            };
            a.SetOutcome(result, report, dangerDelta, claims, lying, evidence);
        }

        static MonsterData LookAlike(MonsterData m) => m.lookAlike != null ? m.lookAlike : m;

        static readonly string[] SuccessLines = { "다녀왔어요! 의뢰 완료입니다. 증거물 확인해 주세요." };
        static readonly string[] FailLines = { "도저히 안 돼서 도망쳐 왔어요…" };
    }
}
