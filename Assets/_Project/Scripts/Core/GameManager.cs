using System;
using System.Collections.Generic;
using System.Linq;
using Husamguk.Data;
using UnityEngine;

namespace Husamguk.Core
{
    public enum LogKind { Info, Good, Bad, Event, Goal }

    [Serializable]
    public class LogEntry
    {
        public int turn;
        public int year;
        public int season;
        public string text;
        public LogKind kind;
    }

    /// <summary>
    /// 왕국 전체 상태와 계절 턴 진행을 맡는다. GDD §4(핵심 루프), §5(시스템 상세).
    /// 한 계절 = 결산(수확·군량·공사·민심·귀부·승격·호족 동향) → 목표 확인 → 사건 발생 → 자동 저장.
    /// 사건(Story.cs)이 떠 있으면 선택하기 전까지 다음 계절로 넘어가지 않는다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] GameDatabase database;
        public GameDatabase Database => database;

        public const int SubmissionLoyalty = 80;
        public const string RestartFlag = "restart_requested";
        static readonly float[] HarvestBySeason = { 1f, 1f, 1.6f, 0.5f };
        public static readonly string[] SeasonNames = { "봄", "여름", "가을", "겨울" };

        public string PlayerFactionId { get; private set; }
        public string CapitalId { get; private set; }
        public string ScenarioId { get; private set; }
        public int Year { get; private set; }
        public int Season { get; private set; } // 0=봄 1=여름 2=가을 3=겨울
        public int TurnNumber { get; private set; } = 1;
        /// <summary>이룬 목표 수 (목표는 순서와 관계없이 조건을 채우면 달성된다).</summary>
        public int ObjectiveIndex => _completedObjectives.Count;
        public StoryEvent PendingEvent { get; private set; }
        public string EventTarget { get; set; }
        public string LastSubmittedId { get; private set; }
        /// <summary>칼로 빼앗은 고을 수 (호족 연합 사건 등에 쓰인다).</summary>
        public int ConquestCount { get; private set; }
        /// <summary>마지막 플레이어 행동의 결과/실패 이유 (하단 상태바에 표시).</summary>
        public string LastMessage { get; private set; }

        readonly Dictionary<string, ProvinceState> _provinces = new();
        readonly Dictionary<string, FactionState> _factions = new();
        readonly Dictionary<string, RetainerState> _retainers = new();
        readonly HashSet<string> _flags = new();
        readonly HashSet<string> _firedEvents = new();
        readonly Dictionary<string, int> _lastFiredTurn = new();
        readonly List<LogEntry> _log = new();
        readonly List<string> _promotionQueue = new();
        readonly HashSet<string> _completedObjectives = new();

        public IReadOnlyCollection<ProvinceState> Provinces => _provinces.Values;
        public IReadOnlyCollection<FactionState> Factions => _factions.Values;
        public IReadOnlyCollection<RetainerState> Retainers => _retainers.Values;
        public IReadOnlyList<LogEntry> Log => _log;

        public bool HasGame => !string.IsNullOrEmpty(PlayerFactionId) && _provinces.Count > 0;
        /// <summary>§3.17 엔딩 MVP 승리 조건: database에 등록된 지역을 전부 복속하면 통일.</summary>
        public bool IsVictory => HasGame && _provinces.Values.All(p => p.ownerFactionId == PlayerFactionId);
        public bool IsDefeated => HasGame && !OwnedProvinces.Any();

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
        public RetainerDef MonarchDef => database.GetRetainer(PlayerFaction?.monarchRetainerId);

        public ProvinceDef Def(ProvinceState s) => s == null ? null : database.GetProvince(s.provinceId);
        public FactionDef Def(FactionState s) => database.GetFaction(s.factionId);
        public RetainerDef Def(RetainerState s) => database.GetRetainer(s.retainerId);
        public SettlementTierDef Tier(ProvinceState s) => s == null ? null : database.settlementTiers.FirstOrDefault(t => t.id == s.tierId);
        public int TierIndex(ProvinceState s) => Mathf.Max(0, database.settlementTiers.IndexOf(Tier(s)));
        public string TierName(ProvinceState s) => Tier(s)?.displayName ?? "";

        public IEnumerable<ProvinceState> ProvincesOf(string factionId) => _provinces.Values.Where(p => p.ownerFactionId == factionId);
        public IEnumerable<ProvinceState> OwnedProvinces => ProvincesOf(PlayerFactionId);
        public int OwnedCount => OwnedProvinces.Count();
        public IEnumerable<ProvinceState> HojokProvinces => _provinces.Values.Where(p => string.IsNullOrEmpty(p.ownerFactionId));
        public IEnumerable<ProvinceState> AdjacentTo(ProvinceState p) =>
            Def(p) == null ? Enumerable.Empty<ProvinceState>() : Def(p).adjacentProvinceIds.Select(GetProvince).Where(x => x != null);

        /// <summary>본거지. 잃었으면 남은 영지 중 하나.</summary>
        public ProvinceState Capital
        {
            get
            {
                var c = GetProvince(CapitalId);
                if (c != null && c.ownerFactionId == PlayerFactionId) return c;
                return OwnedProvinces.FirstOrDefault();
            }
        }

        public bool IsStronghold(ProvinceState p) => Def(p) != null && Def(p).hostileStronghold;
        public bool IsAdjacentToPlayer(ProvinceState p) => AdjacentTo(p).Any(n => n.ownerFactionId == PlayerFactionId);
        public string ProvinceName(string id) => database.GetProvince(id)?.displayName ?? id ?? "";
        public string HojokName(ProvinceState p) =>
            p == null ? "" : string.IsNullOrEmpty(p.grievance) ? $"{ProvinceName(p.provinceId)} 호족" : p.grievance;

        public int GarrisonTotal(ProvinceState p) => p == null ? 0 : p.garrison.Sum(u => u.count);
        public int TotalTroops() => OwnedProvinces.Sum(p => GarrisonTotal(p));

        public bool HasFlag(string flag) => _flags.Contains(flag);
        public void SetFlag(string flag) => _flags.Add(flag);
        public bool ConsumeFlag(string flag) => _flags.Remove(flag);
        public int TurnsSinceFired(string eventId) => _lastFiredTurn.TryGetValue(eventId, out var t) ? TurnNumber - t : 9999;

