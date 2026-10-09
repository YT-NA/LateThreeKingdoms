using System.Collections.Generic;
using UnityEngine;

namespace Husamguk.Data
{
    /// <summary>거점 등급 하나의 정의 (촌/현/군/주/경). GDD §6.1, §5.6.</summary>
    [CreateAssetMenu(menuName = "Husamguk/Settlement Tier", fileName = "Tier_")]
    public class SettlementTierDef : ScriptableObject
    {
        public string id;
        [Tooltip("표시 이름 (예: 현)")]
        public string displayName;
        [Tooltip("한자 표기 (예: 縣)")]
        public string hanja;
        [Tooltip("이 등급으로 오르면 임명할 수 있는 관직 이름 (예: 현령/현감)")]
        public string officeTitle;

        [Header("승격 임계치 (다음 등급으로 가려면 이 등급에서 넘어야 하는 값)")]
        [Min(0)] public int populationThreshold;
        [Tooltip("군사적 중요도: 주둔 병력 수 또는 누적 승전 수")]
        [Min(0)] public int militaryThreshold;
        [Tooltip("경제력: 누적 세수")]
        [Min(0)] public int economyThreshold;

        [Header("승격 시 해금")]
        [Range(0f, 2f)] public float taxBonus = 0.15f;
        public List<BuildingUnlock> unlockedBuildings = new();
        public List<UnitDef> unlockedUnits = new();

        [System.Serializable]
        public class BuildingUnlock
        {
            public string buildingId;
            public string displayName;
        }
    }
}
