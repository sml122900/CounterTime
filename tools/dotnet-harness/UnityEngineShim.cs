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

        public static T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, Opts);
    }
}