        /// <summary>아직 이루지 못한 첫 목표 (화면·책사가 안내하는 목표).</summary>
        public Objective CurrentObjective => ObjectiveBook.All.FirstOrDefault(o => !_completedObjectives.Contains(o.id));
        public bool IsObjectiveDone(Objective o) => o != null && _completedObjectives.Contains(o.id);

        // ───────── 경제 예측 (UI·책사용) ─────────

        int BaseYield(ProvinceState p)
        {
            var provDef = Def(p);
            float tierBonus = 1f + TierIndex(p) * 0.15f;
            // 민심이 낮으면 일손이 놀고 세금이 샌다 — 칼로 얻은 땅(충성 30)은 달랠 때까지 덜 거둔다
            float loyaltyFactor = 0.6f + Mathf.Clamp(p.loyalty, 0, 100) / 250f;
            return Mathf.RoundToInt((14 + p.agricultureLevel * 8 + p.irrigationLevel * 5) * (1f + (provDef?.grainBonus ?? 0f)) * tierBonus * loyaltyFactor);
        }

        public int GrainYield(ProvinceState p) => Mathf.RoundToInt(BaseYield(p) * HarvestBySeason[Season % 4]);
        public int CoinYield(ProvinceState p) => Mathf.RoundToInt(4 + BaseYield(p) * p.taxRate * 2f);
        public int ForecastGrain() => OwnedProvinces.Sum(p => GrainYield(p));
        public int ForecastCoin() => OwnedProvinces.Sum(p => CoinYield(p));
        /// <summary>목표 충성도: 세율이 낮을수록 높다. 실제 충성도는 계절마다 이 값 쪽으로 3씩 움직인다.</summary>
        public int LoyaltyTarget(ProvinceState p) => Mathf.RoundToInt(70f - p.taxRate * 100f);

        public int GrainUpkeep() => OwnedProvinces.Sum(p => p.garrison.Sum(u =>
            Mathf.CeilToInt(u.count * (database.GetUnit(u.unitId)?.foodUpkeepPerSoldier ?? 3) / 3f)));

        public int ConstructionCost(ProvinceState p, string what)
        {
            int level = what == "agriculture" ? p.agricultureLevel : p.irrigationLevel;
            float mult = HasFlag("scholar") ? 0.75f : 1f;
            return Mathf.RoundToInt(40 * (level + 1) * mult);
        }

        /// <summary>한 계절에 모을 수 있는 장정 수 — 고을이 클수록 많고, 민심이 흉흉하면(충성 50 미만) 장정이 모이지 않는다.</summary>
        public int RecruitCap(ProvinceState p)
        {
            int baseCap = 5 + TierIndex(p) * 5;
            if (p.loyalty >= 50) return baseCap;
            if (p.loyalty >= 35) return baseCap / 2;
            return 0;
        }

        /// <summary>세력이 커질수록 남은 호족들이 경계해 포섭 비용이 오른다.</summary>
        public int AppeaseCost() => database.appeaseCoinCost + 10 * Mathf.Max(0, OwnedCount - 1);
        public int SurnameCost() => database.grantSurnamePrestigeCost + 5 * Mathf.Max(0, OwnedCount - 1);

        // ───────── 새 게임 (§3.18 시나리오 선택) ─────────

        public void NewGame(ScenarioDef scenario, ScenarioDef.PlayableCharacter pick)
        {
            ClearState();
            ScenarioId = scenario.id;
            Year = scenario.year;
            Season = 0;
            TurnNumber = 1;
            PlayerFactionId = pick.startingFaction.id;
            CapitalId = pick.startingProvince != null ? pick.startingProvince.id : null;

            foreach (var provDef in database.provinces)
            {
                var ps = new ProvinceState
                {
                    provinceId = provDef.id,
                    ownerFactionId = provDef.startingFaction != null && provDef.startingFaction.id != PlayerFactionId ? provDef.startingFaction.id : null,
                    tierId = provDef.startingTier != null ? provDef.startingTier.id : database.settlementTiers[0].id,
                    grievance = provDef.startingHojokName,
                    loyalty = provDef.hostileStronghold ? 0 : 45,
                };
                ps.garrison = StartingGarrison(provDef);
                _provinces[ps.provinceId] = ps;
            }
            var cap = GetProvince(CapitalId);
            if (cap != null)
            {
                cap.ownerFactionId = PlayerFactionId;
                cap.loyalty = 70;
                AddTroops(cap.provinceId, "infantry", 4);
            }

            foreach (var facDef in database.factions)
                _factions[facDef.id] = new FactionState { factionId = facDef.id, grain = 150, coin = 60, prestige = 15 };

            AddRetainer(pick.retainer.id, 25);
            PlayerFaction.monarchRetainerId = pick.retainer.id;
            if (database.GetRetainer("choieung") != null && pick.retainer.id != "choieung") AddRetainer("choieung", 32);

            AddLog($"{Year}년 봄, {Ko.I(pick.retainer.displayName)} {Ko.Eul(ProvinceName(CapitalId))} 근거지로 일어섰다.", LogKind.Event);
            TryQueueEvent(false);
        }

        void AddRetainer(string retainerId, int age)
        {
            var r = new RetainerState { retainerId = retainerId, factionId = PlayerFactionId, status = RetainerStatus.Office, age = age, loyalty = 90 };
            _retainers[retainerId] = r;
            PlayerFaction.retainerIds.Add(retainerId);
        }

        List<UnitCount> StartingGarrison(ProvinceDef def)
        {
            if (def.hostileStronghold)
                return new List<UnitCount>
                {
                    new() { unitId = "spear", count = 60 },
                    new() { unitId = "archer", count = 25 },
                    new() { unitId = "cavalry", count = 20 },
                };
            int tier = Mathf.Max(0, database.settlementTiers.IndexOf(def.startingTier));
            var list = new List<UnitCount> { new() { unitId = "infantry", count = 14 } };
            if (tier >= 1) list.Add(new UnitCount { unitId = "spear", count = 8 * tier });
            return list;
        }

        void ClearState()
        {
            _provinces.Clear();
            _factions.Clear();
            _retainers.Clear();
            _flags.Clear();
            _firedEvents.Clear();
            _lastFiredTurn.Clear();
            _log.Clear();
            _promotionQueue.Clear();
            _completedObjectives.Clear();
            PendingEvent = null;
            EventTarget = null;
            LastSubmittedId = null;
            LastMessage = null;
            ConquestCount = 0;
        }

