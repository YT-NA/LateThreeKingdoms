using System.IO;
using UnityEngine;

namespace Husamguk.Core
{
    /// <summary>
    /// 단일 세이브 슬롯 (§11 결정: 다중 세이브는 2차 범위). JsonUtility 패턴은 RuleTheKingdom과 동일하지만,
    /// 저장 위치는 persistentDataPath가 아니라 <b>실행 파일이 들어있는 폴더</b>로 둔다 — 클라우드에서 개발하고
    /// 내려받은 실행 파일로만 테스트하는 사용자가 세이브 파일을 바로 찾을 수 있게 하기 위해서다.
    /// 빌드에서는 Application.dataPath가 "{exe 폴더}/{製품명}_Data"라 한 단계 위가 exe 폴더, 에디터에서는
    /// 프로젝트 루트가 된다.
    /// </summary>
    public static class SaveSystem
    {
        static string Path => System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "..", "save.json"));

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
