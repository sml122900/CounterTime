using System;
using System.Text.Json;

// Minimal stand-ins for the UnityEngine members CounterTime.Game's harness-runnable
// subset actually calls (see Harness.csproj's Compile Include comment for the
// analysis of which Game files can/can't be shimmed). No engine behavior — these are
// data holders so the code compiles and runs outside Unity.
namespace UnityEngine
{
    public class MonoBehaviour { }

    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeFieldAttribute : Attribute { }

    public class TextAsset
    {
        public string text;
        public TextAsset() { }
        public TextAsset(string text) { this.text = text; }
    }

    public static class Debug
    {
        public static void Log(object message, object context) { }
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogWarning(object message, object context) { }
        public static void LogError(object message, object context) { }
        public static void LogError(object message) { }
    }

    public static class Mathf
    {
        public static int Clamp(int value, int min, int max) =>
            value < min ? min : (value > max ? max : value);
    }

    public enum KeyCode
    {
        None = 0,
        Space = 32,
        Alpha1 = 49,
        R = 114,
    }

    public static class Input
    {
        public static bool GetKeyDown(KeyCode key) => false;
    }

    public static class JsonUtility
    {
        static readonly JsonSerializerOptions Opts = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = false,
        };
        static readonly JsonSerializerOptions PrettyOpts = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = false,
            WriteIndented = true,
        };

        public static T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, Opts);

        // §H step 3.5a — added for SaveSystem.cs (Game/Save). Real Unity's ToJson takes
        // the object's runtime type, not a generic <T>, so this mirrors that signature.
        public static string ToJson(object obj) => JsonSerializer.Serialize(obj, obj.GetType(), Opts);
        public static string ToJson(object obj, bool prettyPrint) =>
            JsonSerializer.Serialize(obj, obj.GetType(), prettyPrint ? PrettyOpts : Opts);
    }

    // §H step 3.5a — added for SaveSystem.cs's DefaultPath property. Every actual
    // Read/Write call SaveSystemTests.cs exercises takes an explicit path instead, so
    // this only needs to exist for the file to compile — its value is never read by a
    // test. Points at the OS temp dir, matching what a "some writable scratch folder"
    // stand-in should reasonably be outside the real Unity Editor/Player.
    public static class Application
    {
        public static string persistentDataPath => System.IO.Path.GetTempPath();
    }
}