        // ───────── 기록 ─────────

        public void AddLog(string text, LogKind kind = LogKind.Info) =>
            _log.Add(new LogEntry { turn = TurnNumber, year = Year, season = Season, text = text, kind = kind });

        // ───────── 계절 턴 (§4) ─────────

        public void AdvanceSeason()
        {
            if (!HasGame || PendingEvent != null) return;

            TickEconomy();
            TickUpkeep();
            TickConstruction();
            TickLoyaltyDrift();
            TickHojokSubmission();
            TickSettlementTier();
            TickHojokGrowth();
            if (Season == database.seasonsPerYear - 1) TickAging();
            RunAiTurn();
            foreach (var p in _provinces.Values) p.recruitedThisSeason = 0;

            Season++;
            if (Season >= database.seasonsPerYear) { Season = 0; Year++; }
            TurnNumber++;
            LastMessage = null;

            CheckObjectives();
            TryQueueEvent(false);
            SeasonAdvanced?.Invoke();
            SaveGame();
        }

        void TickEconomy()
        {
            int grainSum = 0, coinSum = 0;
            foreach (var p in OwnedProvinces.ToList())
            {
                int g = GrainYield(p);
                int c = CoinYield(p);
                grainSum += g;
                coinSum += c;
                p.cumulativeEconomy += c;
            }
            var player = PlayerFaction;
            if (player == null) return;
            player.grain += grainSum;
            player.coin += coinSum;
            player.prestige += 1; // §5.1 권위: 턴마다 소량 자동
            string harvest = Season == 2 ? " (가을 대수확)" : Season == 3 ? " (겨울 흉작)" : "";
            AddLog($"수확{harvest}: 석 +{grainSum}, 엽전 +{coinSum}, 권위 +1", LogKind.Good);
        }

        void TickUpkeep()
        {
            var player = PlayerFaction;
            if (player == null) return;
            int upkeep = GrainUpkeep();
            if (upkeep <= 0) return;
            player.grain -= upkeep;
            if (player.grain >= 0)
            {
                AddLog($"군량 소모: 석 -{upkeep} (병력 {TotalTroops()}명)");
                return;
            }
            player.grain = 0;
            int fled = LoseTroopsPercent(0.2f);
            ChangeAllOwnedLoyalty(-5);
            AddLog($"군량이 바닥났습니다! 굶주린 병사 {fled}명이 달아나고 민심이 흉흉해졌습니다.", LogKind.Bad);
        }

        void TickConstruction()
        {
            foreach (var p in OwnedProvinces.ToList())
            {
                if (string.IsNullOrEmpty(p.constructionId)) continue;
                p.constructionSeasonsLeft--;
                if (p.constructionSeasonsLeft > 0) continue;
                if (p.constructionId == "agriculture")
                {
                    p.agricultureLevel++;
                    AddLog($"{ProvinceName(p.provinceId)}: 논 개간 완료! 농업 Lv.{p.agricultureLevel}", LogKind.Good);
                }
                else if (p.constructionId == "irrigation")
                {
                    p.irrigationLevel++;
                    AddLog($"{ProvinceName(p.provinceId)}: 저수지 정비 완료! 저수지 Lv.{p.irrigationLevel}", LogKind.Good);
                }
                p.constructionId = null;
            }
        }

        /// <summary>§5.2 충성도: 내 영지는 세율이 정하는 목표치로 3씩 수렴, 미복속 호족은 50 쪽으로 1씩 식는다.</summary>
        void TickLoyaltyDrift()
        {
            foreach (var p in _provinces.Values)
            {
                if (p.ownerFactionId == PlayerFactionId)
                {
                    int before = p.loyalty;
                    int target = LoyaltyTarget(p);
                    p.loyalty = p.loyalty < target ? Mathf.Min(target, p.loyalty + 3) : Mathf.Max(target, p.loyalty - 3);
                    if (before >= 35 && p.loyalty < 35)
                        AddLog($"{ProvinceName(p.provinceId)}의 민심이 흉흉해지고 있습니다 (충성 {p.loyalty}). 세율을 낮추십시오.", LogKind.Bad);
                }
                else if (string.IsNullOrEmpty(p.ownerFactionId) && !IsStronghold(p))
                {
                    if (p.loyalty > 50) p.loyalty--;
                    else if (p.loyalty < 50 && p.loyalty >= 30) p.loyalty++;
                }
            }
        }

        /// <summary>충성 80 이상인 이웃 호족은 계절이 바뀔 때 스스로 귀부한다 — 사병도 그대로 따라온다.</summary>
        void TickHojokSubmission()
        {
            foreach (var p in HojokProvinces.ToList())
            {
                if (IsStronghold(p) || p.loyalty < SubmissionLoyalty || !IsAdjacentToPlayer(p)) continue;
                p.ownerFactionId = PlayerFactionId;
                p.loyalty = 70;
                LastSubmittedId = p.provinceId;
                AddLog($"{Ko.I(HojokName(p))} 스스로 성문을 열고 귀부했습니다! {ProvinceName(p.provinceId)}의 사병 {GarrisonTotal(p)}명이 휘하에 들어왔습니다.", LogKind.Good);
            }
        }

        /// <summary>§5.6 거점 등급 승격 — 임계치를 넘으면 자동 승격, 플레이어 영지면 승격 사건(§3.13)을 띄운다.</summary>
        void TickSettlementTier()
        {
            foreach (var p in OwnedProvinces.ToList())
            {
                var tier = Tier(p);
                if (tier == null) continue;
                var next = database.NextTier(tier);
                if (next == null) continue;
                if (p.cumulativeEconomy < tier.economyThreshold) continue;
                if (GarrisonTotal(p) < tier.militaryThreshold && p.cumulativeMilitary < tier.militaryThreshold) continue;

                p.tierId = next.id;
                SetFlag("promoted_once");
                // 승격 축하 사건은 그 등급에 처음 오를 때만 띄운다 (영지가 많아지면 매 계절 뜨지 않도록). 나머지는 보고에만 남긴다.
                if (!HasFlag("tier_reached_" + next.id))
                {
                    SetFlag("tier_reached_" + next.id);
                    _promotionQueue.Add(p.provinceId);
                }
                AddLog($"{Ko.I(ProvinceName(p.provinceId))} {Ko.Ro(next.displayName)} 승격했습니다!", LogKind.Good);
                ProvincePromoted?.Invoke(p, next);
            }
        }

