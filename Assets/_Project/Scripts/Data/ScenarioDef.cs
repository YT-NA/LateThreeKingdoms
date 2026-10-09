using System.Collections.Generic;
using UnityEngine;

namespace Husamguk.Data
{
    /// <summary>시나리오(시대) 하나의 정의 — 시작 연도와 그 시점에 고를 수 있는 인물 목록. GDD §3.18.</summary>
    [CreateAssetMenu(menuName = "Husamguk/Scenario", fileName = "Scenario_")]
    public class ScenarioDef : ScriptableObject
    {
        public string id;
        public string displayName;
        [Tooltip("서기 연도 (예: 900)")]
        public int year;
        [TextArea] public string description;

        public List<PlayableCharacter> playableCharacters = new();

        [System.Serializable]
        public class PlayableCharacter
        {
            public RetainerDef retainer;
            public FactionDef startingFaction;
            [Tooltip("이 인물이 시작하는 지역 (플레이어 수도로 삼음)")]
            public ProvinceDef startingProvince;
            [TextArea] public string startingSituation;
            [Tooltip("플레이 성향 한 줄 (예: 회유형 — 호족 포섭이 쉽다)")]
            public string playstyle;
        }
    }
}
