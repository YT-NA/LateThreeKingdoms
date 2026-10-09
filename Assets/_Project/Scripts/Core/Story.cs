using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Husamguk.Core
{
    /// <summary>사건 하나의 선택지. 비용(석/엽전/권위)은 엔진이 확인·차감하고, 나머지 효과는 apply가 처리한 뒤 결과 서술을 돌려준다.</summary>
    public class StoryChoice
    {
        public string text;
        public string effect;
        public int grain, coin, prestige;
        public int minTroops;
        public Func<GameManager, string> apply;

        public string CostText()
        {
            var parts = new List<string>();
            if (grain > 0) parts.Add($"석 -{grain}");
            if (coin > 0) parts.Add($"엽전 -{coin}");
            if (prestige > 0) parts.Add($"권위 -{prestige}");
            if (minTroops > 0) parts.Add($"병력 {minTroops}명 이상 필요");
            if (!string.IsNullOrEmpty(effect)) parts.Add(effect);
            return string.Join("  ·  ", parts);
        }
    }

    /// <summary>
    /// 계절이 넘어갈 때 조건이 맞으면 뜨는 사건 (GDD §1.3 딜레마형 선택지, §3.20 초반 온보딩, §3.21 책사).
    /// 플레이어가 선택하기 전에는 다음 계절로 넘어갈 수 없다 (§3.19 "긴급 현안").
    /// </summary>
    public class StoryEvent
    {
        public string id;
        public string title;
        public string speakerId;
        public string speakerName;
        public bool urgent;
        public bool repeatable;
        public int cooldownTurns;
        public Func<GameManager, bool> trigger;
        public Func<GameManager, string> body;
        public List<StoryChoice> choices = new();
    }

    /// <summary>한국어 조사 (받침 유무).</summary>
    public static class Ko
    {
        static int Final(string w)
        {
            if (string.IsNullOrEmpty(w)) return 0;
            char c = w[w.Length - 1];
            if (c < 0xAC00 || c > 0xD7A3) return 0;
            return (c - 0xAC00) % 28;
        }

        public static string Eul(string w) => w + (Final(w) != 0 ? "을" : "를");
        public static string I(string w) => w + (Final(w) != 0 ? "이" : "가");
        public static string Eun(string w) => w + (Final(w) != 0 ? "은" : "는");
        public static string Wa(string w) => w + (Final(w) != 0 ? "과" : "와");
        public static string Ro(string w) { int f = Final(w); return w + (f != 0 && f != 8 ? "으로" : "로"); }
    }

    /// <summary>완산주 일대 시나리오(900년)의 사건 목록. 목록 순서가 우선순위다 (엔딩 → 위기 → 각본 사건 → 계절 사건).</summary>
    public static class StoryBook
    {
        public static readonly List<StoryEvent> All = Build();

        public static StoryEvent Find(string id) => string.IsNullOrEmpty(id) ? null : All.FirstOrDefault(e => e.id == id);

        const string Advisor = "choieung";
        const string AdvisorName = "책사 최응";

        static string Cap(GameManager gm) => gm.Capital != null ? gm.ProvinceName(gm.Capital.provinceId) : "본거지";
        static string Target(GameManager gm) => gm.ProvinceName(gm.EventTarget);
        static string TargetHojok(GameManager gm) => gm.GetProvince(gm.EventTarget) is { } p ? gm.HojokName(p) : "호족";
        static string Monarch(GameManager gm) => gm.MonarchDef != null ? gm.MonarchDef.displayName : "주군";

        static List<UnitCount> Army(params (string unit, int count)[] units) =>
            units.Select(u => new UnitCount { unitId = u.unit, count = u.count }).ToList();

        static List<StoryEvent> Build() => new()
        {
            // ───────── 엔딩 ─────────
            new StoryEvent
            {
                id = "ending_victory", title = "완산주 일대 평정", speakerId = Advisor, speakerName = AdvisorName, urgent = true,
                trigger = gm => gm.IsVictory,
                body = gm =>
                    $"{gm.Year}년 {GameManager.SeasonNames[gm.Season]}. 마침내 완산주 성루에 주군의 깃발이 올랐습니다.\n\n" +
                    $"이름 없는 {Cap(gm)}의 촌주로 일어선 지 {gm.TurnNumber}계절 — 호남 들녘의 여덟 고을이 모두 {Monarch(gm)} 공의 이름 아래 하나가 되었습니다.\n\n" +
                    "허나 이것은 시작일 뿐입니다. 북쪽에선 궁예가 칼을 갈고, 동쪽에선 천 년 신라가 마지막 숨을 고르고 있지요. 천하는 아직 주인을 기다리고 있습니다.",
                choices =
                {
                    new StoryChoice { text = "이 땅을 더 둘러본다", effect = "계속 플레이", apply = gm => "연대기에 이 날이 기록되었습니다." },
                    new StoryChoice { text = "새로운 난세로 (처음부터)", effect = "시나리오 선택으로", apply = gm => { gm.SetFlag(GameManager.RestartFlag); return "새로운 이야기가 기다립니다."; } },
                },
            },
            new StoryEvent
            {
                id = "ending_defeat", title = "가문의 몰락", speakerId = Advisor, speakerName = AdvisorName, urgent = true,
                trigger = gm => gm.IsDefeated,
                body = gm =>
                    "마지막 성마저 넘어갔습니다.\n\n" +
                    $"{Monarch(gm)} 공의 깃발은 불타고, 가문의 이름은 호남 들녘의 바람 속으로 흩어졌습니다. 난세는 패자의 이름을 오래 기억하지 않습니다.\n\n" +
                    "…허나 사람이 살아 있는 한, 다시 일어설 날도 있는 법이지요.",
                choices =
                {
                    new StoryChoice { text = "처음부터 다시", apply = gm => { gm.SetFlag(GameManager.RestartFlag); return "다시 일어섭니다."; } },
                },
            },

            // ───────── 위기 ─────────
            new StoryEvent
            {
                id = "revolt", title = "민란의 조짐", speakerId = Advisor, speakerName = AdvisorName, repeatable = true, cooldownTurns = 2,
                trigger = gm =>
                {
                    var p = gm.OwnedProvinces.Where(x => x.loyalty < gm.Database.loyaltyRevoltThreshold).OrderBy(x => x.loyalty).FirstOrDefault();
                    if (p == null) return false;
                    gm.EventTarget = p.provinceId;
                    return true;
                },
                body = gm =>
                    $"{Target(gm)}에서 급보입니다!\n\n무거운 세금과 부역에 지친 백성들이 낫과 쇠스랑을 들고 관아 앞에 모여들고 있습니다. " +
                    $"(충성 {gm.GetProvince(gm.EventTarget)?.loyalty})\n\n이대로 두면 성을 잃을 수도 있습니다. 어찌하시겠습니까?",
                choices =
                {
                    new StoryChoice
                    {
                        text = "군사를 보내 진압한다", minTroops = 5, effect = "병력 일부 손실 · 충성 +20",
                        apply = gm =>
                        {
                            int lost = gm.LoseTroopsAt(gm.EventTarget, 0.2f);
                            gm.ChangeLoyalty(gm.GetProvince(gm.EventTarget), 20);
                            return $"병사 {lost}명이 다치거나 죽었으나 소요는 가라앉았습니다. 백성들의 눈빛엔 원망이 남았습니다.";
                        },
                    },
                    new StoryChoice
                    {
                        text = "곳간을 열어 백성을 달랜다", grain = 50, effect = "충성 +25 · 이 고을 세율 -5%",
                        apply = gm =>
                        {
                            var p = gm.GetProvince(gm.EventTarget);
                            gm.ChangeLoyalty(p, 25);
                            gm.SetTaxRate(p.provinceId, p.taxRate - 0.05f);
                            return "곳간 문이 열리자 원성이 감사로 바뀌었습니다. \"우리 주인은 그래도 사람을 안다\"는 말이 돕니다.";
                        },
                    },
                    new StoryChoice
                    {
                        text = "내버려 둔다", effect = "운에 맡긴다 — 성을 잃을 수 있음",
                        apply = gm =>
                        {
                            var p = gm.GetProvince(gm.EventTarget);
                            if (UnityEngine.Random.value < 0.5f)
                            {
                                gm.LoseProvinceToRebels(p);
                                return $"성난 백성들이 관아를 불태웠습니다. {Ko.Eun(Target(gm))} 더 이상 주군의 땅이 아닙니다.";
                            }
                            gm.ChangeLoyalty(p, 5);
                            return "소요는 저절로 잦아들었습니다. 이번엔 운이 좋았습니다.";
                        },
                    },
                },
            },
            new StoryEvent
            {
                id = "gyeonhwon_raid", title = "후백제군 남하", speakerId = "gyeonhwon", speakerName = "후백제 왕 견훤", repeatable = true, cooldownTurns = 5,
                trigger = gm =>
                {
                    if (!gm.HasFlag("gyeonhwon_hostile") || gm.HasFlag("gyeonhwon_vassal")) return false;
                    if (System.Math.Min(gm.TurnsSinceFired("envoy_gyeonhwon"), gm.TurnsSinceFired("envoy_gyeonhwon_2")) < 3) return false;
                    var stronghold = gm.Provinces.FirstOrDefault(p => gm.IsStronghold(p));
                    if (stronghold == null || stronghold.ownerFactionId == gm.PlayerFactionId) return false;
                    var target = gm.AdjacentTo(stronghold).Where(p => p.ownerFactionId == gm.PlayerFactionId).OrderBy(p => gm.GarrisonTotal(p)).FirstOrDefault() ?? gm.Capital;
                    if (target == null) return false;
                    gm.EventTarget = target.provinceId;
                    return true;
                },
                body = gm =>
                    $"완산주 쪽 하늘에 흙먼지가 자욱합니다. 후백제의 창병 {RaidSize(gm)}명이 {Ko.Ro(Target(gm))} 밀려오고 있습니다!\n\n" +
                    $"\"감히 내 사신을 내쫓은 놈이 누구냐. 그 성벽이 얼마나 단단한지 보자.\"\n\n" +
                    $"{Target(gm)}의 수비병은 {gm.GarrisonTotal(gm.GetProvince(gm.EventTarget))}명입니다.",
                choices =
                {
                    new StoryChoice
                    {
                        text = "성문을 열고 맞서 싸운다", effect = "전투 — 이기면 권위 +8",
                        apply = gm =>
                        {
                            bool won = gm.DefendAt(gm.EventTarget, Army(("spear", RaidSize(gm)), ("cavalry", 4)), out int lost);
                            if (won) { gm.Gain(0, 0, 8); return $"아군 {lost}명을 잃었으나 후백제군을 물리쳤습니다! \"견훤도 이 성은 못 넘는다\"는 노래가 퍼집니다."; }
                            gm.ChangeLoyalty(gm.GetProvince(gm.EventTarget), -15);
                            int taken = gm.Plunder(0.3f);
                            return $"아군 {lost}명을 잃고 성 안으로 물러났습니다. 후백제군이 들판을 휩쓸고 곡식 {taken}석을 빼앗아 갔습니다.";
                        },
                    },
                    new StoryChoice
                    {
                        text = "성문을 걸어 잠그고 버틴다", grain = 60, effect = "이 고을 충성 -5",
                        apply = gm => { gm.ChangeLoyalty(gm.GetProvince(gm.EventTarget), -5); return "열흘을 버티자 후백제군은 들판만 짓밟고 돌아갔습니다."; },
                    },
                    new StoryChoice
                    {
                        text = "예물을 바쳐 돌려보낸다", coin = 60, effect = "권위 -3",
                        apply = gm => { gm.Gain(0, 0, -3); return "견훤의 장수가 예물 상자를 열어 보고 껄껄 웃으며 군사를 물렸습니다."; },
                    },
                },
            },

            // ───────── 고을 승격 · 귀부 ─────────
            new StoryEvent
            {
                id = "promotion", title = "고을의 승격", speakerId = Advisor, speakerName = AdvisorName, repeatable = true,
                trigger = gm =>
                {
                    var id = gm.PeekPromotion();
                    if (id == null) return false;
                    gm.EventTarget = id;
                    return true;
                },
                body = gm =>
                {
                    var p = gm.GetProvince(gm.EventTarget);
                    var tier = gm.Tier(p);
                    return $"경하드립니다, 주군! {Ko.I(Target(gm))} {Ko.Ro(tier?.displayName ?? "")} 승격했습니다.\n\n" +
                           $"성벽이 높아지고 장터가 넓어졌습니다. 이제 이 고을의 수장은 '{tier?.officeTitle}'(으)로 불립니다.\n\n" +
                           "고을이 커질수록 수확(+15%)과 한 계절에 모을 수 있는 장정의 수가 늘어납니다.";
                },
                choices =
                {
                    new StoryChoice { text = "잔치를 열어 경하한다", grain = 20, effect = "이 고을 충성 +10", apply = gm => { gm.ChangeLoyalty(gm.GetProvince(gm.EventTarget), 10); gm.PopPromotion(); return "풍악 소리가 밤늦도록 이어졌습니다."; } },
                    new StoryChoice { text = "들뜨지 않고 다음을 준비한다", effect = "권위 +2", apply = gm => { gm.Gain(0, 0, 2); gm.PopPromotion(); return "주군의 담담함에 신하들이 고개를 숙였습니다."; } },
                },
            },
            new StoryEvent
            {
                id = "first_submission", title = "칼 없이 얻은 땅", speakerId = Advisor, speakerName = AdvisorName,
                trigger = gm => gm.LastSubmittedId != null && gm.GetProvince(gm.LastSubmittedId)?.ownerFactionId == gm.PlayerFactionId && (gm.EventTarget = gm.LastSubmittedId) != null,
                body = gm =>
                    $"{Ko.I(TargetHojok(gm))} 스스로 성문을 열었습니다!\n\n\"이제부터 우리 가문은 {Monarch(gm)} 공을 받들겠습니다.\"\n\n" +
                    "칼 한 번 뽑지 않고 얻은 땅입니다. 그들의 사병도 고스란히 주군의 군사가 되었지요. 이 소식은 호남 들녘 끝까지 퍼질 것입니다 — 다른 호족들도 저울질을 시작하겠지요.",
                choices =
                {
                    new StoryChoice { text = "성대히 맞이한다", grain = 30, effect = "이 고을 충성 +15 · 권위 +5", apply = gm => { gm.ChangeLoyalty(gm.GetProvince(gm.EventTarget), 15); gm.Gain(0, 0, 5); return "새 가신들이 감격해 엎드렸습니다."; } },
                    new StoryChoice { text = "조용히 받아들인다", effect = "권위 +2", apply = gm => { gm.Gain(0, 0, 2); return "요란하지 않게, 그러나 확실하게 — 땅이 하나 늘었습니다."; } },
                },
            },

            // ───────── 각본 사건 (초반 온보딩 §3.20) ─────────
            new StoryEvent
            {
                id = "prologue", title = "난세의 첫걸음", speakerId = Advisor, speakerName = AdvisorName,
                trigger = gm => gm.TurnNumber == 1,
                body = gm =>
                    $"주군, {gm.Year}년 봄입니다.\n\n" +
                    "견훤이 완산주에 도읍을 정하고 백제의 부흥을 외친 지 몇 달 — 호남 들녘의 호족들은 저마다 성문을 걸어 잠그고 눈치만 보고 있습니다.\n\n" +
                    $"주군께서 다스리시는 {Ko.Eun(Cap(gm))} 아직 작은 {gm.TierName(gm.Capital)}일 뿐이나, 난세에는 작은 성에서 큰 나라가 일어나는 법이지요.\n\n" +
                    "이 최응, 주군의 곁에서 길을 밝히겠습니다. 첫걸음을 어디로 내딛으시겠습니까?",
                choices =
                {
                    new StoryChoice
                    {
                        text = "이웃 호족들을 불러 잔치를 연다", grain = 40, effect = "이웃 호족 충성 +15",
                        apply = gm =>
                        {
                            foreach (var p in gm.HojokProvinces.Where(x => gm.IsAdjacentToPlayer(x)).ToList()) gm.ChangeLoyalty(p, 15);
                            return $"술잔이 몇 순배 돌자 굳었던 호족들의 얼굴이 풀렸습니다. \"{Cap(gm)}의 주인은 사람을 아낄 줄 안다\"는 말이 고을마다 퍼집니다.";
                        },
                    },
                    new StoryChoice
                    {
                        text = "장정을 모아 성벽을 보수한다", grain = 30, effect = "보병 +8",
                        apply = gm => { gm.AddTroops(gm.Capital.provinceId, "infantry", 8); return "망치 소리가 사흘 밤낮 이어졌습니다. 성벽 위에 창을 든 장정 여덟이 새로 섰습니다."; },
                    },
                    new StoryChoice
                    {
                        text = "견훤에게 예물을 보내 몸을 낮춘다", coin = 30, effect = "후백제의 압박이 늦춰짐 · 호족 충성 -5",
                        apply = gm =>
                        {
                            gm.SetFlag("tribute_sent");
                            gm.ChangeAllHojokLoyalty(-5);
                            return "견훤의 사자가 예물을 받아 들고 돌아갔습니다. 당분간 완산주의 칼끝이 이쪽을 향하진 않겠지요. 다만 호족들 사이에선 '견훤의 개'라는 수군거림이 돕니다.";
                        },
                    },
                },
            },
            new StoryEvent
            {
                id = "envoy_gyeonhwon", title = "견훤의 사신", speakerId = "gyeonhwon", speakerName = "후백제 사신 (견훤의 서신)",
                trigger = gm => gm.TurnNumber >= (gm.HasFlag("tribute_sent") ? 10 : 6) || gm.OwnedCount >= 3,
                body = gm =>
                    "후백제 왕 견훤의 사신이 붉은 깃발을 앞세우고 성문 앞에 섰습니다. 그가 펼쳐 든 서신에는 이렇게 적혀 있습니다.\n\n" +
                    "\"완산주의 주인이 누구인지 잊지 마라. 해마다 공물을 바치고 신하의 예를 갖추면 너희 땅을 보전해 주겠다. 거역한다면 — 그 성벽이 얼마나 버틸지 두고 보겠다.\"\n\n" +
                    "사신의 눈길이 성벽 위 우리 병사들을 천천히 훑습니다.",
                choices = EnvoyChoices(true),
            },
            new StoryEvent
            {
                id = "envoy_gyeonhwon_2", title = "견훤의 재촉", speakerId = "gyeonhwon", speakerName = "후백제 사신 (견훤의 서신)",
                trigger = gm => gm.HasFlag("envoy_delayed") && gm.TurnsSinceFired("envoy_gyeonhwon") >= 5,
                body = gm =>
                    "지난번 극진한 대접에 웃으며 돌아갔던 견훤의 사신이 다시 왔습니다. 이번엔 웃음기가 없습니다.\n\n" +
                    "\"우리 왕께서 기다림에 지치셨다. 이번엔 대답을 듣고 가겠다.\"",
                choices = EnvoyChoices(false),
            },
            new StoryEvent
            {
                id = "drifters", title = "성문 앞의 유랑민", speakerId = Advisor, speakerName = AdvisorName,
                trigger = gm => gm.TurnNumber >= 3,
                body = gm =>
                    $"흉년에 쫓긴 유랑민 수십 명이 {Cap(gm)} 성문 앞에 모여들었습니다.\n\n" +
                    "아이를 업은 아낙, 뼈만 남은 노인, 그리고 아직 일할 기운이 남은 장정들… 성문지기가 주군의 명을 기다립니다.\n\n" +
                    "모두 받으면 곳간이 축나고, 내치면 인심을 잃습니다.",
                choices =
                {
                    new StoryChoice
                    {
                        text = "모두 받아들인다", grain = 40, effect = "보병 +10 · 모든 영지 충성 +5",
                        apply = gm => { gm.AddTroops(gm.Capital.provinceId, "infantry", 10); gm.ChangeAllOwnedLoyalty(5); return "유랑민들이 눈물을 흘리며 절을 올렸습니다. 장정들은 기꺼이 창을 들겠다 합니다."; },
                    },
                    new StoryChoice
                    {
                        text = "장정만 가려 받는다", prestige = 2, effect = "보병 +6",
                        apply = gm => { gm.AddTroops(gm.Capital.provinceId, "infantry", 6); return "장정들은 성 안으로, 늙고 어린 이들은 다시 길 위로. 실리는 챙겼으나 뒷말이 남았습니다."; },
                    },
                    new StoryChoice
                    {
                        text = "성문을 닫는다", effect = "이웃 호족 충성 -3",
                        apply = gm => { gm.ChangeAllHojokLoyalty(-3); return "성문이 닫혔습니다. 유랑민들은 이웃 고을로 흩어지며 주군을 원망했습니다."; },
                    },
                },
            },
            new StoryEvent
            {
                id = "bandits", title = "고갯길의 도적떼", speakerId = null, speakerName = null,
                trigger = gm => gm.TurnNumber >= 4,
                body = gm =>
                    $"{Cap(gm)}에서 이웃 고을로 넘어가는 고갯길에 도적떼 일곱이 진을 쳤습니다.\n\n" +
                    "장사꾼의 발길이 끊겨 장터가 썰렁합니다. 도적 두목은 원래 몰락한 호족 집안의 무사였다는 소문입니다.",
                choices =
                {
                    new StoryChoice
                    {
                        text = "군사를 보내 토벌한다", minTroops = 5, effect = "전투 — 이기면 엽전 +50 · 권위 +3",
                        apply = gm =>
                        {
                            bool won = gm.AttackNeutral(gm.Capital.provinceId, Army(("infantry", 7)), out int lost);
                            if (won) { gm.Gain(0, 50, 3); return $"아군 {lost}명을 잃고 도적떼를 쓸어버렸습니다. 빼앗겼던 재물 엽전 50을 되찾았습니다."; }
                            return $"도적들의 매복에 걸려 아군 {lost}명을 잃고 물러났습니다. 산길은 여전히 위험합니다.";
                        },
                    },
                    new StoryChoice
                    {
                        text = "두목을 회유해 사병으로 삼는다", coin = 25, effect = "보병 +6 · 모든 영지 충성 -5",
                        apply = gm => { gm.AddTroops(gm.Capital.provinceId, "infantry", 6); gm.ChangeAllOwnedLoyalty(-5); return "두목이 무릎을 꿇었습니다. 칼솜씨는 쓸 만하나, 백성들은 도적과 한 편이 된 주군을 수군거립니다."; },
                    },
                    new StoryChoice
                    {
                        text = "못 본 척한다", effect = "엽전 -20 (장사 손실)",
                        apply = gm => { gm.Gain(0, -20, 0); return "도적떼는 한 철을 더 설치다 떠났습니다. 그동안 장터 수입이 줄었습니다."; },
                    },
                },
            },
            new StoryEvent
            {
                id = "marriage_offer", title = "혼담", speakerId = Advisor, speakerName = AdvisorName,
                trigger = gm =>
                {
                    if (gm.TurnNumber < 5) return false;
                    var p = gm.HojokProvinces.Where(x => !gm.IsStronghold(x) && gm.IsAdjacentToPlayer(x) && x.loyalty >= 50 && x.loyalty < GameManager.SubmissionLoyalty)
                        .OrderByDescending(x => x.loyalty).FirstOrDefault();
                    if (p == null) return false;
                    gm.EventTarget = p.provinceId;
                    return true;
                },
                body = gm =>
                    $"{TargetHojok(gm)}의 사자가 비단 보따리를 들고 찾아왔습니다.\n\n" +
                    $"\"우리 주인께서 금지옥엽 같은 따님을 {Monarch(gm)} 공의 가문에 보내고자 하십니다.\"\n\n" +
                    "혼인으로 맺은 인연은 칼보다 단단한 법입니다. 받아들이면 저 가문은 곧 우리 사람이 되겠지요. 다만 그만한 체면과 예물이 듭니다.",
                choices =
                {
                    new StoryChoice { text = "혼인을 받아들인다", prestige = 10, effect = "그 호족 충성 +30 (곧 귀부)", apply = gm => { gm.ChangeLoyalty(gm.GetProvince(gm.EventTarget), 30); return "혼례가 성대히 치러졌습니다. 두 가문은 이제 한 집안입니다."; } },
                    new StoryChoice { text = "예물만 보내고 정중히 사양한다", coin = 30, effect = "그 호족 충성 +10", apply = gm => { gm.ChangeLoyalty(gm.GetProvince(gm.EventTarget), 10); return "상대도 체면은 섰으니 서운한 기색은 감췄습니다."; } },
                    new StoryChoice { text = "거절한다", effect = "그 호족 충성 -15", apply = gm => { gm.ChangeLoyalty(gm.GetProvince(gm.EventTarget), -15); return "사자가 얼굴을 붉히며 돌아갔습니다. 저 가문의 문은 당분간 닫혔습니다."; } },
                },
            },
            new StoryEvent
            {
                id = "drought", title = "타들어 가는 들녘", speakerId = null, speakerName = null,
                trigger = gm => gm.Season == 1 && gm.TurnNumber >= 5,
                body = gm =>
                    "여름 내내 비 한 방울 내리지 않습니다.\n\n논바닥이 거북 등처럼 갈라지고, 농부들은 하늘만 올려다봅니다. 이대로면 가을 수확은 기대하기 어렵습니다.",
                choices =
                {
                    new StoryChoice { text = "저수지 공사를 서두른다", grain = 50, effect = "본거지 저수지 Lv +1", apply = gm => { gm.Capital.irrigationLevel++; return $"장정들이 밤낮으로 둑을 쌓았습니다. {Cap(gm)} 저수지가 Lv.{gm.Capital.irrigationLevel}가 되었습니다."; } },
                    new StoryChoice
                    {
                        text = "올해 세금을 감면한다", effect = "모든 영지 세율 10%로 · 충성 +10",
                        apply = gm => { foreach (var p in gm.OwnedProvinces.ToList()) { gm.SetTaxRate(p.provinceId, Mathf.Min(p.taxRate, 0.1f)); gm.ChangeLoyalty(p, 10); } return "\"올해는 세금을 걷지 않는다\"는 방이 붙자 백성들이 엎드려 절했습니다. 엽전 수입은 줄어듭니다."; },
                    },
                    new StoryChoice
                    {
                        text = "하늘에 기우제를 올린다", prestige = 3, effect = "운에 맡긴다",
                        apply = gm =>
                        {
                            if (UnityEngine.Random.value < 0.5f) { gm.ChangeAllOwnedLoyalty(10); gm.Gain(0, 0, 6); return "사흘 뒤 단비가 쏟아졌습니다! 백성들이 주군의 덕이라 칭송합니다. (모든 영지 충성 +10, 권위 +6)"; }
                            int lost = gm.Plunder(0.2f);
                            return $"하늘은 끝내 답하지 않았습니다. 말라 죽은 벼를 갈아엎느라 곡식 {lost}석이 사라졌습니다.";
                        },
                    },
                },
            },
            new StoryEvent
            {
                id = "scholar", title = "떠돌이 선비", speakerId = null, speakerName = null,
                trigger = gm => gm.TurnNumber >= 8,
                body = gm =>
                    "남루한 도포 차림의 선비 하나가 성문을 두드렸습니다.\n\n당나라에 유학했다가 돌아왔으나 의탁할 곳이 없다 합니다. 행색은 초라하나 눈빛이 예사롭지 않습니다.\n\n" +
                    "\"제게 붓 한 자루만 맡겨 주신다면, 이 고을의 살림을 반은 덜어 드리겠습니다.\"",
                choices =
                {
                    new StoryChoice { text = "예를 갖춰 등용한다", coin = 30, effect = "모든 건설 비용 -25% (영구)", apply = gm => { gm.SetFlag("scholar"); return "선비가 장부를 펼치자 새는 곳간이 한눈에 보였습니다. 앞으로 공사비가 크게 줄 것입니다."; } },
                    new StoryChoice { text = "식객으로 머물게 한다", grain = 10, effect = "권위 +2", apply = gm => { gm.Gain(0, 0, 2); return "선비는 한 철을 머물다 고마움을 전하고 떠났습니다."; } },
                    new StoryChoice { text = "돌려보낸다", apply = gm => "선비는 쓸쓸히 웃으며 북쪽으로 길을 잡았습니다. 훗날 어느 나라의 재상이 될지 누가 알겠습니까." },
                },
            },

            // ───────── 중·후반 사건 ─────────
            new StoryEvent
            {
                id = "heir_born", title = "가문의 경사", speakerId = Advisor, speakerName = AdvisorName,
                trigger = gm => gm.TurnNumber >= 10,
                body = gm =>
                    $"주군, 경하드립니다! 안채에서 우렁찬 울음소리가 들렸습니다 — 아드님이십니다.\n\n" +
                    $"난세에 가문을 이을 핏줄이 생긴다는 것은 성 하나를 얻는 것보다 큰 일이지요. {Cap(gm)}의 백성들이 잔치를 기다리고 있습니다.",
                choices =
                {
                    new StoryChoice { text = "온 고을에 잔치를 베푼다", grain = 40, effect = "모든 영지 충성 +8 · 권위 +5", apply = gm => { gm.ChangeAllOwnedLoyalty(8); gm.Gain(0, 0, 5); return "사흘 밤낮 풍악이 울렸습니다. \"우리 주인 댁에 복이 들었다\"며 백성들도 함께 기뻐합니다."; } },
                    new StoryChoice { text = "절에 시주해 아이의 앞날을 빈다", coin = 40, effect = "권위 +8", apply = gm => { gm.Gain(0, 0, 8); return "큰스님이 아이의 이름을 지어 주었습니다. 호족들 사이에 주군 가문의 덕을 칭송하는 말이 돕니다."; } },
                    new StoryChoice { text = "난세에 들뜰 겨를이 없다", effect = "권위 +2", apply = gm => { gm.Gain(0, 0, 2); return "주군은 아이를 한 번 안아 본 뒤 다시 지도 앞에 섰습니다."; } },
                },
            },
            new StoryEvent
            {
                id = "gungye_envoy", title = "북쪽에서 온 밀사", speakerId = "gungye", speakerName = "궁예의 밀사",
                trigger = gm => gm.TurnNumber >= 12,
                body = gm =>
                    "철원에서 후고구려를 세운 궁예의 밀사가 몰래 찾아왔습니다.\n\n" +
                    "\"우리 대왕께서는 남쪽의 견훤을 함께 칠 벗을 찾고 계시오. 손을 잡는다면 군자금을 보내 드리리다.\"\n\n" +
                    "궁예는 스스로를 미륵이라 칭하며 의심 많기로 소문난 자입니다. 그 손을 잡는 것이 득일지 독일지…",
                choices =
                {
                    new StoryChoice
                    {
                        text = "동맹을 맺는다", effect = "엽전 +80 · 이웃 호족 충성 -5 (북쪽 미치광이와 손잡았다는 소문)",
                        apply = gm => { gm.Gain(0, 80, 0); gm.SetFlag("gungye_ally"); gm.ChangeAllHojokLoyalty(-5); return "밀사가 금괴 상자를 내려놓고 사라졌습니다. 호족들 사이엔 \"궁예의 개가 되었다\"는 수군거림이 돕니다."; },
                    },
                    new StoryChoice { text = "정중히 돌려보낸다", effect = "권위 +3", apply = gm => { gm.Gain(0, 0, 3); return "\"우리 일은 우리 손으로 하겠소.\" 밀사는 아쉬운 얼굴로 북쪽으로 돌아갔습니다."; } },
                    new StoryChoice
                    {
                        text = "밀사를 붙잡아 견훤에게 넘긴다", effect = "견훤과의 관계 개선 · 엽전 +40 · 권위 -3",
                        apply = gm =>
                        {
                            gm.Gain(0, 40, -3);
                            if (gm.HasFlag("gyeonhwon_hostile")) { gm.ConsumeFlag("gyeonhwon_hostile"); return "견훤이 크게 기뻐하며 상을 내렸습니다. \"쓸 만한 놈이로군.\" 완산주의 군마가 당분간 움직이지 않을 것입니다."; }
                            return "견훤이 상을 내렸습니다. 다만 의리를 판 자라는 뒷말이 남았습니다.";
                        },
                    },
                },
            },
            new StoryEvent
            {
                id = "hojok_coalition", title = "호족 연합의 반발", speakerId = Advisor, speakerName = AdvisorName,
                trigger = gm => gm.ConquestCount >= 2 && gm.HojokProvinces.Count(p => !gm.IsStronghold(p)) >= 2,
                body = gm =>
                    $"주군께서 칼로 고을을 {gm.ConquestCount}곳이나 빼앗자, 남은 호족들이 밤마다 모여 수군거린다 합니다.\n\n" +
                    "\"다음은 우리 차례다. 힘을 합쳐 막아야 한다.\"\n\n" +
                    "이들이 한데 뭉치면 골치 아파집니다. 어찌하시겠습니까?",
                choices =
                {
                    new StoryChoice { text = "사절을 보내 안심시킨다", coin = 60, effect = "남은 호족 충성 +15", apply = gm => { gm.ChangeAllHojokLoyalty(15); return "\"순순히 따르는 자는 해치지 않는다.\" 사절의 말에 호족들의 모임이 흐지부지 흩어졌습니다."; } },
                    new StoryChoice { text = "국경에서 무력 시위를 한다", minTroops = 40, effect = "권위 +5 · 남은 호족 충성 +5 (두려움)", apply = gm => { gm.Gain(0, 0, 5); gm.ChangeAllHojokLoyalty(5); return "깃발을 앞세운 대군이 국경을 따라 행진하자, 호족들은 감히 맞설 생각을 접었습니다."; } },
                    new StoryChoice { text = "내버려 둔다", effect = "남은 호족 충성 -10 (습격 위험)", apply = gm => { gm.ChangeAllHojokLoyalty(-10); return "호족들의 연합이 굳어졌습니다. 국경 고을의 수비를 단단히 하셔야 할 것입니다."; } },
                },
            },
            new StoryEvent
            {
                id = "wansanju_deserters", title = "완산주의 탈영병", speakerId = null, speakerName = null,
                trigger = gm => gm.TurnNumber >= 15 && gm.Provinces.Any(p => gm.IsStronghold(p) && p.ownerFactionId != gm.PlayerFactionId),
                body = gm =>
                    "밤중에 후백제군 차림의 병사 열댓 명이 성문 앞에 무릎을 꿇었습니다.\n\n" +
                    "\"견훤의 군율이 너무 가혹해 도망쳐 왔습니다. 받아 주신다면 목숨 바쳐 싸우겠습니다. 완산주 성벽의 허술한 곳도 알고 있습니다.\"",
                choices =
                {
                    new StoryChoice
                    {
                        text = "받아들여 우리 군에 편입한다", grain = 30, effect = "창병 +10 · 완산주 수비 감소",
                        apply = gm => { gm.AddTroops(gm.Capital.provinceId, "spear", 10); var s = gm.Provinces.First(p => gm.IsStronghold(p)); int lost = gm.LoseTroopsAt(s.provinceId, 0.08f); return $"탈영병들이 우리 창병이 되었습니다. 이 소식에 완산주에서 {lost}명이 더 달아났다고 합니다."; },
                    },
                    new StoryChoice
                    {
                        text = "성벽의 약점만 캐내고 돌려보낸다", effect = "완산주 수비 15% 감소",
                        apply = gm => { var s = gm.Provinces.First(p => gm.IsStronghold(p)); int lost = gm.LoseTroopsAt(s.provinceId, 0.15f); return $"탈영병들이 그린 성벽 지도를 바탕으로 밀정을 놓아, 완산주 수비병 {lost}명을 흩어 놓았습니다."; },
                    },
                    new StoryChoice { text = "견훤에게 돌려보낸다", effect = "권위 +4", apply = gm => { gm.Gain(0, 0, 4); return "탈영병들은 울부짖으며 끌려갔습니다. 견훤은 주군의 \"의리\"를 기억할 것입니다."; } },
                },
            },
            new StoryEvent
            {
                id = "gyeonhwon_sons", title = "견훤 집안의 불화", speakerId = Advisor, speakerName = AdvisorName,
                trigger = gm => gm.TurnNumber >= 22 && gm.Provinces.Any(p => gm.IsStronghold(p) && p.ownerFactionId != gm.PlayerFactionId),
                body = gm =>
                    "완산주에서 흥미로운 소문이 흘러나옵니다.\n\n" +
                    "견훤이 막내 금강을 총애하자 맏아들 신검이 앙심을 품었고, 성 안의 장수들이 두 편으로 갈라졌다 합니다. 성벽 위의 병사들조차 누구의 명을 따라야 할지 모른다지요.\n\n" +
                    "틈이 벌어진 지금이 기회일 수 있습니다.",
                choices =
                {
                    new StoryChoice
                    {
                        text = "밀정을 보내 이간질한다", coin = 50, effect = "완산주 수비 20% 감소",
                        apply = gm => { var s = gm.Provinces.First(p => gm.IsStronghold(p)); int lost = gm.LoseTroopsAt(s.provinceId, 0.2f); return $"신검파와 금강파가 성 안에서 칼을 맞댔습니다. 완산주 수비병 {lost}명이 쓰러지거나 흩어졌습니다."; },
                    },
                    new StoryChoice
                    {
                        text = "지켜본다", effect = "완산주 수비 8% 감소",
                        apply = gm => { var s = gm.Provinces.First(p => gm.IsStronghold(p)); int lost = gm.LoseTroopsAt(s.provinceId, 0.08f); return $"집안싸움은 저절로 곪아 갔습니다. 완산주 수비병 {lost}명이 성을 떠났다 합니다."; },
                    },
                    new StoryChoice { text = "견훤에게 은밀히 알려 준다", effect = "권위 +6", apply = gm => { gm.Gain(0, 0, 6); if (gm.HasFlag("gyeonhwon_hostile")) gm.ConsumeFlag("gyeonhwon_hostile"); return "견훤이 고마워하며 칼끝을 거두었습니다. 다만 완산주는 다시 단단해질 것입니다."; } },
                },
            },

            // ───────── 계절 사건 (반복) ─────────
            new StoryEvent
            {
                id = "harvest", title = "가을의 풍년", speakerId = Advisor, speakerName = AdvisorName, repeatable = true, cooldownTurns = 8,
                trigger = gm => gm.Season == 2 && gm.TurnNumber >= 3,
                body = gm =>
                    "황금빛 벼이삭이 고개를 숙였습니다. 올가을은 풍년입니다!\n\n곳간 앞에 볏섬이 산처럼 쌓여 갑니다. 남는 곡식을 어디에 쓰시겠습니까?",
                choices =
                {
                    new StoryChoice { text = "곳간에 비축한다", effect = "석 +60", apply = gm => { gm.Gain(60, 0, 0); return "겨울과 다음 원정을 위한 곡식이 넉넉히 쌓였습니다."; } },
                    new StoryChoice { text = "백성과 잔치를 연다", effect = "석 +20 · 모든 영지 충성 +8", apply = gm => { gm.Gain(20, 0, 0); gm.ChangeAllOwnedLoyalty(8); return "마을마다 풍물 소리가 울렸습니다. 백성들의 얼굴에 웃음이 돌아왔습니다."; } },
                    new StoryChoice { text = "군량으로 돌려 장정을 모은다", effect = "석 +20 · 보병 +6", apply = gm => { gm.Gain(20, 0, 0); gm.AddTroops(gm.Capital.provinceId, "infantry", 6); return "배불리 먹인다는 말에 장정들이 몰려들었습니다."; } },
                },
            },
            new StoryEvent
            {
                id = "winter", title = "혹한", speakerId = null, speakerName = null, repeatable = true, cooldownTurns = 8,
                trigger = gm => gm.Season == 3 && gm.TurnNumber >= 4,
                body = gm => "매서운 북풍이 들녘을 휩씁니다.\n\n땔감과 곡식이 모자란 집마다 아이 우는 소리가 끊이지 않습니다. 겨울엔 수확이 반으로 줄어듭니다.",
                choices =
                {
                    new StoryChoice { text = "곳간을 열어 나눈다", grain = 40, effect = "모든 영지 충성 +6", apply = gm => { gm.ChangeAllOwnedLoyalty(6); return "관아 앞에 죽 끓이는 솥이 걸렸습니다. 백성들이 이 겨울을 기억할 것입니다."; } },
                    new StoryChoice { text = "군사들부터 챙긴다", grain = 20, effect = "모든 영지 충성 -4", apply = gm => { gm.ChangeAllOwnedLoyalty(-4); return "병사들은 배불렀으나, 굶주린 백성들의 눈길이 곱지 않습니다."; } },
                    new StoryChoice { text = "각자 버티게 한다", effect = "모든 영지 충성 -8 · 병력 5% 이탈", apply = gm => { gm.ChangeAllOwnedLoyalty(-8); int lost = gm.LoseTroopsPercent(0.05f); return $"곳간은 지켰으나 얼어 죽은 이가 적지 않았습니다. 병사 {lost}명이 고향으로 달아났습니다."; } },
                },
            },
        };

        static int RaidSize(GameManager gm) => 10 + gm.TurnNumber / 4;

        static List<StoryChoice> EnvoyChoices(bool allowDelay)
        {
            var list = new List<StoryChoice>
            {
                new StoryChoice
                {
                    text = "신하의 예를 갖춘다", coin = 50, prestige = 5, effect = "후백제와 화친 · 이웃 호족 충성 -5",
                    apply = gm => { gm.SetFlag("gyeonhwon_vassal"); gm.ChangeAllHojokLoyalty(-5); return "견훤의 그늘 아래 들어갔습니다. 칼날은 피했으나, 호족들은 주군을 견훤의 졸개로 여기기 시작합니다."; },
                },
                new StoryChoice
                {
                    text = "단호히 거절한다", effect = "권위 +10 · 이웃 호족 충성 +5 · 후백제군 침공을 각오해야 함",
                    apply = gm => { gm.SetFlag("gyeonhwon_hostile"); gm.Gain(0, 0, 10); gm.ChangeAllHojokLoyalty(5); return "\"돌아가 전하라. 이 땅의 주인은 여기 있다고.\" 사신이 얼굴이 하얗게 질려 돌아갔습니다. 호족들이 주군의 기개에 감탄합니다 — 그리고 완산주에서 군마가 움직이기 시작합니다."; },
                },
            };
            if (allowDelay)
                list.Add(new StoryChoice
                {
                    text = "극진히 대접해 시간을 번다", grain = 30, coin = 20, effect = "결정을 미룸 (몇 계절 뒤 다시 옴)",
                    apply = gm => { gm.SetFlag("envoy_delayed"); return "사흘 밤낮 술과 고기를 대접하자 사신은 흡족해서 돌아갔습니다. 다만 답을 미룬 것일 뿐입니다."; },
                });
            return list;
        }
    }
}
