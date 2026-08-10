using System.Linq;
using System.Reflection;
using NUnit.Framework;
using CounterTime.Core;
using CounterTime.Game.Match;

namespace CounterTime.Game.Tests
{
    /// <summary>
    /// 4b — <see cref="ResultNarrationTable"/> 의 폴백·되치기 키 분리·모드 격리.
    ///
    /// MonoBehaviour 를 안 쓰므로 dotnet 하니스에서도 돈다.
    /// </summary>
    public class ResultNarrationTableTests
    {
        [Test]
        public void Narrate_OnEmptyTable_FallsBackTo_TechniqueIdAndJudgementName()
        {
            var table = new ResultNarrationTable();

            var r = table.Narrate(Judgement.Ippon, "m36", isCounter: false);

            Assert.AreEqual("m36 — 한판", r.Text);
        }

        [Test]
        public void Narrate_OnEmptyTable_SetsIsFallbackTrue()
        {
            var table = new ResultNarrationTable();

            var r = table.Narrate(Judgement.Yuko, "밭다리", isCounter: false);

            Assert.IsTrue(r.IsFallback);
        }

        [Test]
        public void Narrate_OnRegisteredEntry_ReturnsAuthoredText_NotFallback()
        {
            var table = new ResultNarrationTable();
            table.Add(Judgement.Ippon, "m36", isCounter: false, "한판 — 시그니처 밭다리.");

            var r = table.Narrate(Judgement.Ippon, "m36", isCounter: false);

            Assert.AreEqual("한판 — 시그니처 밭다리.", r.Text);
            Assert.IsFalse(r.IsFallback);
        }

        /// <summary>
        /// 되치기는 반드시 별개 키다 — 같은 (판정, 기술) 라도 누가 던졌는지에 따라
        /// 서술이 완전히 다르다. 틀린 답은 IsCounter 를 무시하고 (판정, 기술) 만으로
        /// 같은 엔트리를 돌려주는 것이다.
        /// </summary>
        [Test]
        public void Narrate_CounterAndNonCounter_AreSeparateKeys_NotCollapsed()
        {
            var table = new ResultNarrationTable();
            table.Add(Judgement.Yuko, "밭다리", isCounter: false, "A의 정공법 밭다리 — 유효.");
            table.Add(Judgement.Yuko, "밭다리", isCounter: true, "되치기로 받은 밭다리 — 유효.");

            var straight = table.Narrate(Judgement.Yuko, "밭다리", isCounter: false);
            var countered = table.Narrate(Judgement.Yuko, "밭다리", isCounter: true);

            Assert.AreEqual("A의 정공법 밭다리 — 유효.", straight.Text);
            Assert.AreEqual("되치기로 받은 밭다리 — 유효.", countered.Text);
            Assert.AreNotEqual(straight.Text, countered.Text);
        }

