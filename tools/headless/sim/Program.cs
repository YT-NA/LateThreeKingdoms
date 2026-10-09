// 헤드리스 시뮬레이션: GameSetup.cs와 같은 샘플 데이터로 게임을 자동 플레이해 막힘·예외·밸런스를 본다.
// 주의: BuildDb()는 Editor/GameSetup.cs의 값을 손으로 옮긴 것 — GameSetup의 샘플 데이터를 바꾸면 여기도 같이 바꿀 것.
// 실행: dotnet run --project tools/headless/sim/Sim.csproj   (자세히: -v)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Husamguk.Core;
using Husamguk.Data;
using UnityEngine;

static class Sim
{
    static T Make<T>(Action<T> cfg) where T : ScriptableObject { var t = ScriptableObject.CreateInstance<T>(); cfg(t); return t; }

    // GameSetup.cs의 샘플 데이터와 같은 값
    static GameDatabase BuildDb()
    {
        var grain = Make<ResourceDef>(r => { r.id = "grain"; r.displayName = "석"; });
        var coin = Make<ResourceDef>(r => { r.id = "coin"; r.displayName = "엽전"; });
        var prestige = Make<ResourceDef>(r => { r.id = "prestige"; r.displayName = "권위"; });
        SettlementTierDef Tier(string id, string n, string office, int pop, int mil, int econ) => Make<SettlementTierDef>(t => { t.id = id; t.displayName = n; t.officeTitle = office; t.populationThreshold = pop; t.militaryThreshold = mil; t.economyThreshold = econ; });
        var tv = Tier("village", "촌", "촌주", 10, 2, 30);
        var tt = Tier("township", "현", "현령", 80, 3, 200);
        var td = Tier("district", "군", "태수", 200, 8, 600);
        var tp = Tier("province", "주", "도독", 500, 20, 1500);
        var tc = Tier("capital", "경", "직할", 1000, 40, 4000);
        UnitDef Unit(string id, string n, UnitClass c, int power, ResourceAmount[] cost, int food) => Make<UnitDef>(u => { u.id = id; u.displayName = n; u.unitClass = c; u.power = power; u.recruitCost = cost; u.foodUpkeepPerSoldier = food; });
        var inf = Unit("infantry", "보병", UnitClass.None, 10, new[] { new ResourceAmount(grain, 4) }, 3);
        var spear = Unit("spear", "창병", UnitClass.Spear, 12, new[] { new ResourceAmount(grain, 5), new ResourceAmount(coin, 2) }, 4);
        var archer = Unit("archer", "궁병", UnitClass.Archer, 11, new[] { new ResourceAmount(grain, 5), new ResourceAmount(coin, 3) }, 4);
        var cav = Unit("cavalry", "기병", UnitClass.Cavalry, 16, new[] { new ResourceAmount(grain, 8), new ResourceAmount(coin, 5) }, 6);
        var fa = Make<FactionDef>(f => { f.id = "isung_clan"; f.displayName = "이서 김씨 가문"; });
        var fb = Make<FactionDef>(f => { f.id = "gobu_clan"; f.displayName = "고부 김씨 가문"; });
        RetainerDef Ret(string id, string n, int l, int m, int i, int s, int p, int c, int v) => Make<RetainerDef>(r => { r.id = id; r.displayName = n; r.leadership = l; r.might = m; r.intellect = i; r.scholarship = s; r.politics = p; r.charm = c; r.virtue = v; });
        var ieok = Ret("ieok", "김억", 50, 55, 50, 40, 45, 65, 60);
        var gimchong = Ret("gimchong", "김총", 68, 72, 45, 30, 40, 38, 40);
        var choi = Ret("choieung", "최응", 30, 20, 85, 90, 75, 60, 70);
        var gh = Ret("gyeonhwon", "견훤", 92, 88, 70, 40, 65, 72, 45); var gy = Ret("gungye", "궁예", 85, 80, 75, 60, 55, 70, 30);
        ProvinceDef Prov(string id, string n, float x, float y, SettlementTierDef t, string hojok, string[] adj, float bonus, bool sh = false) =>
            Make<ProvinceDef>(p => { p.id = id; p.displayName = n; p.mapPosition = new Vector2(x, y); p.startingTier = t; p.startingHojokName = hojok; p.adjacentProvinceIds = adj.ToList(); p.grainBonus = bonus; p.hostileStronghold = sh; });
        var provs = new List<ProvinceDef>
        {
            Prov("wansanju", "완산주", 0, 0, tp, "후백제 (견훤 직할)", new[] { "gimje", "imsil", "jinan", "geumgu" }, 0.1f, true),
            Prov("iseo", "이서", -3, 1, tv, "이서 호족", new[] { "gobu", "geumgu" }, 0),
            Prov("gobu", "고부", -4, -1, tt, "고부 호족", new[] { "iseo", "gimje", "taein" }, 0),
            Prov("gimje", "김제", -1, -2, tt, "김제 호족", new[] { "gobu", "wansanju" }, 0.05f),
            Prov("taein", "태인", -3, -3, tv, "태인 호족", new[] { "gobu", "imsil" }, 0),
            Prov("imsil", "임실", 0, -3, tv, "임실 호족", new[] { "taein", "jinan", "wansanju" }, 0),
            Prov("jinan", "진안", 2, -2, tv, "진안 호족", new[] { "imsil", "wansanju" }, 0),
            Prov("geumgu", "금구", 0, 2, tv, "금구 호족", new[] { "iseo", "wansanju" }, 0),
        };
        var sc = Make<ScenarioDef>(s =>
        {
            s.id = "scenario_900"; s.year = 900;
            s.playableCharacters = new List<ScenarioDef.PlayableCharacter>
            {
                new() { retainer = ieok, startingFaction = fa, startingProvince = provs[1] },
                new() { retainer = gimchong, startingFaction = fb, startingProvince = provs[2] },
            };
        });
        return Make<GameDatabase>(d =>
        {
            d.scenarios = new List<ScenarioDef> { sc };
            d.factions = new List<FactionDef> { fa, fb };
            d.settlementTiers = new List<SettlementTierDef> { tv, tt, td, tp, tc };
            d.provinces = provs;
            d.units = new List<UnitDef> { inf, spear, archer, cav };
            d.retainers = new List<RetainerDef> { ieok, gimchong, choi, gh, gy };
            d.grain = grain; d.coin = coin; d.prestige = prestige; d.appeaseCoinCost = 40; d.appeaseLoyaltyGain = 10; d.grantSurnamePrestigeCost = 25; d.grantSurnameLoyaltyGain = 12;
        });
    }

