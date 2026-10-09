using System;
using System.Collections.Generic;
using System.Linq;
using Husamguk.Data;
using UnityEngine;

namespace Husamguk.Core
{
    /// <summary>
    /// 왕국 전체 상태와 계절 턴 진행을 맡는다. GDD §4(핵심 루프), §5(시스템 상세).
    /// MVP 1차 구현: 경제·호족 충성도·거점 승격·단순 전투·AI·저장만 완전히 동작한다.
    /// 주요 관직/왕위 계승/혼사·자식 교육/민심 관리는 데이터 구조까지만 있고 행동 메서드는 다음 작업.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] GameDatabase database;
        public GameDatabase Database => database;

        public string PlayerFactionId { get; private set; }
        public int Year { get; private set; }
        public int Season { get; private set; } // 0=봄 1=여름 2=가을 3=겨울
        public static readonly string[] SeasonNames = { "봄", "여름", "가을", "겨울" };

        readonly Dictionary<string, ProvinceState> _provinces = new();
        readonly Dictionary<string, FactionState> _factions = new();
        readonly Dictionary<string, RetainerState> _retainers = new();

        public IReadOnlyCollection<ProvinceState> Provinces => _provinces.Values;
        /// <summary>§3.17 엔딩 화면 MVP 승리 조건: database에 등록된 지역을 전부 복속하면 통일.</summary>
        public bool IsVictory => _provinces.Count > 0 && _provinces.Values.All(p => p.ownerFactionId == PlayerFactionId);
        public IReadOnlyCollection<FactionState> Factions => _factions.Values;
        public IReadOnlyCollection<RetainerState> Retainers => _retainers.Values;

        public event Action SeasonAdvanced;
        public event Action<ProvinceState, SettlementTierDef> ProvincePromoted;
        public event Action<string> MonarchDied; // factionId
        public event Action<BattleReport> BattleResolved;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ───────── 조회 ─────────

        public ProvinceState GetProvince(string id) => id != null && _provinces.TryGetValue(id, out var p) ? p : null;
        public FactionState GetFaction(string id) => id != null && _factions.TryGetValue(id, out var f) ? f : null;
        public RetainerState GetRetainer(string id) => id != null && _retainers.TryGetValue(id, out var r) ? r : null;
        public FactionState PlayerFaction => GetFaction(PlayerFactionId);

        public ProvinceDef Def(ProvinceState s) => database.GetProvince(s.provinceId);
        public FactionDef Def(FactionState s) => database.GetFaction(s.factionId);
        public RetainerDef Def(RetainerState s) => database.GetRetainer(s.retainerId);
        public SettlementTierDef Tier(ProvinceState s) => database.settlementTiers.FirstOrDefault(t => t.id == s.tierId);

        public IEnumerable<ProvinceState> ProvincesOf(string factionId) => _provinces.Values.Where(p => p.ownerFactionId == factionId);
        public IEnumerable<ProvinceState> AdjacentTo(ProvinceState p) =>
            Def(p).adjacentProvinceIds.Select(GetProvince).Where(x => x != null);

        // ───────── 새 게임 (§3.18 시나리오 선택) ─────────

        public void NewGame(ScenarioDef scenario, ScenarioDef.PlayableCharacter pick)
        {
            _provinces.Clear();
            _factions.Clear();
            _retainers.Clear();

            Year = scenario.year;
            Season = 0;
            PlayerFactionId = pick.startingFaction.id;

            foreach (var provDef in database.provinces)
            {
                var ps = new ProvinceState
                {
                    provinceId = provDef.id,
                    ownerFactionId = provDef.startingFaction != null ? provDef.startingFaction.id : null,
                    tierId = provDef.startingTier != null ? provDef.startingTier.id : database.settlementTiers[0].id,
                    grievance = provDef.startingHojokName,
                };
                _provinces[ps.provinceId] = ps;
            }
            // 플레이어는 고른 인물의 시작 지역을 직접 영유한다
            if (pick.startingProvince != null)
            {
                var ps = GetProvince(pick.startingProvince.id);
                ps.ownerFactionId = PlayerFactionId;
                ps.loyalty = 100;
            }

            foreach (var facDef in database.factions)
                _factions[facDef.id] = new FactionState { factionId = facDef.id, grain = 200, coin = 100, prestige = 20 };

            // 플레이어 캐릭터를 신하 겸 군주로 등록
            var retainerState = new RetainerState
            {
                retainerId = pick.retainer.id,
                factionId = PlayerFactionId,
                status = RetainerStatus.Office,
                age = 25,
            };
            _retainers[retainerState.retainerId] = retainerState;
            PlayerFaction.monarchRetainerId = retainerState.retainerId;
            PlayerFaction.retainerIds.Add(retainerState.retainerId);
        }

