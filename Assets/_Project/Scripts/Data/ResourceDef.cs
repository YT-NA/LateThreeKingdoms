using UnityEngine;

namespace Husamguk.Data
{
    /// <summary>석·엽전·권위 같은 자원 하나의 정의. GDD §5.1.</summary>
    [CreateAssetMenu(menuName = "Husamguk/Resource", fileName = "Res_")]
    public class ResourceDef : ScriptableObject
    {
        [Tooltip("저장 파일에 쓰이는 고유 ID. 한 번 정하면 바꾸지 말 것.")]
        public string id;
        public string displayName;
        public Sprite icon;
        public Color placeholderColor = Color.white;
    }
}
