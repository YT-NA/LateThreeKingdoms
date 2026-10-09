using System.Collections.Generic;
using System.IO;
using Husamguk.Data;
using UnityEditor;
using UnityEngine;

namespace Husamguk.EditorTools
{
    /// <summary>
    /// 메뉴 [Husamguk/아트 자동 연결]: Art/Sprites 안의 파일 이름 규칙(docs/ART_LIST.md)으로 정의 에셋에 스프라이트를 연결한다.
    ///   portrait_{신하 id}.png → RetainerDef.portrait
    ///   res_{자원 id}.png      → ResourceDef.icon
    ///   unit_{병종 id}.png     → UnitDef.icon
    ///   tier_{등급 id}.png     → SettlementTierDef.settlementSprite
    ///   banner_{세력 id}.png   → FactionDef.banner
    /// 하위 폴더 어디에 둬도 된다. 대응하는 파일이 없으면 기존 스프라이트를 유지한다.
    /// GameSetup.Run()이 샘플 데이터를 만든 뒤 자동으로 호출한다.
    /// </summary>
    public static class SpriteLinker
    {
        [MenuItem("Husamguk/아트 자동 연결")]
        public static void Run()
        {
            Directory.CreateDirectory(PixelArtImporter.SpritesFolder);
            AssetDatabase.Refresh();
            int reimported = FixImportSettings();
            if (reimported > 0) Debug.Log($"[SpriteLinker] 스프라이트가 아닌 설정으로 임포트돼 있던 그림 {reimported}개를 바로잡았습니다.");

            var sprites = FindSprites();
            int linked = 0;
            var missing = new List<string>();

            foreach (var def in LoadAll<RetainerDef>())
                linked += Link(sprites, $"portrait_{def.id}", def, s => def.portrait = s, missing);
            foreach (var def in LoadAll<ResourceDef>())
                linked += Link(sprites, $"res_{def.id}", def, s => def.icon = s, missing);
            foreach (var def in LoadAll<UnitDef>())
                linked += Link(sprites, $"unit_{def.id}", def, s => def.icon = s, missing);
            foreach (var def in LoadAll<SettlementTierDef>())
                linked += Link(sprites, $"tier_{def.id}", def, s => def.settlementSprite = s, missing);
            foreach (var def in LoadAll<FactionDef>())
                linked += Link(sprites, $"banner_{def.id}", def, s => def.banner = s, missing);

            AssetDatabase.SaveAssets();
            Debug.Log($"[SpriteLinker] {linked}개 연결. 아직 그림이 없는 항목 {missing.Count}개:\n" + string.Join("\n", missing));
        }

        /// <summary>
        /// Sprites 폴더의 그림 중 스프라이트로 임포트되지 않은 것을 강제로 다시 임포트한다.
        /// (첫 프로젝트 열기에서 PixelArtImporter보다 텍스처 임포트가 먼저 돌아 일반 텍스처로 찍혀버리는 문제 대응)
        /// </summary>
        static int FixImportSettings()
        {
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { PixelArtImporter.SpritesFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter imp)) continue;
                if (imp.textureType == TextureImporterType.Sprite && imp.spriteImportMode == SpriteImportMode.Single) continue;
                PixelArtImporter.Apply(imp);
                imp.SaveAndReimport();
                count++;
            }
            return count;
        }

        /// <summary>id → 스프라이트. 파일 이름(확장자 제외, 소문자) 기준.</summary>
        public static Dictionary<string, Sprite> FindSprites()
        {
            var result = new Dictionary<string, Sprite>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { PixelArtImporter.SpritesFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) result[Path.GetFileNameWithoutExtension(path).ToLowerInvariant()] = sprite;
            }
            return result;
        }

        static int Link(Dictionary<string, Sprite> sprites, string key, Object target, System.Action<Sprite> assign, List<string> missing)
        {
            if (!sprites.TryGetValue(key, out var sprite))
            {
                missing.Add($"{key}.png");
                return 0;
            }
            assign(sprite);
            EditorUtility.SetDirty(target);
            return 1;
        }

        static IEnumerable<T> LoadAll<T>() where T : ScriptableObject
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && !string.IsNullOrEmpty(GetId(asset))) yield return asset;
            }
        }

        static string GetId(ScriptableObject so) => so switch
        {
            RetainerDef r => r.id,
            ResourceDef r => r.id,
            UnitDef u => u.id,
            SettlementTierDef t => t.id,
            FactionDef f => f.id,
            _ => null,
        };
    }
}
