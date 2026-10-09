using UnityEngine;

namespace Husamguk.Data
{
    /// <summary>세력(고려/후백제/신라 계열 등) 하나의 정의. GDD §2.</summary>
    [CreateAssetMenu(menuName = "Husamguk/Faction", fileName = "Faction_")]
    public class FactionDef : ScriptableObject
    {
        public string id;
        [Tooltip("국호 (예: 고려, 후백제)")]
        public string displayName;
        [Tooltip("지도·UI에 쓰이는 세력 색")]
        public Color mapColor = Color.gray;
        [Tooltip("상단 바·지도에 쓰이는 세력 깃발")]
        public Sprite banner;
        public bool isPlayable = true;
        [TextArea] public string description;
    }
}