        // ───────── 계절 턴 (§4) ─────────

        public void AdvanceSeason()
        {
            TickEconomy();
            TickConstruction();
            TickLoyaltyDrift();
            TickSettlementTier();
            TickAging();
            RunAiTurn();

            Season++;
            if (Season >= database.seasonsPerYear) { Season = 0; Year++; }
            SeasonAdvanced?.Invoke();
        }

        void TickEconomy()
        {
            foreach (var p in _provinces.Values)
            {
                if (string.IsNullOrEmpty(p.ownerFactionId)) continue;
                var faction = GetFaction(p.ownerFactionId);
                if (faction == null) continue;
                var provDef = Def(p);

                int grainProduced = Mathf.RoundToInt((10 + p.agricultureLevel * 8 + p.irrigationLevel * 5) * (1f + (provDef?.grainBonus ?? 0f)));
                int coinProduced = Mathf.RoundToInt(grainProduced * p.taxRate * 1.5f);

                faction.grain += grainProduced;
                faction.coin += coinProduced;
                p.cumulativeEconomy += coinProduced;
            }
            var player = PlayerFaction;
            if (player != null) player.prestige += 1; // §5.1 권위: 턴마다 소량 자동
        }

        void TickConstruction()
        {
            foreach (var p in _provinces.Values)
            {
                if (string.IsNullOrEmpty(p.constructionId)) continue;
                p.constructionSeasonsLeft--;
                if (p.constructionSeasonsLeft > 0) continue;
                if (p.constructionId == "agriculture") p.agricultureLevel++;
                else if (p.constructionId == "irrigation") p.irrigationLevel++;
                p.constructionId = null;
            }
        }

        /// <summary>§5.2 호족 충성도: 세율이 높을수록, 방치할수록 하락.</summary>
        void TickLoyaltyDrift()
        {
            foreach (var p in _provinces.Values)
            {
                if (string.IsNullOrEmpty(p.ownerFactionId)) continue;
                int drift = Mathf.RoundToInt(-2f - p.taxRate * 10f);
                p.loyalty = Mathf.Clamp(p.loyalty + drift, 0, 100);
            }
        }

        /// <summary>§5.6 거점 등급 승격 판정 — 임계치를 넘으면 자동 승격하고 이벤트를 쏜다(§3.13 팝업은 UI가 구독해서 띄움).</summary>
        void TickSettlementTier()
        {
            foreach (var p in _provinces.Values)
            {
                var tier = Tier(p);
                if (tier == null) continue;
                var next = database.NextTier(tier);
                if (next == null) continue;
                if (p.cumulativeEconomy < tier.economyThreshold) continue;
                if (GarrisonTotal(p) < tier.militaryThreshold && p.cumulativeMilitary < tier.militaryThreshold) continue;

                p.tierId = next.id;
                ProvincePromoted?.Invoke(p, next);
            }
        }

        /// <summary>§5.9 군주 수명과 자연 승계.</summary>
        void TickAging()
        {
            foreach (var r in _retainers.Values.ToList())
            {
                r.age++;
                if (r.age < database.monarchOldAge) continue;
                if (UnityEngine.Random.value > database.deathChancePerSeasonAfterOldAge) continue;

                var faction = GetFaction(r.factionId);
                if (faction != null && faction.monarchRetainerId == r.retainerId)
                    ResolveSuccession(faction);
            }
        }

        /// <summary>§5.4 왕위 계승 1~2단계: 명분 최고 후계자가 금고·자리를 물려받는다. 3~4단계(파벌 처분)는 2차 범위.</summary>
        void ResolveSuccession(FactionState faction)
        {
            var heirs = faction.retainerIds
                .Select(GetRetainer)
                .Where(r => r != null && Def(r) != null && Def(r).isHeir)
                .OrderByDescending(r => r.legitimacyScore)
                .ToList();

            if (heirs.Count == 0)
            {
                Debug.Log($"[GameManager] {faction.factionId} 세력에 후계자가 없어 군주 자리가 비었습니다 (2차 범위: 세력 와해).");
                faction.monarchRetainerId = null;
                return;
            }

            var next = heirs[0];
            faction.monarchRetainerId = next.retainerId;
            next.status = RetainerStatus.Office;
            MonarchDied?.Invoke(faction.factionId);
        }

