using UnityEditor;

namespace Husamguk.EditorTools
{
    /// <summary>
    /// Art/Sprites 폴더에 들어오는 그림(PixelLab 결과물 등)을 픽셀아트 스프라이트 설정으로 임포트한다.
    /// Point 필터, 압축 없음, 32 PPU, 밉맵 없음. RuleTheKingdom과 동일한 설정.
    /// </summary>
    public class PixelArtImporter : AssetPostprocessor
    {
        public const string SpritesFolder = "Assets/_Project/Art/Sprites";
        public const int PixelsPerUnit = 32;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpritesFolder)) return;
            var imp = (TextureImporter)assetImporter;
            // 프로젝트를 처음 열 때 이 스크립트가 컴파일되기 전에 그림이 일반 텍스처로 먼저 임포트되는 일이 있었다.
            // 그래서 "설정이 처음인 파일"만이 아니라 "아직 스프라이트가 아닌 파일"도 바로잡는다.
            if (!imp.importSettingsMissing && imp.textureType == TextureImporterType.Sprite) return;
            Apply(imp);
        }

        public static void Apply(TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = PixelsPerUnit;
            imp.filterMode = UnityEngine.FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
        }
    }
}
