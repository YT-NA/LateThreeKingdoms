// Unity 없이 컴파일·시뮬레이션하기 위한 최소 UnityEngine API 스텁. 실제 Unity 동작과 같지 않다 — 타입 검사와 게임 로직 시뮬레이션 용도로만 쓴다.
// 게임 코드가 새 Unity API를 쓰면 여기에 시그니처를 실제 Unity와 똑같이 추가할 것 (매개변수 이름 포함).
// Minimal Unity API stubs, only for type-checking the runtime scripts outside Unity.
using System;
namespace UnityEngine
{
    public class Object { public string name; public static void Destroy(Object o) { } }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T : ScriptableObject => (T)Activator.CreateInstance(typeof(T)); }
    public class GameObject : Object { public GameObject(string n, params Type[] c) { } public string tag; public Transform transform; public T GetComponent<T>() => default; }
    public class Transform : Component { public Vector3 position; }
    public struct Vector3 { public Vector3(float x, float y, float z) { } }
    public class Camera : Behaviour { public bool orthographic; public CameraClearFlags clearFlags; public Color backgroundColor; }
    public enum CameraClearFlags { Skybox, SolidColor }
    public enum FullScreenMode { Windowed }
    public enum FilterMode { Point, Bilinear }
    public class Component : Object { public GameObject gameObject; public T GetComponent<T>() => default; }
    public class Behaviour : Component { }
    public class MonoBehaviour : Behaviour { }
    public class Sprite : Object { }
    public class Texture2D : Object { }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1);
        public static Color gray => new Color(.5f, .5f, .5f);
        public static Color Lerp(Color a, Color b, float t) => new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t);
    }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } }
    public struct Vector2Int { public int x, y; public Vector2Int(int x, int y) { this.x = x; this.y = y; } }
    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a, b); public static float Max(float a, float b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b); public static float Min(float a, float b) => Math.Min(a, b);
        public static int RoundToInt(float f) => (int)Math.Round(f, MidpointRounding.ToEven); public static int CeilToInt(float f) => (int)Math.Ceiling(f); public static int FloorToInt(float f) => (int)Math.Floor(f);
        public static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v; public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        public static float Clamp01(float v) => Clamp(v, 0f, 1f); public static float Round(float f) => (float)Math.Round(f, MidpointRounding.ToEven);
    }
    public static class Debug
    {
        public static void Log(object m) { } public static void LogError(object m) { }
        public static void LogWarning(object m) { } public static void LogWarning(object m, Object ctx) { }
        public static void LogException(Exception e) { }
    }
    public static class Random { public static System.Random Rng = new System.Random(1); public static float value => (float)Rng.NextDouble(); public static float Range(float a, float b) => a + (float)Rng.NextDouble() * (b - a); public static int Range(int a, int b) => Rng.Next(a, b); }
    public static class Application { public static string dataPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "husamguk_sim", "Assets"); }
    public static class JsonUtility
    {
#if SIM
        static readonly System.Text.Json.JsonSerializerOptions Opt = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
        public static string ToJson(object o, bool prettyPrint) => System.Text.Json.JsonSerializer.Serialize(o, o.GetType(), Opt);
        public static T FromJson<T>(string s) => System.Text.Json.JsonSerializer.Deserialize<T>(s, Opt);
#else
        public static string ToJson(object o, bool prettyPrint) => "";
        public static T FromJson<T>(string s) => default;
#endif
    }
    public enum ScaleMode { StretchToFill, ScaleAndCrop, ScaleToFit }
    public enum FontStyle { Normal, Bold }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string s) { } }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string s) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    public class MinAttribute : Attribute { public MinAttribute(float a) { } }
    public class TextAreaAttribute : Attribute { }
    public class SerializeField : Attribute { }
    public class CreateAssetMenuAttribute : Attribute { public string menuName; public string fileName; }
    public class RequireComponent : Attribute { public RequireComponent(Type t) { } }
}
namespace UnityEngine.UIElements
{
    using System.Collections.Generic;
    public delegate void EventCallback<in T>(T evt);
    public class EventBase { }
    public class ClickEvent : EventBase { }
    public class StyleSheet : ScriptableObject { }
    public class PanelSettings : ScriptableObject { public PanelScaleMode scaleMode; public Vector2Int referenceResolution; public PanelScreenMatchMode screenMatchMode; public float match; }
    public enum PanelScaleMode { ConstantPixelSize, ScaleWithScreenSize }
    public enum PanelScreenMatchMode { MatchWidthOrHeight }
    public struct VisualElementStyleSheetSet { public void Add(StyleSheet s) { } }
    public struct Length { public static Length Percent(float v) => default; }
    public struct StyleLength { public static implicit operator StyleLength(float v) => default; public static implicit operator StyleLength(Length v) => default; }
    public struct StyleFloat { public static implicit operator StyleFloat(float v) => default; }
    public struct StyleColor { public static implicit operator StyleColor(Color v) => default; }
    public struct StyleBackground { public StyleBackground(Sprite s) { } }
    public struct StyleEnum<T> where T : struct { public static implicit operator StyleEnum<T>(T v) => default; }
    public enum DisplayStyle { Flex, None }
    public enum ScrollViewMode { Vertical, Horizontal, VerticalAndHorizontal }
    public class IStyle
    {
        public StyleLength width, height, left, top, minWidth;
        public StyleFloat flexGrow;
        public StyleColor backgroundColor;
        public StyleBackground backgroundImage;
        public StyleEnum<DisplayStyle> display;
        public StyleEnum<ScaleMode> unityBackgroundScaleMode;
    }
    public class Painter2D
    {
        public Color strokeColor; public float lineWidth;
        public void BeginPath() { } public void MoveTo(Vector2 p) { } public void LineTo(Vector2 p) { } public void Stroke() { }
    }
    public class MeshGenerationContext { public Painter2D painter2D => null; }
    public class VisualElement
    {
        public IStyle style = new IStyle();
        public VisualElementStyleSheetSet styleSheets;
        public Action<MeshGenerationContext> generateVisualContent;
        public void Add(VisualElement child) { }
        public void Insert(int i, VisualElement child) { }
        public void Clear() { }
        public void AddToClassList(string c) { }
        public void RemoveFromClassList(string c) { }
        public void SetEnabled(bool e) { }
        public void RegisterCallback<T>(EventCallback<T> cb) where T : EventBase { }
    }
    public class TextElement : VisualElement { public string text; }
    public class Label : TextElement { public Label() { } public Label(string t) { } }
    public class Button : TextElement { public Button() { } public Button(Action a) { } }
    public class ScrollView : VisualElement { public ScrollView() { } public ScrollView(ScrollViewMode m) { } }
    public class UIDocument : MonoBehaviour { public VisualElement rootVisualElement; public PanelSettings panelSettings; }
}
