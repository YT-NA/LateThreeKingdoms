using UnityEditor;

namespace Husamguk.EditorTools
{
    /// <summary>
    /// Art/Sprites 폴더에 들어오는 그림(PixelLab 결과물 등)을 자동으로 픽셀아트 설정으로 임포트한다.
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
            // 사용자가 이미 한 번 설정을 바꾼 파일은 건드리지 않는다
            if (!imp.importSettingsMissing) return;

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
