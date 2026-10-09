using System.IO;
using UnityEngine;

namespace Husamguk.Core
{
    /// <summary>단일 세이브 슬롯 (§11 결정: 다중 세이브는 2차 범위). RuleTheKingdom과 동일한 JsonUtility 패턴.</summary>
    public static class SaveSystem
    {
        static string Path => System.IO.Path.Combine(Application.persistentDataPath, "save.json");

        public static void Save(SaveData data)
        {
            data.savedAtUnixSeconds = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            File.WriteAllText(Path, JsonUtility.ToJson(data, prettyPrint: true));
        }

        public static SaveData Load()
        {
            if (!File.Exists(Path)) return null;
            try { return JsonUtility.FromJson<SaveData>(File.ReadAllText(Path)); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveSystem] 세이브 파일을 읽지 못했습니다: {e.Message}");
                return null;
            }
        }

        public static bool HasSave() => File.Exists(Path);
        public static void Delete() { if (File.Exists(Path)) File.Delete(Path); }
    }
}