        // ───────── 호족 판도 액션 (§3.2) ─────────

        public bool TryAppeaseHojok(string provinceId)
        {
            var p = GetProvince(provinceId);
            var player = PlayerFaction;
            if (p == null || player == null || player.coin < database.appeaseCoinCost) return false;
            player.coin -= database.appeaseCoinCost;
            p.loyalty = Mathf.Clamp(p.loyalty + database.appeaseLoyaltyGain, 0, 100);
            return true;
        }

        /// <summary>사성(賜姓) — 혼인보다 가벼운 호족 포섭 수단.</summary>
        public bool TryGrantSurname(string provinceId)
        {
            var p = GetProvince(provinceId);
            var player = PlayerFaction;
            if (p == null || player == null || player.prestige < database.grantSurnamePrestigeCost) return false;
            player.prestige -= database.grantSurnamePrestigeCost;
            p.loyalty = Mathf.Clamp(p.loyalty + database.grantSurnameLoyaltyGain, 0, 100);
            return true;
        }

        /// <summary>숙청 명령(§3.2) — 영지 전체 병력을 동원해 대상 호족을 선제 토벌한다. 손실은 보유 지역 전체에 비례 배분.</summary>
        public bool TryPurge(string provinceId, out BattleReport report)
        {
            report = null;
            var p = GetProvince(provinceId);
            if (p == null) return false;
            var sent = PlayerGarrison();
            report = ResolveBattle(PlayerFactionId, sent, provinceId, GarrisonList(p));
            ApplyAttackerLosses(sent, report.attackerSurvivors);
            if (report.victory)
            {
                p.ownerFactionId = PlayerFactionId;
                p.loyalty = 40;
            }
            return true;
        }

        List<UnitCount> PlayerGarrison()
        {
            var list = new List<UnitCount>();
            foreach (var p in ProvincesOf(PlayerFactionId)) list.AddRange(p.garrison);
            return list;
        }

        static List<UnitCount> GarrisonList(ProvinceState p) => p.garrison;

        /// <summary>sentTotals 대비 survivors 비율만큼만 남기고, 보유한 모든 지역의 주둔군에서 병종별로 비례 차감한다.</summary>
        void ApplyAttackerLosses(List<UnitCount> sentTotals, List<UnitCount> survivors)
        {
            foreach (var sent in sentTotals)
            {
                int survived = survivors.FirstOrDefault(s => s.unitId == sent.unitId)?.count ?? 0;
                if (sent.count <= 0) continue;
                float ratio = Mathf.Clamp01((float)survived / sent.count);
                foreach (var p in ProvincesOf(PlayerFactionId))
                {
                    var g = p.garrison.FirstOrDefault(u => u.unitId == sent.unitId);
                    if (g != null) g.count = Mathf.FloorToInt(g.count * ratio);
                }
            }
        }

        int GarrisonTotal(ProvinceState p) => p.garrison.Sum(u => u.count);

        // ───────── 경제·농업 (§3.6) ─────────

        public bool TryUpgradeAgriculture(string provinceId) => TryStartConstruction(provinceId, "agriculture");
        public bool TryUpgradeIrrigation(string provinceId) => TryStartConstruction(provinceId, "irrigation");

        bool TryStartConstruction(string provinceId, string what)
        {
            var p = GetProvince(provinceId);
            var player = PlayerFaction;
            if (p == null || player == null || p.ownerFactionId != PlayerFactionId) return false;
            if (!string.IsNullOrEmpty(p.constructionId)) return false;
            int level = what == "agriculture" ? p.agricultureLevel : p.irrigationLevel;
            int cost = 40 * (level + 1);
            if (player.grain < cost) return false;
            player.grain -= cost;
            p.constructionId = what;
            p.constructionSeasonsLeft = 2;
            return true;
        }

        public void SetTaxRate(string provinceId, float rate)
        {
            var p = GetProvince(provinceId);
            if (p != null) p.taxRate = Mathf.Clamp01(rate);
        }

