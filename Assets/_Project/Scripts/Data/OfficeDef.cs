using UnityEngine;

namespace Husamguk.Data
{
    public enum OfficeField { Administration, Military, Diplomacy }

    /// <summary>중앙 조정의 핵심 관직(대상서/대장군/내사령) 하나의 정의. GDD §3.3.</summary>
    [CreateAssetMenu(menuName = "Husamguk/Office", fileName = "Office_")]
    public class OfficeDef : ScriptableObject
    {
        public string id;
        [Tooltip("관직 이름 (예: 대상서)")]
        public string displayName;
        public OfficeField field;
        [TextArea] public string effectDescription;
        [Tooltip("효과 수치 (예: 0.15 = 내정 효율 +15%)")]
        public float effectValue;
    }
}
