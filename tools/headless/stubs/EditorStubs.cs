// 에디터 스크립트 타입 검사용 최소 UnityEditor API 스텁.
using System;
using UnityEngine;
namespace UnityEditor
{
    public class MenuItem : Attribute { public MenuItem(string p) { } }
    public static class AssetDatabase
    {
        public static void CreateAsset(UnityEngine.Object o, string p) { }
        public static string[] FindAssets(string f) => null; public static string[] FindAssets(string f, string[] folders) => null;
        public static string GUIDToAssetPath(string g) => null; public static string GetAssetPath(UnityEngine.Object o) => null;
        public static T LoadAssetAtPath<T>(string p) where T : UnityEngine.Object => null;
        public static void Refresh() { } public static void SaveAssets() { }
    }
    public static class EditorUtility { public static bool DisplayDialog(string a, string b, string c) => true; public static bool DisplayDialog(string a, string b, string c, string d) => true; public static void SetDirty(UnityEngine.Object o) { } }
    public static class EditorApplication { public static void Exit(int c) { } }
    public class AssetImporter : UnityEngine.Object { public static AssetImporter GetAtPath(string p) => null; public void SaveAndReimport() { } }
    public enum TextureImporterType { Default, Sprite }
    public enum SpriteImportMode { None, Single, Multiple }
    public enum TextureImporterCompression { Uncompressed }
    public class TextureImporter : AssetImporter
    {
        public TextureImporterType textureType; public SpriteImportMode spriteImportMode; public float spritePixelsPerUnit;
        public FilterMode filterMode; public TextureImporterCompression textureCompression; public bool mipmapEnabled; public bool alphaIsTransparency; public bool importSettingsMissing;
    }
    public class AssetPostprocessor { public string assetPath; public AssetImporter assetImporter; }
    public class SerializedProperty { public UnityEngine.Object objectReferenceValue; }
    public class SerializedObject { public SerializedObject(UnityEngine.Object o) { } public SerializedProperty FindProperty(string n) => null; public bool ApplyModifiedPropertiesWithoutUndo() => true; }
    public static class PlayerSettings
    {
        public static string productName, companyName, bundleVersion; public static FullScreenMode fullScreenMode;
        public static int defaultScreenWidth, defaultScreenHeight; public static bool resizableWindow, runInBackground;
        public static class SplashScreen { public static bool showUnityLogo; }
    }
    public class EditorBuildSettingsScene { public EditorBuildSettingsScene(string p, bool e) { } }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes; }
    public enum BuildTarget { StandaloneWindows64 }
    public enum BuildOptions { None }
    public struct BuildPlayerOptions { public string[] scenes; public string locationPathName; public BuildTarget target; public BuildOptions options; }
    public static class BuildPipeline { public static UnityEditor.Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions o) => null; }
}
namespace UnityEditor.Build.Reporting
{
    public enum BuildResult { Succeeded, Failed }
    public class BuildSummary { public BuildResult result; public int totalErrors; }
    public class BuildReport { public BuildSummary summary; }
}
namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene }
    public enum NewSceneMode { Single }
    public struct Scene { }
    public static class EditorSceneManager
    {
        public static Scene NewScene(NewSceneSetup s, NewSceneMode m) => default;
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() => true;
        public static bool SaveScene(Scene s, string p) => true;
    }
}