        // ───────── 병영·징집 / 출진 (§3.9, §3.11) ─────────

        public bool TryRecruit(string provinceId, string unitId, int count)
        {
            var p = GetProvince(provinceId);
            var unit = database.GetUnit(unitId);
            var player = PlayerFaction;
            if (p == null || unit == null || player == null || count <= 0) return false;
            int totalCoin = 0, totalGrain = 0;
            foreach (var c in unit.recruitCost)
            {
                if (c.resource == database.coin) totalCoin += c.amount * count;
                else if (c.resource == database.grain) totalGrain += c.amount * count;
            }
            if (player.coin < totalCoin || player.grain < totalGrain) return false;
            player.coin -= totalCoin;
            player.grain -= totalGrain;

            var existing = p.garrison.FirstOrDefault(u => u.unitId == unitId);
            if (existing != null) existing.count += count;
            else p.garrison.Add(new UnitCount { unitId = unitId, count = count });
            return true;
        }

        /// <summary>출진 — fromProvinceId의 병력을 targetProvinceId로 보내 즉시 교전(§3.10 MVP는 즉시 계산).</summary>
        public BattleReport Deploy(string fromProvinceId, string targetProvinceId, List<UnitCount> army)
        {
            var from = GetProvince(fromProvinceId);
            var target = GetProvince(targetProvinceId);
            if (from == null || target == null) return null;

            int totalFood = army.Sum(a => a.count * (database.GetUnit(a.unitId)?.foodUpkeepPerSoldier ?? 0));
            var player = PlayerFaction;
            if (player == null || player.grain < totalFood) return null;
            player.grain -= totalFood;

            foreach (var a in army)
            {
                var g = from.garrison.FirstOrDefault(u => u.unitId == a.unitId);
                if (g == null || g.count < a.count) return null;
            }
            foreach (var a in army)
                from.garrison.First(u => u.unitId == a.unitId).count -= a.count;

            var report = ResolveBattle(PlayerFactionId, army, targetProvinceId, target.garrison);
            if (report.victory)
            {
                target.ownerFactionId = PlayerFactionId;
                target.loyalty = 40;
            }
            // 승리했으면 살아남은 아군이 점령지에 주둔, 패배했으면 본거지(from)로 퇴각
            var home = report.victory ? target : from;
            foreach (var r in report.attackerSurvivors)
            {
                var g = home.garrison.FirstOrDefault(u => u.unitId == r.unitId);
                if (g != null) g.count += r.count; else home.garrison.Add(r);
            }
            return report;
        }

        // ───────── 전투 (§5.5) ─────────

        public class BattleReport
        {
            public bool victory;
            public float attackerPower, defenderPower;
            public List<UnitCount> attackerSurvivors = new();
            public List<UnitCount> defenderSurvivors = new();
        }

        /// <summary>§5.5: 병종별 전투력 = 인원 수 × 상성 배율, 합산 후 비율로 승패·손실 결정.</summary>
        public BattleReport ResolveBattle(string attackerFactionId, List<UnitCount> attackers, string targetProvinceId, List<UnitCount> defenders)
        {
            float atkPower = 0, defPower = 0;
            foreach (var a in attackers)
            {
                var def = database.GetUnit(a.unitId);
                if (def == null) continue;
                float mult = 1f;
                foreach (var d in defenders)
                {
                    var ddef = database.GetUnit(d.unitId);
                    if (ddef == null) continue;
                    if (UnitClasses.Beats(def.unitClass, ddef.unitClass)) mult = Mathf.Max(mult, database.counterBonus);
                    else if (UnitClasses.Beats(ddef.unitClass, def.unitClass)) mult = Mathf.Min(mult, database.counterPenalty);
                }
                atkPower += a.count * def.power * mult;
            }
            foreach (var d in defenders)
            {
                var def = database.GetUnit(d.unitId);
                if (def == null) continue;
                defPower += d.count * def.power;
            }

            float randomness = UnityEngine.Random.Range(0.9f, 1.1f);
            bool victory = atkPower * randomness >= defPower;
            float ratio = defPower <= 0f ? 0f : Mathf.Clamp01(defPower / Mathf.Max(1f, atkPower));
            float defRatio = atkPower <= 0f ? 0f : Mathf.Clamp01(atkPower / Mathf.Max(1f, defPower));

            var report = new BattleReport { victory = victory, attackerPower = atkPower, defenderPower = defPower };
            foreach (var a in attackers)
            {
                int lost = Mathf.FloorToInt(a.count * (victory ? ratio * 0.3f : ratio));
                int survive = Mathf.Max(0, a.count - lost);
                if (survive > 0) report.attackerSurvivors.Add(new UnitCount { unitId = a.unitId, count = survive });
            }
            foreach (var d in defenders)
            {
                int lost = Mathf.FloorToInt(d.count * (victory ? 1f : defRatio * 0.3f));
                int survive = Mathf.Max(0, d.count - lost);
                if (survive > 0) report.defenderSurvivors.Add(new UnitCount { unitId = d.unitId, count = survive });
            }

            var targetProvince = GetProvince(targetProvinceId);
            if (targetProvince != null) targetProvince.garrison = report.defenderSurvivors;

            BattleResolved?.Invoke(report);
            return report;
        }