        /// <summary>미복속 호족·완산주는 시간이 지날수록 사병을 늘린다 — 미룰수록 비싸진다.</summary>
        void TickHojokGrowth()
        {
            if (TurnNumber % 2 != 0) return;
            foreach (var p in HojokProvinces)
            {
                bool stronghold = IsStronghold(p);
                int cap = stronghold ? 130 : 20 + TierIndex(p) * 10;
                if (GarrisonTotal(p) >= cap) continue;
                string unit = stronghold ? "spear" : "infantry";
                AddTroops(p.provinceId, unit, stronghold ? 2 : 1);
            }
        }

        /// <summary>§5.9 군주 수명 — 1년에 한 번 나이를 먹는다. 후계자가 없으면(MVP) 군주는 그대로 남는다.</summary>
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

        /// <summary>§5.4 왕위 계승 1~2단계: 명분 최고 후계자가 자리를 물려받는다. 3~4단계(파벌 처분)는 2차 범위.</summary>
        void ResolveSuccession(FactionState faction)
        {
            var heirs = faction.retainerIds
                .Select(GetRetainer)
                .Where(r => r != null && Def(r) != null && Def(r).isHeir)
                .OrderByDescending(r => r.legitimacyScore)
                .ToList();
            if (heirs.Count == 0) return;

            var next = heirs[0];
            faction.monarchRetainerId = next.retainerId;
            next.status = RetainerStatus.Office;
            MonarchDied?.Invoke(faction.factionId);
        }

        // ───────── 목표 (§1.4) ─────────

        public int ObjectiveProgress(Objective o) => o == null ? 0 : o.progress(this);
        public int ObjectiveTarget(Objective o) => o == null ? 1 : ObjectiveBook.TargetFor(o, this);

        /// <summary>순서와 관계없이 조건을 채운 목표는 모두 달성 처리한다 (한 목표에 막혀 전체가 멈추지 않도록).</summary>
        public void CheckObjectives()
        {
            foreach (var o in ObjectiveBook.All)
            {
                if (_completedObjectives.Contains(o.id)) continue;
                if (ObjectiveProgress(o) < ObjectiveTarget(o)) continue;
                _completedObjectives.Add(o.id);
                o.reward?.Invoke(this);
                AddLog($"목표 달성 — 「{o.title}」 (보상: {o.rewardText})", LogKind.Goal);
            }
        }

        // ───────── 사건 (Story.cs) ─────────

        void TryQueueEvent(bool urgentOnly)
        {
            if (PendingEvent != null) return;
            foreach (var ev in StoryBook.All)
            {
                if (urgentOnly && !ev.urgent) continue;
                if (!ev.repeatable && _firedEvents.Contains(ev.id)) continue;
                if (ev.repeatable && TurnsSinceFired(ev.id) < ev.cooldownTurns) continue;
                EventTarget = null;
                bool ok;
                try { ok = ev.trigger(this); }
                catch (Exception e) { Debug.LogException(e); ok = false; }
                if (!ok) continue;
                PendingEvent = ev;
                _firedEvents.Add(ev.id);
                _lastFiredTurn[ev.id] = TurnNumber;
                return;
            }
        }

        public bool CanAfford(StoryChoice ch)
        {
            var f = PlayerFaction;
            return f != null && f.grain >= ch.grain && f.coin >= ch.coin && f.prestige >= ch.prestige && TotalTroops() >= ch.minTroops;
        }

        /// <summary>떠 있는 사건의 선택지를 고른다. 고를 수 없으면 null, 고르면 결과 서술.</summary>
        public string ResolveEvent(int index)
        {
            var ev = PendingEvent;
            if (ev == null || index < 0 || index >= ev.choices.Count) return null;
            var ch = ev.choices[index];
            if (!CanAfford(ch)) return null;
            var f = PlayerFaction;
            f.grain -= ch.grain;
            f.coin -= ch.coin;
            f.prestige -= ch.prestige;
            string result;
            try { result = ch.apply != null ? ch.apply(this) : ""; }
            catch (Exception e) { Debug.LogException(e); result = ""; }
            AddLog($"〔{ev.title}〕 {ch.text} — {result}", LogKind.Event);
            PendingEvent = null;
            CheckObjectives();
            TryQueueEvent(true);
            if (!HasFlag(RestartFlag)) SaveGame();
            return result;
        }

        public string PeekPromotion() => _promotionQueue.Count > 0 ? _promotionQueue[0] : null;
        public void PopPromotion() { if (_promotionQueue.Count > 0) _promotionQueue.RemoveAt(0); }

        // ───────── 사건·행동용 헬퍼 ─────────

        public void Gain(int grain, int coin, int prestige)
        {
            var f = PlayerFaction;
            if (f == null) return;
            f.grain = Mathf.Max(0, f.grain + grain);
            f.coin = Mathf.Max(0, f.coin + coin);
            f.prestige = Mathf.Max(0, f.prestige + prestige);
        }

        /// <summary>곡식을 비율만큼 잃는다(약탈·흉작). 잃은 양을 돌려준다.</summary>
        public int Plunder(float ratio)
        {
            var f = PlayerFaction;
            if (f == null) return 0;
            int lost = Mathf.RoundToInt(f.grain * ratio);
            f.grain -= lost;
            return lost;
        }

        public void ChangeLoyalty(ProvinceState p, int delta)
        {
            if (p != null) p.loyalty = Mathf.Clamp(p.loyalty + delta, 0, 100);
        }

        public void ChangeAllOwnedLoyalty(int delta)
        {
            foreach (var p in OwnedProvinces.ToList()) ChangeLoyalty(p, delta);
        }

        public void ChangeAllHojokLoyalty(int delta)
        {
            foreach (var p in HojokProvinces.Where(x => !IsStronghold(x)).ToList()) ChangeLoyalty(p, delta);
        }

        public void AddTroops(string provinceId, string unitId, int count)
        {
            var p = GetProvince(provinceId);
            if (p == null || count <= 0) return;
            var g = p.garrison.FirstOrDefault(u => u.unitId == unitId);
            if (g != null) g.count += count; else p.garrison.Add(new UnitCount { unitId = unitId, count = count });
        }

