using System.IO;
using UnityEngine;

namespace CounterTime.Game.Save
{
    /// META_DESIGN.md §M1 — single JSON save file, one slot. Serialization lives here
    /// (Game) — Core never touches files/JsonUtility/UnityEngine (purity rule
    /// preserved). `Application.persistentDataPath` is the one thing here that only
    /// runs inside Unity/the harness shim; every actual Read/Write overload below takes
    /// an explicit path so it — and the round-trip tests — never need to touch it.
    public static class SaveSystem
    {
        const string FileName = "save.json";

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        public static void Write(SaveData data) => Write(DefaultPath, data);

        /// 원자적 쓰기(META_DESIGN §M1) — 임시 파일에 먼저 다 쓰고 마지막에 이름만
        /// 바꾼다. 쓰는 도중 크래시가 나도 대상 경로엔 이전 상태가 그대로 남거나(이름
        /// 바꾸기 전) 새 상태로 완전히 바뀌거나(이름 바꾸기 성공) 둘 중 하나뿐이다 —
        /// 절반만 써진 파일이 저장 경로에 남는 경우가 없다.
        public static void Write(string path, SaveData data)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string json = JsonUtility.ToJson(data, true);
            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tempPath, path);
        }

        public static SaveData Read() => Read(DefaultPath);

        /// 세이브 파일이 아직 없으면(첫 실행) 빈 SaveData 를 준다 — 예외가 아니라 새
        /// 플레이어의 정상 상태다.
        public static SaveData Read(string path) =>
            File.Exists(path) ? JsonUtility.FromJson<SaveData>(File.ReadAllText(path)) : new SaveData();
    }
}
