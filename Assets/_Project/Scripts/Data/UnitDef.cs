using UnityEngine;

namespace Husamguk.Data
{
    /// <summary>병종 상성 분류. 창병 > 기병 > 궁병 > 창병 (RuleTheKingdom과 동일한 가위바위보 패턴).</summary>
    public enum UnitClass { None, Spear, Cavalry, Archer }

    public static class UnitClasses
    {
        /// <summary>a가 b를 상성으로 이기는지.</summary>
        public static bool Beats(UnitClass a, UnitClass b) =>
            (a == UnitClass.Spear && b == UnitClass.Cavalry) ||
            (a == UnitClass.Cavalry && b == UnitClass.Archer) ||
            (a == UnitClass.Archer && b == UnitClass.Spear);
    }

    /// <summary>병종(보병/기병/궁병/창병) 하나의 정의. GDD §3.9, §3.11, §5.5.</summary>
    [CreateAssetMenu(menuName = "Husamguk/Unit", fileName = "Unit_")]
    public class UnitDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public UnitClass unitClass = UnitClass.None;
        public Sprite icon;

        [Header("모집 (병영·징집, §3.9)")]
        public ResourceAmount[] recruitCost;
        [Min(1)] public int recruitSeasons = 1;

        [Header("전투 (§5.5)")]
        [Min(1)] public int power = 10;

        [Header("행군 (출진, §3.11)")]
        [Tooltip("출진 시 1인당 소모하는 식량(석)")]
        [Min(0)] public int foodUpkeepPerSoldier = 4;
    }
}