        public int LoseTroopsAt(string provinceId, float ratio)
        {
            var p = GetProvince(provinceId);
            if (p == null) return 0;
            int lost = 0;
            foreach (var g in p.garrison)
            {
                int l = Mathf.CeilToInt(g.count * ratio);
                l = Mathf.Min(l, g.count);
                g.count -= l;
                lost += l;
            }
            p.garrison.RemoveAll(g => g.count <= 0);
            return lost;
        }

        public int LoseTroopsPercent(float ratio) => OwnedProvinces.ToList().Sum(p => LoseTroopsAt(p.provinceId, ratio));

        public void LoseProvinceToRebels(ProvinceState p)
        {
            if (p == null) return;
            p.ownerFactionId = null;
            p.grievance = $"{ProvinceName(p.provinceId)} 반란군";
            p.loyalty = 10;
            p.constructionId = null;
            p.garrison = new List<UnitCount> { new() { unitId = "infantry", count = 6 } };
        }

        /// <summary>내 영지(provinceId)를 적(enemy)이 공격. 막아내면 true. 잃은 병력 수를 돌려준다.</summary>
        public bool DefendAt(string provinceId, List<UnitCount> enemy, out int lost)
        {
            var p = GetProvince(provinceId);
            int before = GarrisonTotal(p);
            var report = ResolveBattle(null, enemy, provinceId, p.garrison);
            lost = before - GarrisonTotal(p);
            return !report.victory;
        }

        /// <summary>내 영지(provinceId)의 수비병으로 도적 같은 중립 무리를 친다. 이기면 true.</summary>
        public bool AttackNeutral(string provinceId, List<UnitCount> enemy, out int lost)
        {
            var p = GetProvince(provinceId);
            var army = p.garrison.Select(u => new UnitCount { unitId = u.unitId, count = u.count }).ToList();
            int before = army.Sum(u => u.count);
            var report = ResolveBattle(PlayerFactionId, army, null, enemy);
            p.garrison = report.attackerSurvivors;
            lost = before - GarrisonTotal(p);
            return report.victory;
        }

        // ───────── 호족 판도 액션 (§3.2) ─────────

        bool CheckHojokTarget(ProvinceState p)
        {
            if (p == null || !string.IsNullOrEmpty(p.ownerFactionId)) { LastMessage = "이미 우리 땅이거나 대상이 없습니다."; return false; }
            if (IsStronghold(p)) { LastMessage = "견훤의 직할지는 회유할 수 없습니다. 칼로 빼앗을 수밖에요."; return false; }
            if (!IsAdjacentToPlayer(p)) { LastMessage = $"{Ko.Eun(ProvinceName(p.provinceId))} 너무 멀어 우리 영향력이 닿지 않습니다. 맞닿은 고을부터 얻으십시오."; return false; }
            return true;
        }

        public int AppeaseGain()
        {
            int charm = MonarchDef != null ? MonarchDef.charm : 50;
            return Mathf.Max(3, database.appeaseLoyaltyGain + (charm - 50) / 5);
        }

        public bool TryAppeaseHojok(string provinceId)
        {
            var p = GetProvince(provinceId);
            var player = PlayerFaction;
            if (!CheckHojokTarget(p)) return false;
            int cost = AppeaseCost();
            if (player.coin < cost) { LastMessage = $"엽전이 모자랍니다 (필요 {cost})."; return false; }
            player.coin -= cost;
            int gain = AppeaseGain();
            ChangeLoyalty(p, gain);
            LastMessage = $"{HojokName(p)}에게 재물을 보냈습니다. 충성 +{gain} → {p.loyalty}" + (p.loyalty >= SubmissionLoyalty ? " — 다음 계절에 귀부할 것입니다!" : "");
            OnPlayerActed();
            return true;
        }

        /// <summary>사성(賜姓) — 권위를 써서 성씨를 내려 호족을 포섭.</summary>
        public bool TryGrantSurname(string provinceId)
        {
            var p = GetProvince(provinceId);
            var player = PlayerFaction;
            if (!CheckHojokTarget(p)) return false;
            int cost = SurnameCost();
            if (player.prestige < cost) { LastMessage = $"권위가 모자랍니다 (필요 {cost})."; return false; }
            player.prestige -= cost;
            ChangeLoyalty(p, database.grantSurnameLoyaltyGain);
            LastMessage = $"{HojokName(p)}에게 성씨를 내렸습니다. 충성 +{database.grantSurnameLoyaltyGain} → {p.loyalty}" + (p.loyalty >= SubmissionLoyalty ? " — 다음 계절에 귀부할 것입니다!" : "");
            OnPlayerActed();
            return true;
        }

        /// <summary>토벌 — 모든 영지의 병력을 동원해 맞닿은 고을을 친다. 이기면 차지하지만, 다른 호족들이 겁을 먹고 등을 돌린다(딜레마).</summary>
        public bool TryPurge(string provinceId, out BattleReport report)
        {
            report = null;
            var p = GetProvince(provinceId);
            if (p == null || p.ownerFactionId == PlayerFactionId) { LastMessage = "대상이 없습니다."; return false; }
            if (!IsAdjacentToPlayer(p)) { LastMessage = $"{Ko.Eun(ProvinceName(p.provinceId))} 우리 땅과 맞닿아 있지 않습니다."; return false; }
            var sent = OwnedProvinces.SelectMany(x => x.garrison).GroupBy(u => u.unitId)
                .Select(g => new UnitCount { unitId = g.Key, count = g.Sum(u => u.count) }).Where(u => u.count > 0).ToList();
            if (sent.Sum(u => u.count) == 0) { LastMessage = "보낼 병사가 없습니다. 먼저 장정을 모으십시오."; return false; }

            report = ResolveBattle(PlayerFactionId, sent, provinceId, p.garrison);
            ApplyAttackerLosses(sent, report.attackerSurvivors);
            AfterPlayerAttack(p, report);
            OnPlayerActed();
            return true;
        }