    static GameManager MakeGm(GameDatabase db)
    {
        var gm = (GameManager)Activator.CreateInstance(typeof(GameManager));
        typeof(GameManager).GetField("database", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(gm, db);
        typeof(GameManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(gm, null);
        return gm;
    }

    static void Act(GameManager gm, string style, List<string> trace)
    {
        var f = gm.PlayerFaction;
        int upkeep = gm.GrainUpkeep();
        // 공사
        foreach (var p in gm.OwnedProvinces.ToList())
        {
            if (!string.IsNullOrEmpty(p.constructionId)) continue;
            string what = p.agricultureLevel <= p.irrigationLevel ? "agriculture" : "irrigation";
            if (f.grain >= gm.ConstructionCost(p, what) + Math.Max(40, upkeep * 2))
            {
                if (what == "agriculture") gm.TryUpgradeAgriculture(p.provinceId); else gm.TryUpgradeIrrigation(p.provinceId);
            }
        }
        // 장터: 남는 엽전을 곡식으로
        while (f.coin > gm.AppeaseCost() + 60 && f.grain < 300) gm.TryTrade(true);
        // 징집: 곡식 순증이 남으면
        bool late = gm.OwnedCount >= 4 || gm.TurnNumber > 16;
        foreach (var p in gm.OwnedProvinces.OrderByDescending(x => x.provinceId == gm.CapitalId).ToList())
        {
            for (int k = 0; k < 3; k++)
            {
                int net = gm.ForecastGrain() - gm.GrainUpkeep();
                if ((net < 6 && f.grain < 250) || f.grain < 70) break;
                string unit = late ? (k % 3 == 2 ? "infantry" : "archer") : "infantry";
                if (!gm.TryRecruit(p.provinceId, unit, 5)) break;
            }
        }
        // 회유·사성
        if (style != "aggressive" || gm.TurnNumber < 6)
        {
            var t = gm.HojokProvinces.Where(x => !gm.IsStronghold(x) && gm.IsAdjacentToPlayer(x)).OrderByDescending(x => x.loyalty).FirstOrDefault();
            if (t != null)
            {
                int g = 0;
                while (t.loyalty < GameManager.SubmissionLoyalty && f.prestige >= gm.SurnameCost() && g++ < 5) gm.TryGrantSurname(t.provinceId);
                g = 0;
                while (t.loyalty < GameManager.SubmissionLoyalty && f.coin >= gm.AppeaseCost() + 15 && g++ < 5) gm.TryAppeaseHojok(t.provinceId);
            }
        }
        // 토벌
        var army = gm.OwnedProvinces.SelectMany(x => x.garrison).GroupBy(u => u.unitId).Select(g => new UnitCount { unitId = g.Key, count = g.Sum(u => u.count) }).ToList();
        foreach (var t in gm.Provinces.Where(x => x.ownerFactionId != gm.PlayerFactionId && gm.IsAdjacentToPlayer(x)).OrderBy(x => gm.GarrisonTotal(x)).ToList())
        {
            bool stronghold = gm.IsStronghold(t);
            if (!stronghold && style != "aggressive" && gm.TurnNumber < 14) continue;
            if (stronghold && gm.HojokProvinces.Count(x => !gm.IsStronghold(x)) > 0 && style != "aggressive") continue;
            gm.ComputePowers(gm.PlayerFactionId, army, t.provinceId, t.garrison, out float atk, out float def);
            float ratio = def <= 0 ? 99 : atk / def;
            if (ratio >= (stronghold ? 1.25f : 1.4f))
            {
                gm.TryPurge(t.provinceId, out var rep);
                trace.Add($"T{gm.TurnNumber} 토벌 {t.provinceId} ratio {ratio:0.00} → {(rep != null && rep.victory ? "승" : "패")} (손실 {rep?.attackerLost})");
                break;
            }
        }
    }

    static void Play(GameDatabase db, int seed, int character, string style, bool verbose)
    {
        UnityEngine.Random.Rng = new System.Random(seed);
        var gm = MakeGm(db);
        var sc = db.scenarios[0];
        gm.NewGame(sc, sc.playableCharacters[character]);
        var objTurns = new List<string>();
        var events = new List<string>();
        var trace = new List<string>();
        var seenObj = new HashSet<string>();
        void TrackObj() { foreach (var o in ObjectiveBook.All) if (gm.IsObjectiveDone(o) && seenObj.Add(o.id)) objTurns.Add($"{o.id}@T{gm.TurnNumber}"); }
        string outcome = "시간 초과";
        int minGrain = int.MaxValue;
        for (int guard = 0; guard < 80; guard++)
        {
            int g2 = 0;
            while (gm.PendingEvent != null && g2++ < 10)
            {
                var ev = gm.PendingEvent;
                _ = ev.body(gm);
                var affordable = Enumerable.Range(0, ev.choices.Count).Where(i => gm.CanAfford(ev.choices[i])).ToList();
                if (affordable.Count == 0) { Console.WriteLine($"  !! 막힘: {ev.id} — 고를 수 있는 선택지가 없음"); return; }
                int pick = affordable[UnityEngine.Random.Rng.Next(affordable.Count)];
                if (ev.id == "ending_victory") pick = 0;
                TrackObj();
                events.Add($"T{gm.TurnNumber}:{ev.id}[{pick}]");
                var res = gm.ResolveEvent(pick);
                if (res == null) { Console.WriteLine($"  !! ResolveEvent null: {ev.id}"); return; }
            }
            if (gm.ConsumeFlag(GameManager.RestartFlag)) { outcome = "재시작 선택"; break; }
            if (gm.IsVictory) { outcome = $"승리 (T{gm.TurnNumber}, {gm.Year}년)"; break; }
            if (gm.IsDefeated) { outcome = $"패배 (T{gm.TurnNumber})"; break; }

            Advisor.Get(gm);
            Act(gm, style, trace);
            if (gm.IsVictory) { outcome = $"승리 (T{gm.TurnNumber}, {gm.Year}년)"; break; }
            TrackObj();
            if (gm.PendingEvent != null) continue;
            gm.AdvanceSeason();
            minGrain = Math.Min(minGrain, gm.PlayerFaction.grain);
            TrackObj();
            if (guard == 10)
            {
                // 저장/불러오기 왕복 확인
                gm.SaveGame();
                var gm2 = MakeGm(db);
                if (!gm2.LoadGame()) Console.WriteLine("  !! 불러오기 실패");
                else if (gm2.TurnNumber != gm.TurnNumber || gm2.OwnedCount != gm.OwnedCount || gm2.Log.Count != gm.Log.Count || gm2.ObjectiveIndex != gm.ObjectiveIndex)
                    Console.WriteLine($"  !! 저장 왕복 불일치 turn {gm2.TurnNumber}/{gm.TurnNumber} owned {gm2.OwnedCount}/{gm.OwnedCount} log {gm2.Log.Count}/{gm.Log.Count}");
            }
        }
        var f = gm.PlayerFaction;
        Console.WriteLine($"[seed {seed}, {(character == 0 ? "김억" : "김총")}, {style}] {outcome} | 영지 {gm.OwnedCount}/8, 병력 {gm.TotalTroops()}, 석 {f.grain}(최저 {minGrain}) 엽전 {f.coin} 권위 {f.prestige}");
        Console.WriteLine($"  목표: {string.Join(", ", objTurns)}");
        Console.WriteLine($"  사건 {events.Count}개: {string.Join(" ", events)}");
        if (verbose)
        {
            foreach (var t in trace) Console.WriteLine("  " + t);
            foreach (var e in gm.Log.Where(e => e.kind == LogKind.Bad || e.kind == LogKind.Goal)) Console.WriteLine($"   - T{e.turn} {e.text}");
        }
    }

    static void Main(string[] args)
    {
        System.IO.Directory.CreateDirectory(Application.dataPath);
        var db = BuildDb();
        bool verbose = args.Contains("-v");
        foreach (var style in new[] { "peaceful", "aggressive" })
            for (int ch = 0; ch < 2; ch++)
                for (int seed = 1; seed <= 3; seed++)
                    Play(db, seed, ch, style, verbose && seed == 1);
    }
}
