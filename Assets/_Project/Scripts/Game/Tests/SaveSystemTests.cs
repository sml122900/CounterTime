using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using CounterTime.Game.Save;

namespace CounterTime.Game.Tests
{
    /// §H 3.5a — SaveSystem 파일 왕복. `Application.persistentDataPath` 를 쓰는 기본
    /// 오버로드는 여기서 안 건드린다(에디터/하니스 어느 쪽이든 실제 유저 세이브를
    /// 건드리면 안 되므로) — 전부 스크래치 임시 경로로 검증한다.
    public class SaveSystemTests
    {
        string _path;

        [SetUp]
        public void SetUp() =>
            _path = Path.Combine(Path.GetTempPath(), "counter-time-save-test-" + System.Guid.NewGuid() + ".json");

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_path)) File.Delete(_path);
            if (File.Exists(_path + ".tmp")) File.Delete(_path + ".tmp");
        }

        static SaveData BuildFullSample() => new SaveData
        {
            playerName = "김선수",
            mastery = new List<MasteryEntry>
            {
                new MasteryEntry { techId = "밭다리걸기", level = 4 },
                new MasteryEntry { techId = "허리후리기", level = 1 },
            },
            opponents = new List<OpponentSaveEntry>
            {
                new OpponentSaveEntry
                {
                    id = "jindoyun",
                    name = "진도윤",
                    scoutingLevel = 2,
                    adaptation = new List<AdaptationEntry>
                    {
                        new AdaptationEntry { techId = "업어치기", count = 3 },
                    },
                    knownByMe = new List<string> { "안뒤축감아치기" },
                },
            },
            career = new CareerSaveData { practiceCupResult = PracticeCupResult.Bronze },
        };

        /// 스키마의 필드 전부(§M1 그대로: playerName/mastery/opponents 안의 4개 필드 +
        /// 중첩 adaptation/knownByMe/career)를 하나씩 짚는다 — 어느 한 필드가 조용히
        /// 안 실리는 회귀를 잡기 위해 일부러 필드마다 따로 assert 한다.
        [Test]
        public void Write_ThenRead_RoundTripsEveryField()
        {
            var original = BuildFullSample();

            SaveSystem.Write(_path, original);
            var loaded = SaveSystem.Read(_path);

            Assert.AreEqual(original.playerName, loaded.playerName);

            Assert.AreEqual(2, loaded.mastery.Count);
            Assert.AreEqual("밭다리걸기", loaded.mastery[0].techId);
            Assert.AreEqual(4, loaded.mastery[0].level);
            Assert.AreEqual("허리후리기", loaded.mastery[1].techId);
            Assert.AreEqual(1, loaded.mastery[1].level);

            Assert.AreEqual(1, loaded.opponents.Count);
            var opp = loaded.opponents[0];
            Assert.AreEqual("jindoyun", opp.id);
            Assert.AreEqual("진도윤", opp.name);
            Assert.AreEqual(2, opp.scoutingLevel);
            Assert.AreEqual(1, opp.adaptation.Count);
            Assert.AreEqual("업어치기", opp.adaptation[0].techId);
            Assert.AreEqual(3, opp.adaptation[0].count);
            Assert.AreEqual(1, opp.knownByMe.Count);
            Assert.AreEqual("안뒤축감아치기", opp.knownByMe[0]);

            Assert.AreEqual(PracticeCupResult.Bronze, loaded.career.practiceCupResult);
        }

        [Test]
        public void Read_WhenFileMissing_ReturnsFreshEmptySaveData()
        {
            var loaded = SaveSystem.Read(_path);   // 이 테스트에선 한 번도 Write 안 함

            Assert.AreEqual("", loaded.playerName);
            Assert.IsEmpty(loaded.mastery);
            Assert.IsEmpty(loaded.opponents);
            Assert.AreEqual(PracticeCupResult.None, loaded.career.practiceCupResult,
                "None 이 §M1 의 '| null' 케이스를 대신한다 — 아직 대회 결과 없음");
        }

        [Test]
        public void Write_DoesNotLeaveTempFileBehind()
        {
            SaveSystem.Write(_path, BuildFullSample());

            Assert.IsFalse(File.Exists(_path + ".tmp"), "쓰기가 끝나면 임시 파일이 이름을 다 바꿔서 안 남아야 한다");
            Assert.IsTrue(File.Exists(_path));
        }

        [Test]
        public void Write_Overwrites_PreviousSave()
        {
            SaveSystem.Write(_path, new SaveData { playerName = "이전" });
            SaveSystem.Write(_path, new SaveData { playerName = "새로" });

            Assert.AreEqual("새로", SaveSystem.Read(_path).playerName);
        }

        [Test]
        public void Write_CreatesTheDirectory_IfItDoesNotExist()
        {
            string nestedPath = Path.Combine(Path.GetTempPath(),
                "counter-time-save-test-dir-" + System.Guid.NewGuid(), "save.json");

            SaveSystem.Write(nestedPath, new SaveData { playerName = "폴더 생성 확인" });

            Assert.IsTrue(File.Exists(nestedPath));
            Assert.AreEqual("폴더 생성 확인", SaveSystem.Read(nestedPath).playerName);

            File.Delete(nestedPath);
            Directory.Delete(Path.GetDirectoryName(nestedPath));
        }
    }
}