        void AfterPlayerAttack(ProvinceState target, BattleReport report)
        {
            string name = ProvinceName(target.provinceId);
            if (report.victory)
            {
                string hojok = HojokName(target);
                bool stronghold = IsStronghold(target);
                target.ownerFactionId = PlayerFactionId;
                target.loyalty = 30;
                target.cumulativeMilitary += 1;
                ConquestCount++;
                foreach (var other in HojokProvinces.Where(x => !IsStronghold(x)).ToList()) ChangeLoyalty(other, -10);
                if (stronghold) { SetFlag("took_wansanju"); Gain(0, 0, 15); }
                LastMessage = $"{Ko.Eul(name)} 함락했습니다! 아군 {report.attackerLost}명 손실. 다른 호족들이 두려움에 떨며 등을 돌립니다 (이웃 충성 -10).";
                AddLog($"{Ko.Eul(name)} 무력으로 차지했다 ({hojok} 토벌, 아군 {report.attackerLost}명 손실).", LogKind.Good);
            }
            else
            {
                LastMessage = $"{name} 공격 실패. 아군 {report.attackerLost}명을 잃고 물러났습니다.";
                AddLog($"{name} 공격에 실패했다 (아군 {report.attackerLost}명 손실).", LogKind.Bad);
            }
        }

        /// <summary>sentTotals 대비 survivors 비율만큼만 남기고, 보유한 모든 지역의 주둔군에서 병종별로 비례 차감한다.</summary>
        void ApplyAttackerLosses(List<UnitCount> sentTotals, List<UnitCount> survivors)
        {
            foreach (var sent in sentTotals)
            {
                int survived = survivors.FirstOrDefault(s => s.unitId == sent.unitId)?.count ?? 0;
                if (sent.count <= 0) continue;
                float ratio = Mathf.Clamp01((float)survived / sent.count);
                foreach (var p in OwnedProvinces)
                {
                    var g = p.garrison.FirstOrDefault(u => u.unitId == sent.unitId);
                    if (g != null) g.count = Mathf.FloorToInt(g.count * ratio);
                }
            }
            foreach (var p in OwnedProvinces) p.garrison.RemoveAll(g => g.count <= 0);
        }

        // ───────── 경제·농업 (§3.6) ─────────

        public bool TryUpgradeAgriculture(string provinceId) => TryStartConstruction(provinceId, "agriculture");
        public bool TryUpgradeIrrigation(string provinceId) => TryStartConstruction(provinceId, "irrigation");

        bool TryStartConstruction(string provinceId, string what)
        {
            var p = GetProvince(provinceId);
            var player = PlayerFaction;
            if (p == null || player == null || p.ownerFactionId != PlayerFactionId) return false;
            if (!string.IsNullOrEmpty(p.constructionId)) { LastMessage = "이 고을은 이미 공사 중입니다."; return false; }
            int cost = ConstructionCost(p, what);
            if (player.grain < cost) { LastMessage = $"석이 모자랍니다 (필요 {cost})."; return false; }
            player.grain -= cost;
            p.constructionId = what;
            p.constructionSeasonsLeft = 2;
            SetFlag("built_once");
            LastMessage = $"{ProvinceName(p.provinceId)}에서 {(what == "agriculture" ? "논 개간" : "저수지 정비")}을 시작했습니다. 두 계절 뒤 완공됩니다.";
            OnPlayerActed();
            return true;
        }

        // ───────── 장터 — 남는 엽전과 곡식을 바꾼다 ─────────

        public const int TradeLot = 20;

        /// <summary>엽전 20 → 석 20 (사들이기) / 석 20 → 엽전 10 (내다 팔기).</summary>
        public bool TryTrade(bool buyGrain)
        {
            var f = PlayerFaction;
            if (f == null) return false;
            if (buyGrain)
            {
                if (f.coin < TradeLot) { LastMessage = $"엽전이 모자랍니다 (필요 {TradeLot})."; return false; }
                f.coin -= TradeLot;
                f.grain += TradeLot;
                LastMessage = $"장터에서 곡식을 사들였습니다. 엽전 -{TradeLot}, 석 +{TradeLot}";
            }
            else
            {
                if (f.grain < TradeLot) { LastMessage = $"석이 모자랍니다 (필요 {TradeLot})."; return false; }
                f.grain -= TradeLot;
                f.coin += TradeLot / 2;
                LastMessage = $"장터에 곡식을 내다 팔았습니다. 석 -{TradeLot}, 엽전 +{TradeLot / 2}";
            }
            OnPlayerActed();
            return true;
        }

        public void SetTaxRate(string provinceId, float rate)
        {
            var p = GetProvince(provinceId);
            if (p != null) p.taxRate = Mathf.Clamp(Mathf.Round(rate * 20f) / 20f, 0f, 0.5f);
        }

        // ───────── 병영·징집 / 출진 (§3.9, §3.11) ─────────

        public bool TryRecruit(string provinceId, string unitId, int count)
        {
            var p = GetProvince(provinceId);
            var unit = database.GetUnit(unitId);
            var player = PlayerFaction;
            if (p == null || unit == null || player == null || count <= 0 || p.ownerFactionId != PlayerFactionId) return false;
            if (p.recruitedThisSeason + count > RecruitCap(p))
            {
                LastMessage = RecruitCap(p) == 0
                    ? $"{ProvinceName(p.provinceId)}의 민심이 흉흉해(충성 {p.loyalty}) 장정이 모이지 않습니다. 세율을 낮춰 민심부터 달래십시오."
                    : $"{ProvinceName(p.provinceId)}에서는 이번 계절에 {Mathf.Max(0, RecruitCap(p) - p.recruitedThisSeason)}명만 더 모을 수 있습니다 (고을이 커지고 민심이 좋을수록 늘어남).";
                return false;
            }
            RecruitCost(unit, count, out int totalGrain, out int totalCoin);
            if (player.coin < totalCoin || player.grain < totalGrain) { LastMessage = $"모집 비용이 모자랍니다 (석 {totalGrain}, 엽전 {totalCoin})."; return false; }
            player.coin -= totalCoin;
            player.grain -= totalGrain;
            p.recruitedThisSeason += count;
            AddTroops(provinceId, unitId, count);
            LastMessage = $"{ProvinceName(p.provinceId)}에서 {unit.displayName} {count}명을 모았습니다.";
            OnPlayerActed();
            return true;
        }

        public void RecruitCost(UnitDef unit, int count, out int grain, out int coin)
        {
            grain = 0; coin = 0;
            if (unit?.recruitCost == null) return;
            foreach (var c in unit.recruitCost)
            {
                if (c.resource == database.coin) coin += c.amount * count;
                else if (c.resource == database.grain) grain += c.amount * count;
            }
        }

        public int MarchFoodCost(List<UnitCount> army) => army.Sum(a => a.count * (database.GetUnit(a.unitId)?.foodUpkeepPerSoldier ?? 0));