        // ───────── AI (§5.8 — MVP부터 공격적) ─────────

        void RunAiTurn()
        {
            foreach (var faction in _factions.Values.ToList())
            {
                if (faction.factionId == PlayerFactionId) continue;
                AiActForFaction(faction);
            }
            // 미복속 호족 지역도 서로/플레이어를 공격할 수 있다 (완산주 일대 정세가 플레이어 없이도 바뀜)
            foreach (var p in _provinces.Values.ToList())
                if (string.IsNullOrEmpty(p.ownerFactionId)) AiActForHojok(p);
        }

        void AiActForFaction(FactionState faction)
        {
            foreach (var p in ProvincesOf(faction.factionId).ToList())
            {
                var target = AdjacentTo(p)
                    .Where(n => n.ownerFactionId != faction.factionId)
                    .OrderBy(n => GarrisonTotal(n))
                    .FirstOrDefault();
                if (target == null) continue;

                int myPower = GarrisonTotal(p);
                int theirPower = GarrisonTotal(target);
                if (myPower < theirPower * 1.3f || myPower < 3) continue; // §5.8: 충분히 강할 때만 공격

                var army = new List<UnitCount>(p.garrison.Select(u => new UnitCount { unitId = u.unitId, count = u.count }));
                p.garrison.Clear();
                var report = ResolveBattle(faction.factionId, army, target.provinceId, target.garrison);
                if (report.victory)
                {
                    target.ownerFactionId = faction.factionId;
                    target.loyalty = 40;
                    target.garrison = report.attackerSurvivors; // 공격한 병력이 점령지에 주둔
                }
                else p.garrison = report.attackerSurvivors; // 패퇴한 병력은 본거지로 돌아옴
            }
        }

        void AiActForHojok(ProvinceState hojok)
        {
            // MVP: 충성도가 낮을수록(불만이 클수록) 인접한 약한 지역을 넘본다. 단순 확률로 둬서 정세가 살아있게 한다.
            if (UnityEngine.Random.value > 0.1f) return;
            var target = AdjacentTo(hojok).Where(n => n.provinceId != hojok.provinceId).OrderBy(n => GarrisonTotal(n)).FirstOrDefault();
            if (target == null || GarrisonTotal(hojok) < GarrisonTotal(target) * 1.3f) return;
            var report = ResolveBattle(hojok.ownerFactionId, hojok.garrison, target.provinceId, target.garrison);
            if (report.victory) { target.ownerFactionId = hojok.ownerFactionId; target.grievance = hojok.grievance; }
        }

        // ───────── 저장/불러오기 ─────────

        public void SaveGame()
        {
            var data = new SaveData
            {
                scenarioId = "",
                playerFactionId = PlayerFactionId,
                year = Year,
                season = Season,
                provinces = _provinces.Values.ToList(),
                factions = _factions.Values.ToList(),
                retainers = _retainers.Values.ToList(),
            };
            SaveSystem.Save(data);
        }

        public bool LoadGame()
        {
            var data = SaveSystem.Load();
            if (data == null) return false;

            _provinces.Clear();
            _factions.Clear();
            _retainers.Clear();
            foreach (var p in data.provinces) _provinces[p.provinceId] = p;
            foreach (var f in data.factions) _factions[f.factionId] = f;
            foreach (var r in data.retainers) _retainers[r.retainerId] = r;

            PlayerFactionId = data.playerFactionId;
            Year = data.year;
            Season = data.season;
            return true;
        }
    }
}
