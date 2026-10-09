using System;
using System.Collections.Generic;

namespace Husamguk.Core
{
    public enum RetainerStatus { Free, Office, ProvinceAppointment, Captive }

    [Serializable]
    public class UnitCount
    {
        public string unitId;
        public int count;
    }

    /// <summary>지역(ProvinceDef) 하나의 런타임 상태. GDD §7.</summary>
    [Serializable]
    public class ProvinceState
    {
        public string provinceId;
        [Tooltip("비어 있으면 미복속 호족 소유")]
        public string ownerFactionId;
        public string tierId;

        public int agricultureLevel = 1;
        public int irrigationLevel = 1;
        public float taxRate = 0.2f;

        public List<UnitCount> garrison = new();

        [Tooltip("호족이 다스리는 지역일 때의 충성도 (0~100)")]
        public int loyalty = 50;
        public string grievance;

        [Tooltip("진행 중인 건설(논/저수지 등). 비어 있으면 없음")]
        public string constructionId;
        public int constructionSeasonsLeft;

        [Tooltip("거점 승격 판정용 누적치 (§5.6)")]
        public int cumulativeMilitary;
        public int cumulativeEconomy;

        [Tooltip("민심 관리 (§5.7, MVP 축소판)")]
        public int publicSentiment = 60;
        public int security = 60;
    }

    /// <summary>세력(FactionDef) 하나의 런타임 상태.</summary>
    [Serializable]
    public class FactionState
    {
        public string factionId;
        public int grain;
        public int coin;
        public int prestige;
        public string monarchRetainerId;
        public List<string> retainerIds = new();
    }

    /// <summary>신하(RetainerDef) 하나의 런타임 상태 — 왕족(후계자)도 isHeir로 같은 틀을 쓴다.</summary>
    [Serializable]
    public class RetainerState
    {
        public string retainerId;
        public string factionId;
        public RetainerStatus status = RetainerStatus.Free;
        public string officeId;
        public string assignedProvinceId;
        public int loyalty = 60;
        public int level = 1;
        public int xp;
        public int age = 20;

        [Tooltip("혼인으로 영구 동맹을 맺은 호족/가문 id (§3.14)")]
        public string marriedIntoId;

        [Header("교육으로 쌓인 스탯 증가분 (§3.15) — 기본치(RetainerDef)에 더한다")]
        public int leadershipGrowth, mightGrowth, intellectGrowth, scholarshipGrowth, politicsGrowth, charmGrowth, virtueGrowth;
        public int educationActionsLeftThisSeason;
        public string tutorRetainerId;

        [Header("후계자 전용 (§5.4)")]
        public float legitimacyScore;
        public float supportPercent;
        public bool isCrownPrince;
    }
}