        /// <summary>출진 — fromProvinceId의 병력을 맞닿은 targetProvinceId로 보내 즉시 교전(§3.10 MVP는 즉시 계산).</summary>
        public BattleReport Deploy(string fromProvinceId, string targetProvinceId, List<UnitCount> army)
        {
            var from = GetProvince(fromProvinceId);
            var target = GetProvince(targetProvinceId);
            if (from == null || target == null || from.ownerFactionId != PlayerFactionId || target.ownerFactionId == PlayerFactionId) return null;
            if (!AdjacentTo(from).Contains(target)) { LastMessage = $"{Ko.Eun(ProvinceName(fromProvinceId))} {ProvinceName(targetProvinceId)}와(과) 맞닿아 있지 않습니다."; return null; }
            if (army.Sum(a => a.count) <= 0) { LastMessage = "보낼 병사가 없습니다."; return null; }

            int totalFood = MarchFoodCost(army);
            var player = PlayerFaction;
            if (player == null || player.grain < totalFood) { LastMessage = $"원정 군량이 모자랍니다 (석 {totalFood} 필요)."; return null; }
            foreach (var a in army)
            {
                var g = from.garrison.FirstOrDefault(u => u.unitId == a.unitId);
                if (g == null || g.count < a.count) { LastMessage = "주둔 병력보다 많이 보낼 수 없습니다."; return null; }
            }
            player.grain -= totalFood;
            foreach (var a in army)
                from.garrison.First(u => u.unitId == a.unitId).count -= a.count;
            from.garrison.RemoveAll(g => g.count <= 0);

            var report = ResolveBattle(PlayerFactionId, army, targetProvinceId, target.garrison);
            // 승리했으면 살아남은 아군이 점령지에 주둔, 패배했으면 본거지(from)로 퇴각
            var home = report.victory ? target : from;
            AfterPlayerAttack(target, report);
            foreach (var r in report.attackerSurvivors)
            {
                var g = home.garrison.FirstOrDefault(u => u.unitId == r.unitId);
                if (g != null) g.count += r.count; else home.garrison.Add(new UnitCount { unitId = r.unitId, count = r.count });
            }
            OnPlayerActed();
            return report;
        }

        void OnPlayerActed()
        {
            CheckObjectives();
            TryQueueEvent(true);
        }

        // ───────── 전투 (§5.5) ─────────

        public class BattleReport
        {
            public bool victory;
            public float attackerPower, defenderPower;
            public int attackerSent, attackerLost, defenderSent, defenderLost;
            public List<UnitCount> attackerSurvivors = new();
            public List<UnitCount> defenderSurvivors = new();
        }

        /// <summary>성곽 보정 — 촌 1.15 · 현 1.3 · 군 1.45 · 주 1.6 · 경 1.75.</summary>
        public float WallMultiplier(ProvinceState p) => 1.15f + TierIndex(p) * 0.15f;

        float LeadershipMultiplier()
        {
            int lead = MonarchDef != null ? MonarchDef.leadership : 50;
            return 1f + (lead - 50) / 100f;
        }

        /// <summary>
        /// 한 병종이 상대 군세 전체를 상대할 때의 상성 배율 — 상대 구성 비율로 가중 평균한다.
        /// (예: 창병 50·궁병 20·기병 15를 상대하면 궁병이 가장 유리하다. 하나라도 천적이 섞였다고 통째로 불리해지지 않는다.)
        /// </summary>
        float CounterMultiplier(UnitDef unit, List<UnitCount> enemy)
        {
            int total = 0;
            float sum = 0f;
            foreach (var e in enemy)
            {
                var ed = database.GetUnit(e.unitId);
                if (ed == null || e.count <= 0) continue;
                float m = UnitClasses.Beats(unit.unitClass, ed.unitClass) ? database.counterBonus
                    : UnitClasses.Beats(ed.unitClass, unit.unitClass) ? database.counterPenalty : 1f;
                sum += m * e.count;
                total += e.count;
            }
            return total == 0 ? 1f : sum / total;
        }

        /// <summary>상성(창>기>궁>창, 양쪽 모두)과 군주 통솔·수성 이점을 반영한 양측 전투력 (무작위 제외) — 출진 화면의 승산 예측에도 쓴다.</summary>
        public void ComputePowers(string attackerFactionId, List<UnitCount> attackers, string targetProvinceId, List<UnitCount> defenders, out float atk, out float def)
        {
            atk = 0; def = 0;
            foreach (var a in attackers)
            {
                var ad = database.GetUnit(a.unitId);
                if (ad == null) continue;
                atk += a.count * ad.power * CounterMultiplier(ad, defenders);
            }
            foreach (var d in defenders)
            {
                var dd = database.GetUnit(d.unitId);
                if (dd == null) continue;
                def += d.count * dd.power * CounterMultiplier(dd, attackers);
            }
            if (attackerFactionId != null && attackerFactionId == PlayerFactionId) atk *= LeadershipMultiplier();
            var target = GetProvince(targetProvinceId);
            if (target != null)
            {
                def *= WallMultiplier(target); // 성곽: 고을이 클수록 공략이 어렵다 (완산주 같은 주(州)는 1.6배)
                if (target.ownerFactionId == PlayerFactionId && attackerFactionId != PlayerFactionId) def *= LeadershipMultiplier();
            }
        }

        /// <summary>§5.5: 병종별 전투력 = 인원 수 × 상성 배율, 합산 후 비율로 승패·손실 결정.</summary>
        public BattleReport ResolveBattle(string attackerFactionId, List<UnitCount> attackers, string targetProvinceId, List<UnitCount> defenders)
        {
            ComputePowers(attackerFactionId, attackers, targetProvinceId, defenders, out float atkPower, out float defPower);

            float randomness = UnityEngine.Random.Range(0.9f, 1.1f);
            bool victory = atkPower > 0f && atkPower * randomness >= defPower;
            float ratio = defPower <= 0f ? 0f : Mathf.Clamp01(defPower / Mathf.Max(1f, atkPower));
            float defRatio = atkPower <= 0f ? 0f : Mathf.Clamp01(atkPower / Mathf.Max(1f, defPower));

            var report = new BattleReport { victory = victory, attackerPower = atkPower, defenderPower = defPower };
            foreach (var a in attackers)
            {
                report.attackerSent += a.count;
                int lost = Mathf.FloorToInt(a.count * (victory ? ratio * 0.3f : Mathf.Min(1f, ratio * 0.6f)));
                int survive = Mathf.Max(0, a.count - lost);
                report.attackerLost += a.count - survive;
                if (survive > 0) report.attackerSurvivors.Add(new UnitCount { unitId = a.unitId, count = survive });
            }
            foreach (var d in defenders)
            {
                report.defenderSent += d.count;
                int lost = Mathf.FloorToInt(d.count * (victory ? 1f : defRatio * 0.3f));
                int survive = Mathf.Max(0, d.count - lost);
                report.defenderLost += d.count - survive;
                if (survive > 0) report.defenderSurvivors.Add(new UnitCount { unitId = d.unitId, count = survive });
            }

            var targetProvince = GetProvince(targetProvinceId);
            if (targetProvince != null) targetProvince.garrison = report.defenderSurvivors;

            BattleResolved?.Invoke(report);
            return report;
        }

