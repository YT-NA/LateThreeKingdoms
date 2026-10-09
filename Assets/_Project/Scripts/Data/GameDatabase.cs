using System.Collections.Generic;
using UnityEngine;

namespace Husamguk.Data
{
    /// <summary>게임에 존재하는 모든 정의 목록 + MVP 밸런스 수치. GDD §7.</summary>
    [CreateAssetMenu(menuName = "Husamguk/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public List<ScenarioDef> scenarios = new();
        public List<FactionDef> factions = new();
        public List<SettlementTierDef> settlementTiers = new();
        public List<ProvinceDef> provinces = new();
        public List<UnitDef> units = new();
        public List<OfficeDef> offices = new();
        public List<RetainerDef> retainers = new();

        [Header("자원 (§5.1)")]
        public ResourceDef grain;   // 석
        public ResourceDef coin;    // 엽전
        public ResourceDef prestige; // 권위

        [Header("계절 (§4)")]
        [Tooltip("1년을 이루는 계절 수")]
        [Min(1)] public int seasonsPerYear = 4;

        [Header("호족 충성도 (§5.2)")]
        [Range(0, 100)] public int loyaltyRevoltThreshold = 25;
        [Min(0)] public int appeaseLoyaltyGain = 10;
        [Min(0)] public int appeaseCoinCost = 30;
        [Min(0)] public int marriagePrestigeCost = 40;
        [Min(0)] public int grantSurnamePrestigeCost = 20;
        [Min(0)] public int grantSurnameLoyaltyGain = 15;

        [Header("전투 (§5.5)")]
        [Min(1f)] public float counterBonus = 1.5f;
        [Range(0.1f, 1f)] public float counterPenalty = 0.67f;

        [Header("왕위 계승 (§5.4, §5.9)")]
        [Min(0)] public int firstbornLegitimacyBonus = 20;
        [Min(1)] public int heirAdultAge = 15;
        [Min(1)] public int monarchOldAge = 50;
        [Range(0f, 1f)] public float deathChancePerSeasonAfterOldAge = 0.03f;

        [Header("민심 (§5.7, MVP 축소판)")]
        [Range(0, 100)] public int unrestRevoltThreshold = 20;

        Dictionary<string, ProvinceDef> _provinceById;
        Dictionary<string, FactionDef> _factionById;
        Dictionary<string, RetainerDef> _retainerById;
        Dictionary<string, UnitDef> _unitById;

        public ProvinceDef GetProvince(string id)
        {
            _provinceById ??= BuildIndex(provinces, p => p.id);
            return id != null && _provinceById.TryGetValue(id, out var p) ? p : null;
        }

        public FactionDef GetFaction(string id)
        {
            _factionById ??= BuildIndex(factions, f => f.id);
            return id != null && _factionById.TryGetValue(id, out var f) ? f : null;
        }

        public RetainerDef GetRetainer(string id)
        {
            _retainerById ??= BuildIndex(retainers, r => r.id);
            return id != null && _retainerById.TryGetValue(id, out var r) ? r : null;
        }

        public UnitDef GetUnit(string id)
        {
            _unitById ??= BuildIndex(units, u => u.id);
            return id != null && _unitById.TryGetValue(id, out var u) ? u : null;
        }

        /// <summary>등급 순서상 다음 거점 등급 (최고 등급이면 null). settlementTiers는 촌→경 순서로 등록돼 있어야 한다.</summary>
        public SettlementTierDef NextTier(SettlementTierDef current)
        {
            int i = settlementTiers.IndexOf(current);
            if (i < 0 || i + 1 >= settlementTiers.Count) return null;
            return settlementTiers[i + 1];
        }

        static Dictionary<string, T> BuildIndex<T>(List<T> list, System.Func<T, string> key) where T : Object
        {
            var dict = new Dictionary<string, T>();
            foreach (var item in list)
            {
                if (item == null) continue;
                var k = key(item);
                if (string.IsNullOrEmpty(k))
                    Debug.LogWarning($"[GameDatabase] '{item.name}'에 id가 비어 있습니다.", item);
                else if (!dict.TryAdd(k, item))
                    Debug.LogWarning($"[GameDatabase] 중복 id '{k}' ({item.name})", item);
            }
            return dict;
        }

        void OnValidate()
        {
            _provinceById = null;
            _factionById = null;
            _retainerById = null;
            _unitById = null;
        }
    }
}
