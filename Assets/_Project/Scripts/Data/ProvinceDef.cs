using System.Collections.Generic;
using UnityEngine;

namespace Husamguk.Data
{
    /// <summary>지역(거점) 하나의 설계도. GDD §3.0, §7.</summary>
    [CreateAssetMenu(menuName = "Husamguk/Province", fileName = "Province_")]
    public class ProvinceDef : ScriptableObject
    {
        [Tooltip("저장 파일에 쓰이는 고유 ID. 한 번 정하면 바꾸지 말 것.")]
        public string id;
        public string displayName;
        [Tooltip("지도 화면(§3.0)에서의 위치 (임의 좌표계)")]
        public Vector2 mapPosition;
        [Tooltip("인접한 지역들의 id — 원정·행군이 가능한 경로")]
        public List<string> adjacentProvinceIds = new();

        [Header("시작 상태")]
        public FactionDef startingFaction;
        [Tooltip("시작 소속 세력이 없으면(미복속 호족) 여기에 호족 이름을 적는다")]
        public string startingHojokName;
        public SettlementTierDef startingTier;
        [Min(0)] public int startingPopulation = 50;

        [Header("지역 특산 (§2.6, 2차 범위 — MVP는 완산주만 사용 가능)")]
        [TextArea] public string specialty;
        [Range(0f, 1f)] public float grainBonus;
    }
}