        // ───────── AI (§5.8) ─────────

        void RunAiTurn()
        {
            foreach (var faction in _factions.Values.ToList())
            {
                if (faction.factionId == PlayerFactionId) continue;
                AiActForFaction(faction);
            }
            foreach (var p in HojokProvinces.ToList()) AiHojokRaid(p);
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
                if (myPower < theirPower * 1.3f || myPower < 3) continue;

                var army = p.garrison.Select(u => new UnitCount { unitId = u.unitId, count = u.count }).ToList();
                p.garrison.Clear();
                var report = ResolveBattle(faction.factionId, army, target.provinceId, target.garrison);
                if (report.victory)
                {
                    target.ownerFactionId = faction.factionId;
                    target.loyalty = 40;
                    target.garrison = report.attackerSurvivors;
                }
                else p.garrison = report.attackerSurvivors;
            }
        }

        /// <summary>우리에게 적의를 품은(충성 30 미만) 이웃 호족은 가끔 국경 고을을 습격해 곡식을 약탈한다.</summary>
        void AiHojokRaid(ProvinceState hojok)
        {
            if (IsStronghold(hojok) || hojok.loyalty >= 30 || GarrisonTotal(hojok) < 4) return;
            if (UnityEngine.Random.value > 0.15f) return;
            var target = AdjacentTo(hojok).Where(n => n.ownerFactionId == PlayerFactionId).OrderBy(n => GarrisonTotal(n)).FirstOrDefault();
            if (target == null) return;

            var raiders = hojok.garrison.Select(u => new UnitCount { unitId = u.unitId, count = Mathf.CeilToInt(u.count * 0.6f) }).ToList();
            var report = ResolveBattle(null, raiders, target.provinceId, target.garrison);
            foreach (var r in raiders)
            {
                int survived = report.attackerSurvivors.FirstOrDefault(s => s.unitId == r.unitId)?.count ?? 0;
                var home = hojok.garrison.FirstOrDefault(u => u.unitId == r.unitId);
                if (home != null) home.count = Mathf.Max(0, home.count - (r.count - survived));
            }
            hojok.garrison.RemoveAll(u => u.count <= 0);

            if (report.victory)
            {
                int taken = Plunder(0.25f);
                ChangeLoyalty(target, -10);
                AddLog($"{HojokName(hojok)}의 사병들이 {Ko.Eul(ProvinceName(target.provinceId))} 습격해 곡식 {taken}석을 약탈했습니다!", LogKind.Bad);
            }
            else
            {
                AddLog($"{HojokName(hojok)}의 습격을 {ProvinceName(target.provinceId)} 수비병이 막아냈습니다. (적 {report.attackerLost}명 사상)", LogKind.Good);
            }
        }

        // ───────── 저장/불러오기 ─────────

        public void SaveGame()
        {
            if (!HasGame) return;
            var data = new SaveData
            {
                scenarioId = ScenarioId,
                playerFactionId = PlayerFactionId,
                capitalId = CapitalId,
                year = Year,
                season = Season,
                turnNumber = TurnNumber,
                completedObjectiveIds = _completedObjectives.ToList(),
                pendingEventId = PendingEvent?.id,
                eventTarget = EventTarget,
                lastSubmittedId = LastSubmittedId,
                conquestCount = ConquestCount,
                provinces = _provinces.Values.ToList(),
                factions = _factions.Values.ToList(),
                retainers = _retainers.Values.ToList(),
                flags = _flags.ToList(),
                firedEventIds = _firedEvents.ToList(),
                lastFiredEventIds = _lastFiredTurn.Keys.ToList(),
                lastFiredTurns = _lastFiredTurn.Values.ToList(),
                log = _log.ToList(),
            };
            try { SaveSystem.Save(data); }
            catch (Exception e) { Debug.LogWarning($"[GameManager] 저장 실패: {e.Message}"); }
        }

        public bool LoadGame()
        {
            var data = SaveSystem.Load();
            if (data == null || data.provinces == null || data.provinces.Count == 0) return false;

            ClearState();
            foreach (var p in data.provinces) _provinces[p.provinceId] = p;
            foreach (var f in data.factions) _factions[f.factionId] = f;
            foreach (var r in data.retainers) _retainers[r.retainerId] = r;
            if (data.flags != null) foreach (var f in data.flags) _flags.Add(f);
            if (data.firedEventIds != null) foreach (var id in data.firedEventIds) _firedEvents.Add(id);
            if (data.lastFiredEventIds != null && data.lastFiredTurns != null)
                for (int i = 0; i < data.lastFiredEventIds.Count && i < data.lastFiredTurns.Count; i++)
                    _lastFiredTurn[data.lastFiredEventIds[i]] = data.lastFiredTurns[i];
            if (data.log != null) _log.AddRange(data.log);

            ScenarioId = data.scenarioId;
            PlayerFactionId = data.playerFactionId;
            CapitalId = data.capitalId;
            Year = data.year;
            Season = data.season;
            TurnNumber = Mathf.Max(1, data.turnNumber);
            if (data.completedObjectiveIds != null) foreach (var id in data.completedObjectiveIds) _completedObjectives.Add(id);
            PendingEvent = StoryBook.Find(data.pendingEventId);
            EventTarget = data.eventTarget;
            LastSubmittedId = string.IsNullOrEmpty(data.lastSubmittedId) ? null : data.lastSubmittedId;
            ConquestCount = data.conquestCount;
            return true;
        }
    }
}