        /// <summary>
        /// 대조 모드(스크립트 경로) 파일들은 <see cref="ResultNarrationTable"/> 을 몰라야
        /// 한다. <see cref="MatchRunner"/> 는 <c>JudgementNarrated</c> 이벤트를 쏘긴
        /// 하지만 그 이벤트를 실제로 소비해 표를 쿼리하는 건 play mode 전용
        /// <c>ResultNarrationPresenter</c> 뿐이고, 대조 모드 씬·테스트는 그 컴포넌트를
        /// 붙이지 않는다 — 그래서 격리는 "이벤트를 안 쏜다"가 아니라 "이 네 타입 중
        /// 어느 것도 표 타입을 참조하지 않는다"로 검증한다.
        ///
        /// 틀린 답은 어느 스크립트 경로 타입이든 필드·프로퍼티·메서드 시그니처에
        /// ResultNarrationTable 이 등장하는 것이다.
        /// </summary>
        /// <summary>
        /// docs/step4-narration-worksheet.md 배정 중 8건이 <see cref="ResultNarrationTable.CreateDefault"/>
        /// 에 등록돼 폴백이 아니어야 한다.
        /// </summary>
        [Test]
        public void CreateDefault_HasAllEightWorksheetEntries_NoneAreFallback()
        {
            var table = ResultNarrationTable.CreateDefault();

            Assert.IsFalse(table.Narrate(Judgement.Yuko, "업어치기", isCounter: true).IsFallback);
            Assert.IsFalse(table.Narrate(Judgement.Yuko, "업어치기", isCounter: false).IsFallback);
            Assert.IsFalse(table.Narrate(Judgement.None, "밭다리", isCounter: false).IsFallback);
            Assert.IsFalse(table.Narrate(Judgement.None, "허리후리기", isCounter: false).IsFallback);
            Assert.IsFalse(table.Narrate(Judgement.WazaAri, "밭다리", isCounter: false).IsFallback);
            Assert.IsFalse(table.Narrate(Judgement.WazaAri, "말아업어치기", isCounter: false).IsFallback);
            Assert.IsFalse(table.Narrate(Judgement.Ippon, "말아업어치기", isCounter: true).IsFallback);
            Assert.IsFalse(table.Narrate(Judgement.Ippon, "밭다리", isCounter: false).IsFallback);
        }

        /// <summary>
        /// m18 (Yuko, "밭다리", true)는 더 이상 등록하지 않는다 — 문구가 "같은 밭다리,
        /// 세 번째"로 횟수를 명시해서, play 모드(브레인 경로)에서 몇 번째 발생이든 뜨는
        /// 이 키에 그대로 물리면 오정보가 된다. 시나리오 개정 대기
        /// (docs/42-node-audit.md §6). 등록 안 됐으므로 항상 폴백이다.
        /// </summary>
        [Test]
        public void CreateDefault_M18_StaysFallback_TextWouldMisstateOccurrenceCount()
        {
            var table = ResultNarrationTable.CreateDefault();

            Assert.IsTrue(table.Narrate(Judgement.Yuko, "밭다리", isCounter: true).IsFallback);
        }

        /// <summary>
        /// m27·m34는 워크시트 결정대로 등록하지 않는다 — 폴백을 유지해야 한다.
        /// 이 표의 어떤 (판정, 기술) 조합으로 물어도 노드 id "m27"/"m34" 는 등록된 적이
        /// 없으므로 항상 폴백이다.
        /// </summary>
        [Test]
        public void CreateDefault_M27AndM34_StayFallback()
        {
            var table = ResultNarrationTable.CreateDefault();

            Assert.IsTrue(table.Narrate(Judgement.Yuko, "m27", isCounter: false).IsFallback);
            Assert.IsTrue(table.Narrate(Judgement.WazaAri, "m34", isCounter: false).IsFallback);
        }

        [Test]
        public void ModeIsolation_ScriptedPathTypes_NeverReferenceNarrationTable()
        {
            var tableType = typeof(ResultNarrationTable);
            var scriptedPathTypes = new[]
            {
                typeof(MatchRunner),
                typeof(ScriptedAssembler),
                typeof(ScriptedCommitSource),
                typeof(ScriptedReadResolver),
                typeof(ScenarioTurnChoices),
            };

            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic |
                                      BindingFlags.Instance | BindingFlags.Static;

            foreach (var t in scriptedPathTypes)
            {
                bool fieldHit = t.GetFields(all).Any(f => f.FieldType == tableType);
                bool propHit = t.GetProperties(all).Any(p => p.PropertyType == tableType);
                bool methodHit = t.GetMethods(all).Any(m =>
                    m.ReturnType == tableType ||
                    m.GetParameters().Any(p => p.ParameterType == tableType));

                Assert.IsFalse(fieldHit || propHit || methodHit,
                    $"{t.FullName} references {tableType.FullName} — 대조 모드가 4b 표를 타면 안 된다.");
            }
        }
    }
}
