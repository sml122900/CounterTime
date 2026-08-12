using System;
using System.Collections.Generic;
using CounterTime.Game.Save;

namespace CounterTime.Game.Meta
{
    /// <summary>
    /// §H 3.5b — practice cup bracket (META_DESIGN §M3). ROSTER.md fixes who the player
    /// fights each round (32강=황도철 … 결승=진도윤, 동메달전=오재훈) — that schedule is
    /// authored, not derived from bracket-tree adjacency. This class therefore does NOT
    /// model a full generic bracket tree: the player's own progress is a linear schedule
    /// (<see cref="TournamentRound"/>), and the other 31 entrants get a separate cosmetic
    /// round-of-32 grid, resolved via seeded coin flip, that never intersects the player's
    /// path. [설계 판단 — see STATUS.md 판단 필요 목록: this trades bracket-tree realism
    /// for a much smaller scaffold.]
    ///
    /// §H 3.5b-로스터 — each round's opponent now has a distinct <c>OpponentDefinition</c>
    /// (Brain layer, see <c>MetaFlowController.OpponentFactoryFor</c>); this class only
    /// carries the display names/ids, never the engine wiring (keeps this class free of a
    /// Brain-layer reference).
    /// </summary>
    public sealed class TournamentBracket
    {
        public const int SlotCount = 32;

        // ROSTER.md §R1–§R6, indexed to match TournamentRound's 5 win-path values.
        // 8강/4강 이름은 작업 지시로 ROSTER.md 초안 placeholder(서강윤/임세라)에서
        // 최종 이름(서도현/임준철)으로 교체됐다 — ROSTER.md 본문도 같이 갱신했다.
        static readonly string[] RoundOpponentName = { "황도철", "문준배", "서도현", "임준철", "진도윤" };
        const string BronzeOpponentName = "오재훈";

        // SaveData.opponents[] id — Brain 팩토리 선택(MetaFlowController)과 세이브 기록
        // 둘 다 같은 문자열을 쓴다.
        static readonly string[] RoundOpponentId =
            { "hwangdochul", "moonjunbae", "seodohyun", "imjunchul", "jindoyun" };
        const string BronzeOpponentId = "ohjaehoon";

        static readonly string[] RoundDisplayName = { "32강", "16강", "8강", "4강", "결승", "동메달전" };

        // §M3 "filler names (from a placeholder list)" — 30 entries for the 30 slots that
        // are never the player's opponent. Deliberately generic (not fake real names).
        static readonly string[] FillerPool =
        {
            "참가자01", "참가자02", "참가자03", "참가자04", "참가자05", "참가자06", "참가자07", "참가자08",
            "참가자09", "참가자10", "참가자11", "참가자12", "참가자13", "참가자14", "참가자15", "참가자16",
            "참가자17", "참가자18", "참가자19", "참가자20", "참가자21", "참가자22", "참가자23", "참가자24",
            "참가자25", "참가자26", "참가자27", "참가자28", "참가자29", "참가자30",
        };

        readonly string _playerName;
        readonly Random _rng;

        public TournamentBracket(string playerName, int seed)
        {
            _playerName = string.IsNullOrEmpty(playerName) ? "나" : playerName;
            _rng = new Random(seed);
        }

        public static string RoundLabel(TournamentRound round) => RoundDisplayName[(int)round];

        /// The name the player faces this round — always resolved from ROSTER's fixed
        /// schedule, never from bracket math.
        public static string PlayerOpponentName(TournamentRound round) =>
            round == TournamentRound.BronzeMatch ? BronzeOpponentName : RoundOpponentName[(int)round];

        /// §H 3.5b-로스터 — SaveData.opponents[] id, same schedule as the display name.
        public static string PlayerOpponentId(TournamentRound round) =>
            round == TournamentRound.BronzeMatch ? BronzeOpponentId : RoundOpponentId[(int)round];

        /// Round-of-32 display grid — 32 names, index 0 is always the player, index 1 is
        /// always the player's round-1 opponent (황도철). The remaining 30 slots are a
        /// shuffled filler pool, cosmetic only.
        public IReadOnlyList<string> Round1Grid()
        {
            var others = new List<string>(FillerPool);
            Shuffle(others);
            var grid = new List<string>(SlotCount) { _playerName, RoundOpponentName[0] };
            grid.AddRange(others);
            return grid;
        }

        /// Resolves every non-player round-1 match to a winner via an independent coin
        /// flip per match — flavor only ("대진표가 살아있어 보인다"), never read by the
        /// player's own path or by later rounds.
        public IReadOnlyList<string> ResolveOtherFirstRoundWinners()
        {
            var grid = Round1Grid();
            var winners = new List<string>();
            for (int i = 2; i < grid.Count; i += 2)
                winners.Add(_rng.Next(2) == 0 ? grid[i] : grid[i + 1]);
            return winners;
        }

        void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// The next round on a win, or null if the cup is over (won the final or the
        /// bronze match).
        public static TournamentRound? NextRoundOnWin(TournamentRound current) => current switch
        {
            TournamentRound.R32 => TournamentRound.R16,
            TournamentRound.R16 => TournamentRound.QuarterFinal,
            TournamentRound.QuarterFinal => TournamentRound.SemiFinal,
            TournamentRound.SemiFinal => TournamentRound.Final,
            _ => null, // Final or BronzeMatch — cup over
        };

        /// META_DESIGN §M3 medal rule for a win.
        public static PracticeCupResult ResultOnWin(TournamentRound current) => current switch
        {
            TournamentRound.Final => PracticeCupResult.Gold,
            TournamentRound.BronzeMatch => PracticeCupResult.Bronze,
            _ => PracticeCupResult.None, // still in progress, not a terminal result
        };

        /// Only a semifinal loss keeps the cup going (→ the bronze match). Callers must
        /// check this before calling <see cref="ResultOnLoss"/> — every other loss is
        /// terminal.
        public static bool LossContinuesToBronze(TournamentRound current) =>
            current == TournamentRound.SemiFinal;

        /// §M1 has no separate "4th place" value, so a bronze-match loss maps to Lost —
        /// same as losing any other non-final round. Only call this once
        /// <see cref="LossContinuesToBronze"/> is false.
        public static PracticeCupResult ResultOnLoss(TournamentRound current) =>
            current == TournamentRound.Final ? PracticeCupResult.Silver : PracticeCupResult.Lost;
    }
}
