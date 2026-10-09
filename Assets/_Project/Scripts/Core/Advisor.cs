using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Husamguk.Core
{
    /// <summary>책사가 건네는 조언 한 줄 + 바로가기 버튼 (GDD §3.21 책사 힌트 시스템).</summary>
    public class Advice
    {
        public string line;
        public string button;
        public string group;
        public string subTab;
        public string provinceId;
        public bool urgent;
    }

    /// <summary>
    /// 지금 상태를 진단해 "다음에 둘 수"를 최대 3개까지 고른다. 위기 → 현재 목표 → 계절 리듬 순.
    /// 강제 튜토리얼이 아니라 책사(최응)의 말투로 제안만 한다.
    /// </summary>
    public static class Advisor
    {
        public static List<Advice> Get(GameManager gm)
        {
            var list = new List<Advice>();
            var f = gm.PlayerFaction;
            if (f == null || gm.Capital == null) return list;

            // ── 위기 ──
            int upkeep = gm.GrainUpkeep();
            int grainNet = gm.ForecastGrain() - upkeep;
            if (grainNet < 0 && f.grain + grainNet * 2 < 0)
                list.Add(new Advice
                {
                    urgent = true,
                    line = $"주군, 군량이 위태롭습니다. 곳간에 석 {f.grain}, 이번 계절 수확 {gm.ForecastGrain()}에 병사들이 {upkeep}을 먹어 치웁니다. 두 계절 안에 바닥나면 병사들이 달아납니다. 논을 개간하시거나, 장터에서 엽전으로 곡식을 사들이시거나, 징집을 멈추십시오.",
                    button = "경제·농업", group = "영지", subTab = "경제·농업",
                });

            var unrest = gm.OwnedProvinces.Where(p => p.loyalty < 35).OrderBy(p => p.loyalty).FirstOrDefault();
            if (unrest != null)
                list.Add(new Advice
                {
                    urgent = true,
                    line = $"{gm.ProvinceName(unrest.provinceId)}의 민심이 흉흉합니다(충성 {unrest.loyalty}). 세율을 낮추시면 계절마다 조금씩 회복됩니다. 25 아래로 떨어지면 민란이 납니다.",
                    button = "세율 조정", group = "영지", subTab = "경제·농업", provinceId = unrest.provinceId,
                });

            var raider = gm.HojokProvinces.Where(p => !gm.IsStronghold(p) && p.loyalty < 30 && gm.IsAdjacentToPlayer(p)).FirstOrDefault();
            if (raider != null)
                list.Add(new Advice
                {
                    urgent = true,
                    line = $"{gm.HojokName(raider)}이(가) 우리에게 적의를 품었습니다(충성 {raider.loyalty}). 언제 습격해 올지 모릅니다. 국경 수비를 늘리시거나, 아예 먼저 치시지요.",
                    button = "출진", group = "군사", subTab = "출진", provinceId = raider.provinceId,
                });

            // ── 현재 목표 ──
            var obj = gm.CurrentObjective;
            if (obj != null)
            {
                var a = ObjectiveAdvice(gm, obj);
                if (a != null) list.Add(a);
            }

            // ── 놀고 있는 일손 ──
            if (!gm.OwnedProvinces.Any(p => !string.IsNullOrEmpty(p.constructionId)))
            {
                var cap = gm.Capital;
                int cost = gm.ConstructionCost(cap, "agriculture");
                if (f.grain >= cost + upkeep)
                    list.Add(new Advice
                    {
                        line = $"지금 진행 중인 공사가 없습니다. 곳간에 석이 {f.grain} 있으니 {gm.ProvinceName(cap.provinceId)}에 논을 개간하시지요(석 {cost}). 두 계절 뒤부터 수확이 늘어납니다.",
                        button = "논 개간하러", group = "영지", subTab = "경제·농업", provinceId = cap.provinceId,
                    });
            }

            // ── 남아도는 엽전 ──
            if (f.coin >= 200 && !gm.HojokProvinces.Any(p => !gm.IsStronghold(p) && gm.IsAdjacentToPlayer(p)))
                list.Add(new Advice
                {
                    line = $"곳간에 엽전이 {f.coin}이나 쌓여 놀고 있습니다. 장터에서 곡식으로 바꿔 군량을 넉넉히 하시고, 궁병·기병처럼 엽전이 드는 정병을 모으시지요.",
                    button = "장터로", group = "영지", subTab = "경제·농업",
                });

            // ── 계절 리듬 (§1.4 Micro) ──
            if (gm.Season == 1)
                list.Add(new Advice { line = "가을 수확이 코앞입니다. 가을엔 수확이 1.6배 — 원정 군량을 마련할 때입니다. 공사도 지금 걸어 두면 가을에 끝납니다." });
            else if (gm.Season == 2)
                list.Add(new Advice { line = "겨울엔 수확이 반으로 줄어듭니다. 병력을 크게 늘리기 전에 곳간을 먼저 채워 두십시오." });

            if (list.Count == 0)
                list.Add(new Advice { line = "지금은 힘을 기를 때입니다. 다음 계절로 넘기시지요." });

            return list.Take(3).ToList();
        }

        static Advice ObjectiveAdvice(GameManager gm, Objective obj)
        {
            switch (obj.id)
            {
                case "build":
                    return new Advice { line = "먼저 곳간부터 채우셔야 합니다. 경제·농업에서 논을 개간하십시오 — 두 계절이면 끝나고, 그 뒤로는 계절마다 수확이 늘어납니다.", button = "논 개간하러", group = "영지", subTab = "경제·농업" };
                case "troops":
                    return new Advice { line = $"우리 사병은 {gm.TotalTroops()}명뿐입니다. 이웃이 얕잡아 보지 않도록 병영에서 장정을 모으십시오. 고을 크기만큼만 한 계절에 모을 수 있습니다.", button = "병영으로", group = "군사", subTab = "병영·징집" };
                case "first_land":
                case "expand":
                {
                    var leaning = gm.HojokProvinces.Where(p => !gm.IsStronghold(p) && gm.IsAdjacentToPlayer(p)).OrderByDescending(p => p.loyalty).FirstOrDefault();
                    var weakest = gm.HojokProvinces.Where(p => !gm.IsStronghold(p) && gm.IsAdjacentToPlayer(p)).OrderBy(p => gm.GarrisonTotal(p)).FirstOrDefault();
                    if (leaning != null && leaning.loyalty >= 55)
                        return new Advice
                        {
                            line = $"{gm.HojokName(leaning)}의 마음이 기울고 있습니다(충성 {leaning.loyalty}/{GameManager.SubmissionLoyalty}). 회유나 사성을 거듭하시면 계절이 바뀔 때 스스로 성문을 열 것입니다. 그들의 사병도 함께 따라오지요.",
                            button = "회유하러", group = "호족·신하", subTab = "호족 판도", provinceId = leaning.provinceId,
                        };
                    if (weakest != null && gm.TotalTroops() > gm.GarrisonTotal(weakest) * 1.5f)
                        return new Advice
                        {
                            line = $"{gm.HojokName(weakest)}의 사병은 {gm.GarrisonTotal(weakest)}명. 지금 우리 병력({gm.TotalTroops()})이면 칠 수 있습니다. 다만 칼로 얻으면 다른 호족들이 겁을 먹고 등을 돌립니다(이웃 충성 -10).",
                            button = "토벌하러", group = "호족·신하", subTab = "호족 판도", provinceId = weakest.provinceId,
                        };
                    if (leaning != null)
                        return new Advice
                        {
                            line = $"가장 가까운 호족은 {gm.HojokName(leaning)}입니다(충성 {leaning.loyalty}). 엽전으로 회유하거나 권위로 성씨를 내려 마음을 사십시오. 충성 {GameManager.SubmissionLoyalty}이면 귀부합니다.",
                            button = "호족 판도", group = "호족·신하", subTab = "호족 판도", provinceId = leaning.provinceId,
                        };
                    return null;
                }
                case "promote":
                {
                    var p = gm.OwnedProvinces.OrderByDescending(x => x.cumulativeEconomy).First();
                    var tier = gm.Tier(p);
                    if (tier == null) return null;
                    return new Advice
                    {
                        line = $"{gm.ProvinceName(p.provinceId)}의 누적 세수가 {p.cumulativeEconomy}/{tier.economyThreshold}입니다. 세수가 차고 병사가 {tier.militaryThreshold}명 이상 주둔하면 승격합니다. 세율을 조금 올리면 빨라지지만 민심이 떨어집니다.",
                        button = "세율 조정", group = "영지", subTab = "경제·농업", provinceId = p.provinceId,
                    };
                }
                case "wansanju":
                {
                    var s = gm.Provinces.FirstOrDefault(x => gm.IsStronghold(x));
                    if (s == null) return null;
                    return new Advice
                    {
                        line = $"완산주에는 정예 {gm.GarrisonTotal(s)}명이 버티고 있습니다. 창병은 기병을, 기병은 궁병을, 궁병은 창병을 이깁니다. 적의 구성을 보고 병종을 맞춰 대군을 모으십시오.",
                        button = "출진 준비", group = "군사", subTab = "출진", provinceId = s.provinceId,
                    };
                }
                default:
                    return null;
            }
        }
    }
}
