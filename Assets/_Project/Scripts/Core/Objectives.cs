using System;
using System.Collections.Generic;
using System.Linq;

namespace Husamguk.Core
{
    /// <summary>
    /// 차례로 이어지는 목표 하나 (GDD §1.4 Meso 목표 · §3.20 초반 온보딩).
    /// 진행도가 target에 닿으면 보상을 받고 다음 목표가 열린다.
    /// </summary>
    public class Objective
    {
        public string id;
        public string title;
        public string description;
        public string rewardText;
        public int target = 1;
        public Func<GameManager, int> progress;
        public Action<GameManager> reward;
        [NonSerialized] public string jumpGroup;
        [NonSerialized] public string jumpSubTab;
    }

    public static class ObjectiveBook
    {
        public static readonly List<Objective> All = new()
        {
            new Objective
            {
                id = "build", title = "가문의 기틀을 다져라",
                description = "논 개간이나 저수지 정비를 한 번 시작하십시오. 곳간이 차야 무엇이든 할 수 있습니다.",
                progress = gm => gm.HasFlag("built_once") ? 1 : 0,
                reward = gm => gm.Gain(0, 0, 3), rewardText = "권위 +3",
                jumpGroup = "영지", jumpSubTab = "경제·농업",
            },
            new Objective
            {
                id = "troops", title = "사병을 길러라", target = 25,
                description = "병력을 25명 이상 모으십시오. 난세의 호족들은 칼 가진 자의 말만 듣습니다. (민심이 좋은 고을에서만 장정이 모입니다)",
                progress = gm => gm.TotalTroops(),
                reward = gm => gm.Gain(0, 40, 0), rewardText = "엽전 +40",
                jumpGroup = "군사", jumpSubTab = "병영·징집",
            },
            new Objective
            {
                id = "first_land", title = "첫 영지를 얻어라", target = 2,
                description = "이웃 호족 하나를 회유해 귀부시키거나(충성 80), 토벌해 영지를 2곳으로 늘리십시오.",
                progress = gm => gm.OwnedCount,
                reward = gm => gm.Gain(50, 0, 5), rewardText = "석 +50 · 권위 +5",
                jumpGroup = "호족·신하", jumpSubTab = "호족 판도",
            },
            new Objective
            {
                id = "promote", title = "고을을 키워라",
                description = "영지 하나를 다음 거점 등급으로 승격시키십시오. 세수가 쌓이고 병력이 주둔하면 계절이 바뀔 때 자동으로 승격합니다.",
                progress = gm => gm.HasFlag("promoted_once") ? 1 : 0,
                reward = gm => gm.Gain(0, 60, 0), rewardText = "엽전 +60",
                jumpGroup = "영지", jumpSubTab = "개요",
            },
            new Objective
            {
                id = "expand", title = "세력을 넓혀라", target = 4,
                description = "영지를 4곳으로 늘리십시오. 회유는 느리지만 사병이 고스란히 따라오고 민심도 좋습니다. 토벌은 빠르지만 다른 호족들이 등을 돌리고, 빼앗은 고을은 민심을 달랠 때까지 수확도 장정도 적습니다.",
                progress = gm => gm.OwnedCount,
                reward = gm => gm.Gain(0, 0, 10), rewardText = "권위 +10",
                jumpGroup = "호족·신하", jumpSubTab = "호족 판도",
            },
            new Objective
            {
                id = "wansanju", title = "완산주를 손에 넣어라",
                description = "견훤의 직할지 완산주를 차지하십시오. 회유는 통하지 않습니다 — 창병·궁병·기병이 섞인 정예군이 지키고 있으니, 상성을 맞춘 대군이 필요합니다.",
                progress = gm => gm.Provinces.Any(p => gm.IsStronghold(p) && p.ownerFactionId == gm.PlayerFactionId) ? 1 : 0,
                reward = gm => gm.Gain(0, 0, 20), rewardText = "권위 +20",
                jumpGroup = "군사", jumpSubTab = "출진",
            },
            new Objective
            {
                id = "unify", title = "완산주 일대를 평정하라",
                description = "여덟 고을을 모두 주군의 깃발 아래 두십시오.",
                progress = gm => gm.OwnedCount,
                reward = gm => { }, rewardText = "엔딩",
                jumpGroup = "호족·신하", jumpSubTab = "호족 판도",
            },
        };

        /// <summary>마지막 목표(전 지역 평정)의 target은 지역 수에 맞춘다.</summary>
        public static int TargetFor(Objective o, GameManager gm) => o.id == "unify" ? gm.Provinces.Count : o.target;
    }
}
