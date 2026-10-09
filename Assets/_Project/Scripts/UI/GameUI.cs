using System;
using System.Collections.Generic;
using System.Linq;
using Husamguk.Core;
using Husamguk.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Husamguk.UI
{
    /// <summary>
    /// 전체 UI 셸. GDD §3 — 시나리오 선택(§3.18) 다음 메인 셸.
    /// 상단: 가문·자원(다음 계절 예상 증감)·현재 목표·날짜·다음 계절 / 좌측: 탭 / 중앙: 화면 / 우측: 책사 조언(§3.21)·계절 보고 / 하단: 마지막 행동 결과.
    /// 사건(Story.cs)과 전투 결과는 화면 가운데 대화 창으로 띄운다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GameUI : MonoBehaviour
    {
        [SerializeField] StyleSheet styleSheet;

        VisualElement _root;
        VisualElement _scenarioScreen;
        VisualElement _mainShell;
        VisualElement _tabRail;
        ScrollView _content;
        ScrollView _rightPanel;
        VisualElement _modalLayer;
        VisualElement _resourceRow;
        VisualElement _bannerIcon;
        Label _factionLabel;
        Label _dateLabel;
        Label _objectiveMini;
        Label _statusLabel;
        Button _nextTurnButton;

        GameManager Gm => GameManager.Instance;
        GameDatabase Database => Gm.Database;

        ScenarioDef _pickedScenario;
        ScenarioDef.PlayableCharacter _pickedCharacter;

        string _topGroup = "영지";
        string _subTab = "개요";
        string _selectedOwnProvinceId;
        string _selectedHojokProvinceId;
        string _deploySourceId;

        static readonly (string group, string[] subs)[] TabGroups =
        {
            ("영지", new[] { "개요", "경제·농업" }),
            ("호족·신하", new[] { "호족 판도", "신하" }),
            ("군사", new[] { "병영·징집", "출진" }),
            ("연대기", new[] { "연대기" }),
        };

        static string[] SubTabsOf(string group) => TabGroups.First(t => t.group == group).subs;

        struct ModalButton
        {
            public string label;
            public string sub;
            public bool enabled;
            public Action onClick;

            public ModalButton(string label, string sub, bool enabled, Action onClick)
            {
                this.label = label;
                this.sub = sub;
                this.enabled = enabled;
                this.onClick = onClick;
            }
        }

        // Start (아닌 OnEnable)에서 지어야 GameManager.Awake()가 먼저 끝나 있음을 보장할 수 있다.
        void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _root.Clear();
            if (styleSheet != null) _root.styleSheets.Add(styleSheet);
            _root.AddToClassList("root");
            _root.style.flexGrow = 1;

            _scenarioScreen = new VisualElement();
            _scenarioScreen.AddToClassList("scenario-screen");
            _root.Add(_scenarioScreen);
            BuildMainShell();
            _modalLayer = new VisualElement();
            _modalLayer.AddToClassList("modal-layer");
            _modalLayer.AddToClassList("hidden");
            _root.Add(_modalLayer);

            ShowScenarioScreen();
        }

        // ───────── 시나리오·인물 선택 (§3.18) ─────────

        void ShowScenarioScreen()
        {
            HideModal();
            RefreshScenarioScreen();
            _scenarioScreen.RemoveFromClassList("hidden");
            _mainShell.AddToClassList("hidden");
        }

        void RefreshScenarioScreen()
        {
            _scenarioScreen.Clear();
            if (_pickedScenario == null)
            {
                _pickedScenario = Database.scenarios.FirstOrDefault();
                _pickedCharacter = _pickedScenario != null ? _pickedScenario.playableCharacters.FirstOrDefault() : null;
            }

            var header = new VisualElement();
            header.AddToClassList("title-header");
            header.Add(MakeLabel("후삼국 대란", "game-title"));
            header.Add(MakeLabel("천하통일 — 난세의 이름 없는 호족이 되어, 사람을 얻고 땅을 넓혀라", "game-subtitle"));
            _scenarioScreen.Add(header);

            var body = new VisualElement();
            body.AddToClassList("scenario-body");

            var left = new VisualElement();
            left.AddToClassList("scenario-list");
            left.Add(MakeLabel("시대", "section-title"));
            foreach (var sc in Database.scenarios)
            {
                var item = new VisualElement();
                item.AddToClassList("scenario-item");
                if (sc == _pickedScenario) item.AddToClassList("scenario-item-selected");
                item.Add(MakeLabel($"{sc.year}년", "scenario-year"));
                item.Add(MakeLabel(sc.displayName, "card-title"));
                if (!string.IsNullOrEmpty(sc.description)) item.Add(MakeLabel(sc.description, "card-line"));
                var picked = sc;
                item.RegisterCallback<ClickEvent>(_ =>
                {
                    _pickedScenario = picked;
                    _pickedCharacter = picked.playableCharacters.FirstOrDefault();
                    RefreshScenarioScreen();
                });
                left.Add(item);
            }

            var right = new VisualElement();
            right.AddToClassList("character-list");
            right.Add(MakeLabel("누구로 시작하시겠습니까?", "section-title"));
            if (_pickedScenario != null)
            {
                foreach (var ch in _pickedScenario.playableCharacters)
                {
                    var card = new VisualElement();
                    card.AddToClassList("character-card");
                    if (ch == _pickedCharacter) card.AddToClassList("character-card-selected");
                    if (ch.retainer != null && ch.retainer.portrait != null) card.Add(MakeIcon(ch.retainer.portrait, 120));
                    var info = new VisualElement();
                    info.AddToClassList("character-info");
                    var r = ch.retainer;
                    info.Add(MakeLabel(r != null ? $"{r.displayName}  {r.hanjaName}" : "(이름 없음)", "character-name"));
                    info.Add(MakeLabel($"{(ch.startingFaction != null ? ch.startingFaction.displayName : "")}  ·  시작: {(ch.startingProvince != null ? ch.startingProvince.displayName : "")}", "card-line"));
                    if (!string.IsNullOrEmpty(ch.playstyle)) info.Add(MakeLabel(ch.playstyle, "character-playstyle"));
                    if (r != null) info.Add(MakeLabel($"통솔 {r.leadership}   무력 {r.might}   지력 {r.intellect}   정치 {r.politics}   매력 {r.charm}   덕망 {r.virtue}", "character-stats"));
                    if (!string.IsNullOrEmpty(ch.startingSituation)) info.Add(MakeLabel(ch.startingSituation, "card-line"));
                    card.Add(info);
                    var picked = ch;
                    card.RegisterCallback<ClickEvent>(_ => { _pickedCharacter = picked; RefreshScenarioScreen(); });
                    right.Add(card);
                }
            }

            var buttons = new VisualElement();
            buttons.AddToClassList("row");
            var start = new Button(() =>
            {
                if (_pickedScenario == null || _pickedCharacter == null) return;
                Gm.NewGame(_pickedScenario, _pickedCharacter);
                _topGroup = "영지";
                _subTab = "개요";
                _selectedOwnProvinceId = null;
                _selectedHojokProvinceId = null;
                _deploySourceId = null;
                ShowMainShell();
            }) { text = "이 인물로 시작하기" };
            start.AddToClassList("action-button");
            start.AddToClassList("primary-button");
            start.AddToClassList("big-button");
            buttons.Add(start);
            if (SaveSystem.HasSave())
            {
                var cont = new Button(() => { if (Gm.LoadGame()) ShowMainShell(); }) { text = "이어하기 (저장된 게임)" };
                cont.AddToClassList("action-button");
                cont.AddToClassList("big-button");
                buttons.Add(cont);
            }
            right.Add(buttons);

            body.Add(left);
            body.Add(right);
            _scenarioScreen.Add(body);
        }

        // ───────── 메인 셸 ─────────

        void BuildMainShell()
        {
            _mainShell = new VisualElement();
            _mainShell.AddToClassList("main-shell");

            var topBar = new VisualElement();
            topBar.AddToClassList("top-bar");
            _bannerIcon = new VisualElement();
            _bannerIcon.AddToClassList("banner-icon");
            _factionLabel = MakeLabel("", "faction-label");
            _resourceRow = new VisualElement();
            _resourceRow.AddToClassList("resource-row");
            _objectiveMini = MakeLabel("", "objective-mini");
            _dateLabel = MakeLabel("", "date-label");
            _nextTurnButton = new Button(OnNextTurn) { text = "다음 계절 ▶" };
            _nextTurnButton.AddToClassList("action-button");
            _nextTurnButton.AddToClassList("primary-button");
            _nextTurnButton.AddToClassList("next-turn-button");
            topBar.Add(_bannerIcon);
            topBar.Add(_factionLabel);
            topBar.Add(_resourceRow);
            topBar.Add(Spacer());
            topBar.Add(_objectiveMini);
            topBar.Add(_dateLabel);
            topBar.Add(_nextTurnButton);

            var bodyRow = new VisualElement();
            bodyRow.AddToClassList("body-row");
            _tabRail = new VisualElement();
            _tabRail.AddToClassList("tab-rail");
            _content = new ScrollView(ScrollViewMode.Vertical);
            _content.AddToClassList("content");
            _rightPanel = new ScrollView(ScrollViewMode.Vertical);
            _rightPanel.AddToClassList("alert-panel");
            bodyRow.Add(_tabRail);
            bodyRow.Add(_content);
            bodyRow.Add(_rightPanel);

            var statusBar = new VisualElement();
            statusBar.AddToClassList("status-bar");
            _statusLabel = MakeLabel("", "status-label");
            statusBar.Add(_statusLabel);

            _mainShell.Add(topBar);
            _mainShell.Add(bodyRow);
            _mainShell.Add(statusBar);
            _mainShell.AddToClassList("hidden");
            _root.Add(_mainShell);
        }

        void ShowMainShell()
        {
            _scenarioScreen.AddToClassList("hidden");
            _mainShell.RemoveFromClassList("hidden");
            RefreshAll();
            if (Gm.PendingEvent != null) ShowEventModal();
            else HideModal();
        }

        void RefreshAll()
        {
            if (!Gm.HasGame) return;
            RefreshTopBar();
            RefreshTabRail();
            RefreshContent();
            RefreshRightPanel();
            _statusLabel.text = !string.IsNullOrEmpty(Gm.LastMessage)
                ? Gm.LastMessage
                : "할 일을 마쳤으면 '다음 계절'을 누르십시오. 막히면 오른쪽 책사의 조언을 보십시오.";
        }

        void OnNextTurn()
        {
            if (Gm.PendingEvent != null) { ShowEventModal(); return; }
            Gm.AdvanceSeason();
            RefreshAll();
            if (Gm.PendingEvent != null) ShowEventModal();
        }

        void AfterAction()
        {
            RefreshAll();
            if (Gm.PendingEvent != null) ShowEventModal();
        }

        void Jump(string group, string subTab, string provinceId)
        {
            if (string.IsNullOrEmpty(group)) return;
            _topGroup = group;
            _subTab = string.IsNullOrEmpty(subTab) ? SubTabsOf(group)[0] : subTab;
            if (!string.IsNullOrEmpty(provinceId))
            {
                var p = Gm.GetProvince(provinceId);
                if (p != null && p.ownerFactionId == Gm.PlayerFactionId) { _selectedOwnProvinceId = provinceId; _deploySourceId = provinceId; }
                else _selectedHojokProvinceId = provinceId;
            }
            RefreshTabRail();
            RefreshContent();
        }

        void RefreshTopBar()
        {
            var f = Gm.PlayerFaction;
            var fdef = f != null ? Database.GetFaction(f.factionId) : null;
            if (fdef != null && fdef.banner != null)
            {
                _bannerIcon.style.backgroundImage = new StyleBackground(fdef.banner);
                _bannerIcon.style.display = DisplayStyle.Flex;
            }
            else _bannerIcon.style.display = DisplayStyle.None;

            var monarch = Gm.MonarchDef;
            _factionLabel.text = $"{(fdef != null ? fdef.displayName : "")}  ·  {(monarch != null ? monarch.displayName : "")}";

            _resourceRow.Clear();
            if (f != null)
            {
                _resourceRow.Add(MakeResourceTag(Database.grain, f.grain, Gm.ForecastGrain() - Gm.GrainUpkeep()));
                _resourceRow.Add(MakeResourceTag(Database.coin, f.coin, Gm.ForecastCoin()));
                _resourceRow.Add(MakeResourceTag(Database.prestige, f.prestige, 1));
                _resourceRow.Add(MakeLabel($"병력 {Gm.TotalTroops()}", "resource-label"));
            }
            _dateLabel.text = $"{Gm.Year}년 {GameManager.SeasonNames[Gm.Season]}";
            var obj = Gm.CurrentObjective;
            _objectiveMini.text = obj != null
                ? $"목표: {obj.title}  {Mathf.Min(Gm.ObjectiveProgress(obj), Gm.ObjectiveTarget(obj))}/{Gm.ObjectiveTarget(obj)}"
                : "모든 목표 달성";
            _nextTurnButton.text = Gm.PendingEvent != null ? "결정할 일이 있습니다 ▶" : "다음 계절 ▶";
        }

        void RefreshTabRail()
        {
            _tabRail.Clear();
            foreach (var (group, subs) in TabGroups)
            {
                var g = group;
                var first = subs[0];
                var btn = new Button(() => { _topGroup = g; _subTab = first; RefreshTabRail(); RefreshContent(); }) { text = group };
                btn.AddToClassList("tab-button");
                if (group == _topGroup) btn.AddToClassList("tab-button-selected");
                _tabRail.Add(btn);
            }
            _tabRail.Add(Spacer());
            var menu = new Button(() => { Gm.SaveGame(); ShowScenarioScreen(); }) { text = "처음 화면으로" };
            menu.AddToClassList("tab-button");
            menu.AddToClassList("tab-button-small");
            _tabRail.Add(menu);
        }

        void RefreshContent()
        {
            _content.Clear();
            var subs = SubTabsOf(_topGroup);
            if (!subs.Contains(_subTab)) _subTab = subs[0];

            if (subs.Length > 1)
            {
                var subRow = new VisualElement();
                subRow.AddToClassList("subtab-row");
                foreach (var st in subs)
                {
                    var s = st;
                    var b = new Button(() => { _subTab = s; RefreshContent(); }) { text = st };
                    b.AddToClassList("subtab-button");
                    if (st == _subTab) b.AddToClassList("subtab-button-selected");
                    subRow.Add(b);
                }
                _content.Add(subRow);
            }

            switch (_subTab)
            {
                case "개요": BuildOverviewTab(); break;
                case "경제·농업": BuildEconomyTab(); break;
                case "호족 판도": BuildHojokTab(); break;
                case "신하": BuildRetainerTab(); break;
                case "병영·징집": BuildRecruitTab(); break;
                case "출진": BuildDeployTab(); break;
                case "연대기": BuildChronicleTab(); break;
            }
        }

        // ───────── 우측: 책사 조언(§3.21) + 계절 보고 ─────────

        void RefreshRightPanel()
        {
            _rightPanel.Clear();

            var advisorCard = new VisualElement();
            advisorCard.AddToClassList("advisor-card");
            var head = new VisualElement();
            head.AddToClassList("row");
            var advisorDef = Database.GetRetainer("choieung");
            if (advisorDef != null && advisorDef.portrait != null) head.Add(MakeIcon(advisorDef.portrait, 56));
            var who = new VisualElement();
            who.Add(MakeLabel("책사 최응", "advisor-name"));
            who.Add(MakeLabel("주군, 아뢰옵니다.", "card-hint"));
            head.Add(who);
            advisorCard.Add(head);
            foreach (var a in Advisor.Get(Gm))
            {
                var item = new VisualElement();
                item.AddToClassList("advice-item");
                if (a.urgent) item.AddToClassList("advice-urgent");
                item.Add(MakeLabel(a.line, "advice-line"));
                if (!string.IsNullOrEmpty(a.button))
                {
                    var adv = a;
                    item.Add(SmallButton($"{a.button} ▶", () => Jump(adv.group, adv.subTab, adv.provinceId)));
                }
                advisorCard.Add(item);
            }
            _rightPanel.Add(advisorCard);

            var current = Gm.Log.Where(e => e.turn == Gm.TurnNumber).Reverse().ToList();
            if (current.Count > 0)
            {
                _rightPanel.Add(MakeLabel("이번 계절", "report-title"));
                foreach (var e in current) _rightPanel.Add(LogLine(e));
            }
            var last = Gm.Log.Where(e => e.turn == Gm.TurnNumber - 1).Reverse().ToList();
            if (last.Count > 0)
            {
                var first = last[last.Count - 1];
                _rightPanel.Add(MakeLabel($"지난 계절 결산 ({first.year}년 {GameManager.SeasonNames[first.season % 4]})", "report-title"));
                foreach (var e in last) _rightPanel.Add(LogLine(e));
            }
        }

        static VisualElement LogLine(LogEntry e)
        {
            var l = MakeLabel(e.text, "log-line");
            l.AddToClassList(e.kind switch
            {
                LogKind.Good => "log-good",
                LogKind.Bad => "log-bad",
                LogKind.Event => "log-event",
                LogKind.Goal => "log-goal",
                _ => "log-info",
            });
            return l;
        }

        // ───────── 영지 개요 (§3.1) — 목표 + 판도 지도(§3.0) + 영지 카드 ─────────

        void BuildOverviewTab()
        {
            _content.Add(BuildObjectiveCard());

            var mapPanel = Panel("완산주 일대 판도",
                "고을을 누르면 해당 화면으로 갑니다. 붉은색이 우리 땅, 초록에 가까울수록 우리에게 마음이 기운 호족, 금색 테두리는 손이 닿는(맞닿은) 고을입니다.");
            mapPanel.Add(BuildMap(900f, 430f));
            _content.Add(mapPanel);

            var owned = Gm.OwnedProvinces.ToList();
            var ownPanel = Panel($"우리 영지 ({owned.Count}곳)", null);
            var grid = new VisualElement();
            grid.AddToClassList("card-grid");
            foreach (var p in owned) grid.Add(BuildOwnedProvinceCard(p));
            ownPanel.Add(grid);
            _content.Add(ownPanel);
        }

        VisualElement BuildObjectiveCard()
        {
            var card = new VisualElement();
            card.AddToClassList("objective-card");
            var obj = Gm.CurrentObjective;
            if (obj == null)
            {
                card.Add(MakeLabel("모든 목표를 이루었습니다", "objective-title"));
                card.Add(MakeLabel("완산주 일대는 이제 주군의 것입니다. 다음 장(후백제·신라·고려 캠페인)은 다음 버전에서 이어집니다.", "objective-desc"));
                return card;
            }
            int target = Gm.ObjectiveTarget(obj);
            int prog = Mathf.Min(Gm.ObjectiveProgress(obj), target);
            card.Add(MakeLabel($"현재 목표   (이룬 목표 {Gm.ObjectiveIndex} / {ObjectiveBook.All.Count})", "objective-kicker"));
            card.Add(MakeLabel(obj.title, "objective-title"));
            card.Add(MakeLabel(obj.description, "objective-desc"));
            var row = new VisualElement();
            row.AddToClassList("row");
            row.Add(MakeBar(prog, target, 320f, new Color(0.86f, 0.64f, 0.24f)));
            row.Add(MakeLabel($"{prog} / {target}", "objective-progress"));
            row.Add(MakeLabel($"보상: {obj.rewardText}", "objective-reward"));
            if (!string.IsNullOrEmpty(obj.jumpGroup))
            {
                var o = obj;
                row.Add(SmallButton("바로가기 ▶", () => Jump(o.jumpGroup, o.jumpSubTab, null)));
            }
            card.Add(row);
            return card;
        }

        VisualElement BuildMap(float width, float height)
        {
            var map = new VisualElement();
            map.AddToClassList("map");
            map.style.width = width;
            map.style.height = height;

            var defs = Gm.Provinces.Select(p => Gm.Def(p)).Where(d => d != null).ToList();
            if (defs.Count == 0) return map;
            float minX = defs.Min(d => d.mapPosition.x), maxX = defs.Max(d => d.mapPosition.x);
            float minY = defs.Min(d => d.mapPosition.y), maxY = defs.Max(d => d.mapPosition.y);
            const float nodeW = 150f, nodeH = 66f;

            Vector2 ToPx(Vector2 pos)
            {
                float nx = maxX - minX < 0.01f ? 0.5f : (pos.x - minX) / (maxX - minX);
                float ny = maxY - minY < 0.01f ? 0.5f : (pos.y - minY) / (maxY - minY);
                return new Vector2(nodeW / 2f + 12f + nx * (width - nodeW - 24f), nodeH / 2f + 12f + (1f - ny) * (height - nodeH - 24f));
            }

            var edges = new List<(Vector2 a, Vector2 b)>();
            var seen = new HashSet<string>();
            foreach (var d in defs)
            {
                foreach (var nid in d.adjacentProvinceIds)
                {
                    var nd = Database.GetProvince(nid);
                    if (nd == null) continue;
                    string key = string.CompareOrdinal(d.id, nid) < 0 ? d.id + "|" + nid : nid + "|" + d.id;
                    if (!seen.Add(key)) continue;
                    edges.Add((ToPx(d.mapPosition), ToPx(nd.mapPosition)));
                }
            }
            map.generateVisualContent += ctx =>
            {
                var painter = ctx.painter2D;
                painter.strokeColor = new Color(0.62f, 0.48f, 0.28f, 0.75f);
                painter.lineWidth = 3f;
                foreach (var (a, b) in edges)
                {
                    painter.BeginPath();
                    painter.MoveTo(a);
                    painter.LineTo(b);
                    painter.Stroke();
                }
            };

            foreach (var p in Gm.Provinces.ToList())
            {
                var d = Gm.Def(p);
                if (d == null) continue;
                var pos = ToPx(d.mapPosition);
                bool mine = p.ownerFactionId == Gm.PlayerFactionId;
                bool stronghold = Gm.IsStronghold(p);

                var node = new VisualElement();
                node.AddToClassList("map-node");
                if (mine) node.AddToClassList("map-node-mine");
                else if (!stronghold && Gm.IsAdjacentToPlayer(p)) node.AddToClassList("map-node-border");
                node.style.left = pos.x - nodeW / 2f;
                node.style.top = pos.y - nodeH / 2f;
                node.style.width = nodeW;
                node.style.height = nodeH;
                node.style.backgroundColor = NodeColor(p, mine, stronghold);

                var top = new VisualElement();
                top.AddToClassList("map-node-row");
                var tierSprite = Gm.Tier(p) != null ? Gm.Tier(p).settlementSprite : null;
                if (tierSprite != null) top.Add(MakeIcon(tierSprite, 28));
                top.Add(MakeLabel($"{d.displayName} ({Gm.TierName(p)})", "map-node-name"));
                node.Add(top);
                string sub = mine ? $"우리 땅 · 병력 {Gm.GarrisonTotal(p)}"
                    : stronghold ? $"후백제 · 병력 {Gm.GarrisonTotal(p)}"
                    : $"충성 {p.loyalty} · 사병 {Gm.GarrisonTotal(p)}";
                node.Add(MakeLabel(sub, "map-node-sub"));

                string id = p.provinceId;
                node.RegisterCallback<ClickEvent>(_ => OnMapNodeClicked(id));
                map.Add(node);
            }
            return map;
        }

        Color NodeColor(ProvinceState p, bool mine, bool stronghold)
        {
            if (mine) return new Color(0.56f, 0.16f, 0.12f);
            if (stronghold) return new Color(0.2f, 0.07f, 0.1f);
            float t = Mathf.Clamp01(p.loyalty / (float)GameManager.SubmissionLoyalty);
            return Color.Lerp(new Color(0.33f, 0.2f, 0.14f), new Color(0.17f, 0.4f, 0.22f), t);
        }

        void OnMapNodeClicked(string provinceId)
        {
            var p = Gm.GetProvince(provinceId);
            if (p == null) return;
            if (p.ownerFactionId == Gm.PlayerFactionId) Jump("영지", "경제·농업", provinceId);
            else if (Gm.IsStronghold(p)) Jump("군사", "출진", provinceId);
            else Jump("호족·신하", "호족 판도", provinceId);
        }

        VisualElement BuildOwnedProvinceCard(ProvinceState p)
        {
            var card = new VisualElement();
            card.AddToClassList("card");
            var tier = Gm.Tier(p);
            var head = new VisualElement();
            head.AddToClassList("row");
            if (tier != null && tier.settlementSprite != null) head.Add(MakeIcon(tier.settlementSprite, 72));
            var info = new VisualElement();
            info.AddToClassList("card-info");
            info.Add(MakeLabel($"{Gm.ProvinceName(p.provinceId)}  {tier?.displayName}({tier?.hanja})" + (p.provinceId == Gm.CapitalId ? "  · 본거지" : ""), "card-title"));
            info.Add(MakeLabel($"병력 {Gm.GarrisonTotal(p)}  ·  농업 Lv.{p.agricultureLevel}  ·  저수지 Lv.{p.irrigationLevel}  ·  세율 {Pct(p.taxRate)}", "card-line"));
            info.Add(MakeLabel($"이번 계절 예상: 석 +{Gm.GrainYield(p)}, 엽전 +{Gm.CoinYield(p)}", "card-line"));
            head.Add(info);
            card.Add(head);
            card.Add(LoyaltyRow(p.loyalty, $"(목표 {Gm.LoyaltyTarget(p)})"));
            var next = tier != null ? Database.NextTier(tier) : null;
            if (next != null)
                card.Add(MakeLabel($"다음 등급 '{next.displayName}'까지 — 누적 세수 {Mathf.Min(p.cumulativeEconomy, tier.economyThreshold)}/{tier.economyThreshold}, 주둔 병력 {Gm.GarrisonTotal(p)}/{tier.militaryThreshold}", "card-hint"));
            bool building = !string.IsNullOrEmpty(p.constructionId);
            card.Add(MakeLabel(building ? $"공사 중: {ConstructionName(p.constructionId)} ({p.constructionSeasonsLeft}계절 남음)" : "진행 중인 공사 없음 — 일손이 놀고 있습니다", building ? "card-good" : "card-hint"));
            var id = p.provinceId;
            card.Add(SmallButton("내정하기 ▶", () => Jump("영지", "경제·농업", id)));
            return card;
        }

        // ───────── 경제·농업 (§3.6) ─────────

        void BuildEconomyTab()
        {
            var owned = Gm.OwnedProvinces.ToList();
            if (owned.Count == 0) { _content.Add(MakeLabel("보유한 지역이 없습니다.", "panel-desc")); return; }
            if (owned.All(x => x.provinceId != _selectedOwnProvinceId)) _selectedOwnProvinceId = owned[0].provinceId;
            _content.Add(ProvincePicker(owned, _selectedOwnProvinceId, id => { _selectedOwnProvinceId = id; RefreshContent(); }));

            var p = Gm.GetProvince(_selectedOwnProvinceId);
            var f = Gm.PlayerFaction;
            string name = Gm.ProvinceName(p.provinceId);

            var yieldPanel = Panel($"{name}의 살림", "가을엔 수확이 1.6배, 겨울엔 절반입니다. 고을 등급이 하나 오를 때마다 수확이 15% 늘어납니다.");
            yieldPanel.Add(MakeLabel($"이번 계절 예상 수확:  석 +{Gm.GrainYield(p)}    엽전 +{Gm.CoinYield(p)}", "big-line"));
            int net = Gm.ForecastGrain() - Gm.GrainUpkeep();
            yieldPanel.Add(MakeLabel($"영지 전체 — 수확 석 +{Gm.ForecastGrain()}, 군량 소모 석 -{Gm.GrainUpkeep()}  →  다음 계절 순증감 {Signed(net)}", net < 0 ? "card-bad" : "card-line"));
            _content.Add(yieldPanel);

            var build = Panel("공사", "공사는 두 계절이 걸리고, 한 고을에서 한 번에 하나씩만 할 수 있습니다.");
            bool busy = !string.IsNullOrEmpty(p.constructionId);
            if (busy) build.Add(MakeLabel($"공사 중: {ConstructionName(p.constructionId)} — {p.constructionSeasonsLeft}계절 남음", "card-good"));
            var row = new VisualElement();
            row.AddToClassList("card-grid");
            int agriCost = Gm.ConstructionCost(p, "agriculture");
            int irriCost = Gm.ConstructionCost(p, "irrigation");
            string pid = p.provinceId;
            row.Add(ActionCard("논 개간", $"농업 Lv.{p.agricultureLevel} → {p.agricultureLevel + 1}", "계절 수확 +8 (등급·계절 보정 전)",
                $"착공 (석 {agriCost})", !busy && f.grain >= agriCost, () => { Gm.TryUpgradeAgriculture(pid); AfterAction(); }));
            row.Add(ActionCard("저수지 정비", $"저수지 Lv.{p.irrigationLevel} → {p.irrigationLevel + 1}", "계절 수확 +5",
                $"착공 (석 {irriCost})", !busy && f.grain >= irriCost, () => { Gm.TryUpgradeIrrigation(pid); AfterAction(); }));
            build.Add(row);
            _content.Add(build);

            var market = Panel("장터", "남는 엽전으로 곡식을 사들이거나, 남는 곡식을 내다 팔 수 있습니다. 파는 값은 사는 값의 절반입니다.");
            var marketRow = new VisualElement();
            marketRow.AddToClassList("row-wrap");
            int lot = GameManager.TradeLot;
            var buy = SmallButton($"곡식 사들이기 (엽전 {lot} → 석 {lot})", () => { Gm.TryTrade(true); RefreshAll(); });
            buy.SetEnabled(f.coin >= lot);
            marketRow.Add(buy);
            var buy5 = SmallButton($"×5 (엽전 {lot * 5} → 석 {lot * 5})", () => { for (int i = 0; i < 5; i++) Gm.TryTrade(true); RefreshAll(); });
            buy5.SetEnabled(f.coin >= lot * 5);
            marketRow.Add(buy5);
            var sell = SmallButton($"곡식 내다 팔기 (석 {lot} → 엽전 {lot / 2})", () => { Gm.TryTrade(false); RefreshAll(); });
            sell.SetEnabled(f.grain >= lot);
            marketRow.Add(sell);
            market.Add(marketRow);
            _content.Add(market);

            var tax = Panel("세율", "세율을 올리면 엽전이 더 걷히지만 민심이 떨어집니다. 충성은 계절마다 '목표 충성' 쪽으로 3씩 움직이고, 25 아래면 민란이 납니다.");
            var taxRow = new VisualElement();
            taxRow.AddToClassList("row");
            taxRow.Add(SmallButton("◀ 5% 내리기", () => { Gm.SetTaxRate(pid, Gm.GetProvince(pid).taxRate - 0.05f); RefreshAll(); }));
            taxRow.Add(MakeLabel($"세율 {Pct(p.taxRate)}", "big-line"));
            taxRow.Add(SmallButton("5% 올리기 ▶", () => { Gm.SetTaxRate(pid, Gm.GetProvince(pid).taxRate + 0.05f); RefreshAll(); }));
            tax.Add(taxRow);
            tax.Add(MakeLabel($"→ 엽전 계절당 +{Gm.CoinYield(p)},  목표 충성 {Gm.LoyaltyTarget(p)}", "card-line"));
            tax.Add(LoyaltyRow(p.loyalty, $"(목표 {Gm.LoyaltyTarget(p)})"));
            _content.Add(tax);
        }

        // ───────── 호족 판도 (§3.2) ─────────

        void BuildHojokTab()
        {
            var list = Gm.Provinces.Where(p => p.ownerFactionId != Gm.PlayerFactionId)
                .OrderByDescending(p => Gm.IsAdjacentToPlayer(p))
                .ThenBy(p => Gm.IsStronghold(p))
                .ThenByDescending(p => p.loyalty)
                .ToList();
            _content.Add(Panel("호족 판도",
                $"충성이 {GameManager.SubmissionLoyalty} 이상인 이웃 호족은 계절이 바뀔 때 스스로 귀부하고, 그들의 사병도 함께 들어옵니다. " +
                "회유는 엽전, 사성(성씨 하사)은 권위를 씁니다. 토벌은 빠르지만 칼로 얻은 땅은 민심이 차갑고, 다른 호족들이 겁을 먹고 등을 돌립니다(이웃 충성 -10)."));
            if (list.Count == 0) { _content.Add(MakeLabel("완산주 일대를 모두 평정했습니다!", "big-line")); return; }
            var grid = new VisualElement();
            grid.AddToClassList("card-grid");
            foreach (var p in list) grid.Add(BuildHojokCard(p));
            _content.Add(grid);
        }

        VisualElement BuildHojokCard(ProvinceState p)
        {
            var card = new VisualElement();
            card.AddToClassList("card");
            if (p.provinceId == _selectedHojokProvinceId) card.AddToClassList("card-selected");
            bool stronghold = Gm.IsStronghold(p);
            bool adjacent = Gm.IsAdjacentToPlayer(p);
            var f = Gm.PlayerFaction;
            var tier = Gm.Tier(p);

            var head = new VisualElement();
            head.AddToClassList("row");
            if (tier != null && tier.settlementSprite != null) head.Add(MakeIcon(tier.settlementSprite, 64));
            var info = new VisualElement();
            info.AddToClassList("card-info");
            info.Add(MakeLabel($"{Gm.ProvinceName(p.provinceId)}  {tier?.displayName}", "card-title"));
            info.Add(MakeLabel(Gm.HojokName(p), "card-line"));
            info.Add(MakeLabel(adjacent ? "우리 땅과 맞닿음 — 손이 닿는다" : "멀리 있음 — 맞닿은 고을부터 얻어야 함", adjacent ? "card-good" : "card-hint"));
            head.Add(info);
            card.Add(head);

            if (stronghold) card.Add(MakeLabel("견훤의 직할지 — 회유가 통하지 않는다. 칼로 빼앗을 수밖에 없다.", "card-bad"));
            else
            {
                card.Add(LoyaltyRow(p.loyalty, $"/ 귀부 {GameManager.SubmissionLoyalty}"));
                card.Add(MakeLabel(Attitude(p.loyalty), "card-hint"));
            }
            card.Add(MakeLabel($"사병 {Gm.GarrisonTotal(p)}명 — {Composition(p.garrison)}  · 성곽 ×{Gm.WallMultiplier(p):0.##}", "card-line"));

            var army = PlayerArmy();
            float ratio = PowerRatio(army, p);
            if (army.Sum(u => u.count) > 0) card.Add(MakeLabel($"우리 전군({army.Sum(u => u.count)}명)으로 치면: {OddsText(ratio)}", ratio >= 1.3f ? "card-good" : ratio >= 0.95f ? "card-line" : "card-bad"));

            var buttons = new VisualElement();
            buttons.AddToClassList("row-wrap");
            string pid = p.provinceId;
            string pname = Gm.ProvinceName(pid);
            int cost = Gm.AppeaseCost();
            var appease = SmallButton($"회유 (엽전 {cost}, 충성 +{Gm.AppeaseGain()})", () => { Gm.TryAppeaseHojok(pid); _selectedHojokProvinceId = pid; AfterAction(); });
            appease.SetEnabled(!stronghold && adjacent && f.coin >= cost);
            buttons.Add(appease);
            int pcost = Gm.SurnameCost();
            var surname = SmallButton($"사성 (권위 {pcost}, 충성 +{Database.grantSurnameLoyaltyGain})", () => { Gm.TryGrantSurname(pid); _selectedHojokProvinceId = pid; AfterAction(); });
            surname.SetEnabled(!stronghold && adjacent && f.prestige >= pcost);
            buttons.Add(surname);
            var purge = SmallButton("토벌 (전군 동원)", () => ConfirmAttack(ratio, pname, () =>
            {
                Gm.TryPurge(pid, out var report);
                ShowBattleModal(report, pname);
            }));
            purge.SetEnabled(adjacent && army.Sum(u => u.count) > 0);
            buttons.Add(purge);
            card.Add(buttons);
            return card;
        }

        // ───────── 신하 ─────────

        void BuildRetainerTab()
        {
            var ret = Gm.Retainers.Where(r => r.factionId == Gm.PlayerFactionId).ToList();
            var monarch = Gm.MonarchDef;
            _content.Add(Panel("가문의 사람들",
                $"군주의 매력은 호족 회유의 효과(현재 충성 +{Gm.AppeaseGain()})에, 통솔은 전투력(×{1f + ((monarch != null ? monarch.leadership : 50) - 50) / 100f:0.00})에 반영됩니다. " +
                "인재를 더 모으면 주요 관직 임명과 후계 문제가 열립니다."));
            var grid = new VisualElement();
            grid.AddToClassList("card-grid");
            foreach (var r in ret)
            {
                var def = Gm.Def(r);
                if (def == null) continue;
                var card = new VisualElement();
                card.AddToClassList("card");
                var head = new VisualElement();
                head.AddToClassList("row");
                if (def.portrait != null) head.Add(MakeIcon(def.portrait, 96));
                var info = new VisualElement();
                info.AddToClassList("card-info");
                bool isMonarch = Gm.PlayerFaction.monarchRetainerId == r.retainerId;
                info.Add(MakeLabel($"{def.displayName}  {def.hanjaName}", "card-title"));
                info.Add(MakeLabel(isMonarch ? $"가주 · {r.age}세" : $"책사 · {r.age}세 · 충성 {r.loyalty}", "card-line"));
                info.Add(MakeLabel($"통솔 {def.leadership}  무력 {def.might}  지력 {def.intellect}  학문 {def.scholarship}", "card-line"));
                info.Add(MakeLabel($"정치 {def.politics}  매력 {def.charm}  덕망 {def.virtue}", "card-line"));
                head.Add(info);
                card.Add(head);
                grid.Add(card);
            }
            _content.Add(grid);
        }

        // ───────── 병영·징집 (§3.9) ─────────

        void BuildRecruitTab()
        {
            var owned = Gm.OwnedProvinces.ToList();
            if (owned.Count == 0) { _content.Add(MakeLabel("보유한 지역이 없습니다.", "panel-desc")); return; }
            if (owned.All(x => x.provinceId != _selectedOwnProvinceId)) _selectedOwnProvinceId = owned[0].provinceId;
            _content.Add(ProvincePicker(owned, _selectedOwnProvinceId, id => { _selectedOwnProvinceId = id; RefreshContent(); }));

            var p = Gm.GetProvince(_selectedOwnProvinceId);
            string pid = p.provinceId;
            int cap = Gm.RecruitCap(p);
            int left = Mathf.Max(0, cap - p.recruitedThisSeason);
            int net = Gm.ForecastGrain() - Gm.GrainUpkeep();

            var garrison = Panel($"{Gm.ProvinceName(pid)} 주둔군",
                $"한 계절에 고을 크기만큼(이 고을 지금 {cap}명) 장정을 모을 수 있습니다. 충성이 50 미만이면 절반, 35 미만이면 아무도 모이지 않습니다. 병사는 계절마다 군량을 먹으니 곳간 사정을 보며 모으십시오.");
            garrison.Add(GarrisonRow(p.garrison));
            garrison.Add(MakeLabel($"이번 계절 더 모을 수 있는 인원: {left}명", "big-line"));
            garrison.Add(MakeLabel($"전체 병력 {Gm.TotalTroops()}명 · 계절당 군량 석 {Gm.GrainUpkeep()} · 다음 계절 곡식 순증감 {Signed(net)}", net < 0 ? "card-bad" : "card-line"));
            _content.Add(garrison);

            var grid = new VisualElement();
            grid.AddToClassList("card-grid");
            var f = Gm.PlayerFaction;
            foreach (var unit in Database.units)
            {
                var card = new VisualElement();
                card.AddToClassList("card");
                var head = new VisualElement();
                head.AddToClassList("row");
                if (unit.icon != null) head.Add(MakeIcon(unit.icon, 64));
                var info = new VisualElement();
                info.AddToClassList("card-info");
                info.Add(MakeLabel(unit.displayName, "card-title"));
                info.Add(MakeLabel(UnitClassText(unit.unitClass), "card-line"));
                info.Add(MakeLabel($"전투력 {unit.power} · 계절 군량 {unit.foodUpkeepPerSoldier / 3f:0.#}석/명", "card-hint"));
                head.Add(info);
                card.Add(head);
                var row = new VisualElement();
                row.AddToClassList("row-wrap");
                foreach (int n in new[] { 5, 10 })
                {
                    Gm.RecruitCost(unit, n, out int g, out int c);
                    string costText = c > 0 ? $"석 {g}, 엽전 {c}" : $"석 {g}";
                    var u = unit;
                    int count = n;
                    var b = SmallButton($"{n}명 모집 ({costText})", () => { Gm.TryRecruit(pid, u.id, count); AfterAction(); });
                    b.SetEnabled(left >= n && f.grain >= g && f.coin >= c);
                    row.Add(b);
                }
                card.Add(row);
                grid.Add(card);
            }
            _content.Add(grid);
        }

        // ───────── 출진 (§3.10, §3.11) ─────────

        void BuildDeployTab()
        {
            var owned = Gm.OwnedProvinces.ToList();
            if (owned.Count == 0) { _content.Add(MakeLabel("보유한 지역이 없습니다.", "panel-desc")); return; }
            if (owned.All(x => x.provinceId != _deploySourceId))
            {
                // 지도에서 적 고을을 눌러 왔으면 그 고을과 맞닿은 우리 고을 중 병력이 가장 많은 곳을 고른다
                var target = Gm.GetProvince(_selectedHojokProvinceId);
                var near = target != null ? Gm.AdjacentTo(target).Where(x => x.ownerFactionId == Gm.PlayerFactionId).OrderByDescending(x => Gm.GarrisonTotal(x)).FirstOrDefault() : null;
                _deploySourceId = (near ?? owned.OrderByDescending(x => Gm.GarrisonTotal(x)).First()).provinceId;
            }
            _content.Add(ProvincePicker(owned, _deploySourceId, id => { _deploySourceId = id; RefreshContent(); }));

            var src = Gm.GetProvince(_deploySourceId);
            string srcName = Gm.ProvinceName(src.provinceId);
            var srcPanel = Panel($"{srcName}에서 출진",
                "한 고을의 병력을 이끌고 맞닿은 고을을 칩니다. 이기면 살아남은 병사가 그곳에 주둔하고, 지면 돌아옵니다. 원정 군량(1인당 석)이 듭니다. 창병은 기병을, 기병은 궁병을, 궁병은 창병을 이깁니다 — 적의 구성을 보고 병종을 맞추십시오. 큰 고을일수록 성곽이 높아 공략이 어렵습니다.");
            srcPanel.Add(GarrisonRow(src.garrison));
            _content.Add(srcPanel);

            var targets = Gm.AdjacentTo(src).Where(n => n.ownerFactionId != Gm.PlayerFactionId).ToList();
            if (targets.Count == 0)
            {
                _content.Add(MakeLabel($"{Ko.Wa(srcName)} 맞닿은 적 고을이 없습니다. 위에서 다른 고을을 고르십시오.", "card-hint"));
                return;
            }
            var grid = new VisualElement();
            grid.AddToClassList("card-grid");
            var f = Gm.PlayerFaction;
            foreach (var t in targets)
            {
                var card = new VisualElement();
                card.AddToClassList("card");
                if (t.provinceId == _selectedHojokProvinceId) card.AddToClassList("card-selected");
                string tname = Gm.ProvinceName(t.provinceId);
                card.Add(MakeLabel($"→ {tname}  ({Gm.HojokName(t)})", "card-title"));
                card.Add(MakeLabel($"적 {Gm.GarrisonTotal(t)}명 — {Composition(t.garrison)}  · 성곽 ×{Gm.WallMultiplier(t):0.##}", "card-line"));

                var all = src.garrison.Select(u => new UnitCount { unitId = u.unitId, count = u.count }).ToList();
                var half = src.garrison.Select(u => new UnitCount { unitId = u.unitId, count = u.count / 2 }).Where(u => u.count > 0).ToList();
                foreach (var (label, army) in new[] { ("전군 출진", all), ("절반 출진", half) })
                {
                    int n = army.Sum(u => u.count);
                    if (n <= 0) continue;
                    float ratio = PowerRatio(army, t);
                    int food = Gm.MarchFoodCost(army);
                    card.Add(MakeLabel($"{label} {n}명: {OddsText(ratio)} · 군량 석 {food}", ratio >= 1.3f ? "card-good" : ratio >= 0.95f ? "card-line" : "card-bad"));
                    string srcId = src.provinceId, tid = t.provinceId;
                    var sent = army;
                    var b = SmallButton($"{label} ▶", () => ConfirmAttack(ratio, tname, () =>
                    {
                        var report = Gm.Deploy(srcId, tid, sent);
                        if (report == null) { RefreshAll(); return; }
                        ShowBattleModal(report, tname);
                    }));
                    b.SetEnabled(f.grain >= food);
                    card.Add(b);
                }
                grid.Add(card);
            }
            _content.Add(grid);
        }

        // ───────── 연대기 ─────────

        void BuildChronicleTab()
        {
            _content.Add(Panel("연대기", "이 가문이 걸어온 길. 그때의 선택이 지금의 판도를 만들었습니다."));
            foreach (var g in Gm.Log.GroupBy(e => e.turn).OrderByDescending(g => g.Key))
            {
                var first = g.First();
                var panel = new VisualElement();
                panel.AddToClassList("panel");
                panel.Add(MakeLabel($"{first.year}년 {GameManager.SeasonNames[first.season % 4]}", "panel-title"));
                foreach (var e in g) panel.Add(LogLine(e));
                _content.Add(panel);
            }
        }

        // ───────── 대화 창: 사건 · 전투 결과 · 확인 ─────────

        void ShowModal(string title, Sprite portrait, string speaker, string body, IEnumerable<ModalButton> buttons)
        {
            _modalLayer.Clear();
            var box = new VisualElement();
            box.AddToClassList("modal-box");
            box.Add(MakeLabel(title, "modal-title"));

            var mid = new VisualElement();
            mid.AddToClassList("modal-mid");
            if (portrait != null || !string.IsNullOrEmpty(speaker))
            {
                var who = new VisualElement();
                who.AddToClassList("modal-speaker");
                if (portrait != null) who.Add(MakeIcon(portrait, 128));
                if (!string.IsNullOrEmpty(speaker)) who.Add(MakeLabel(speaker, "modal-speaker-name"));
                mid.Add(who);
            }
            mid.Add(MakeLabel(body, "modal-body"));
            box.Add(mid);

            var choices = new VisualElement();
            choices.AddToClassList("modal-choices");
            foreach (var b in buttons)
            {
                var btn = new Button(b.onClick);
                btn.AddToClassList("choice-button");
                btn.Add(MakeLabel(b.label, "choice-text"));
                if (!string.IsNullOrEmpty(b.sub)) btn.Add(MakeLabel(b.sub, "choice-sub"));
                btn.SetEnabled(b.enabled);
                choices.Add(btn);
            }
            box.Add(choices);
            _modalLayer.Add(box);
            _modalLayer.RemoveFromClassList("hidden");
        }

        void HideModal()
        {
            if (_modalLayer == null) return;
            _modalLayer.AddToClassList("hidden");
            _modalLayer.Clear();
        }

        Sprite PortraitOf(string retainerId)
        {
            if (string.IsNullOrEmpty(retainerId)) return null;
            var def = Database.GetRetainer(retainerId);
            return def != null ? def.portrait : null;
        }

        void ShowEventModal()
        {
            var ev = Gm.PendingEvent;
            if (ev == null) { HideModal(); return; }
            string body;
            try { body = ev.body(Gm); }
            catch (Exception e) { Debug.LogException(e); body = ""; }

            var buttons = new List<ModalButton>();
            for (int i = 0; i < ev.choices.Count; i++)
            {
                int index = i;
                var ch = ev.choices[i];
                bool can = Gm.CanAfford(ch);
                buttons.Add(new ModalButton(ch.text, ch.CostText() + (can ? "" : "   — 자원이 모자랍니다"), can, () => OnChoice(ev, index)));
            }
            ShowModal(ev.title, PortraitOf(ev.speakerId), ev.speakerName, body, buttons);
        }

        void OnChoice(StoryEvent ev, int index)
        {
            string choiceText = ev.choices[index].text;
            string result = Gm.ResolveEvent(index);
            if (result == null) { ShowEventModal(); return; }
            ShowModal(ev.title, PortraitOf(ev.speakerId), ev.speakerName, $"▶ {choiceText}\n\n{result}",
                new[] { new ModalButton("계속", null, true, AfterModalResult) });
        }

        void AfterModalResult()
        {
            if (Gm.ConsumeFlag(GameManager.RestartFlag))
            {
                SaveSystem.Delete();
                _pickedScenario = null;
                ShowScenarioScreen();
                return;
            }
            RefreshAll();
            if (Gm.PendingEvent != null) ShowEventModal();
            else HideModal();
        }

        void ShowBattleModal(GameManager.BattleReport report, string targetName)
        {
            if (report == null) { RefreshAll(); return; }
            string body = report.victory
                ? $"함성과 함께 {targetName}의 성문이 부서졌습니다!\n\n아군 {report.attackerSent}명 중 {report.attackerLost}명을 잃고, 적 {report.defenderSent}명을 무찔렀습니다.\n\n" +
                  $"{Ko.Eun(targetName)} 이제 주군의 땅입니다. 다만 칼로 얻은 땅의 민심은 차갑고(충성 30), 이 소식을 들은 다른 호족들이 겁을 먹고 등을 돌렸습니다."
                : $"적의 저항이 거셉니다…\n\n아군 {report.attackerSent}명 중 {report.attackerLost}명을 잃고 물러났습니다. 적은 {report.defenderSent}명 중 {report.defenderLost}명을 잃었습니다.\n\n" +
                  "다음엔 병종 상성을 맞추거나 병력을 더 모아서 치십시오.";
            ShowModal(report.victory ? $"승전 — {targetName}" : $"패전 — {targetName}", PortraitOf("choieung"), "책사 최응", body,
                new[] { new ModalButton("계속", null, true, AfterModalResult) });
        }

        /// <summary>§3.21 무모한 선택 경고 — 전력이 밀리면 책사가 만류한다. 그래도 강행은 가능.</summary>
        void ConfirmAttack(float ratio, string targetName, Action proceed)
        {
            if (ratio >= 0.95f) { proceed(); return; }
            ShowModal("책사의 만류", PortraitOf("choieung"), "책사 최응",
                $"주군, 잠시만 기다리십시오! {targetName}의 병력이 우리보다 강합니다 (전력비 {ratio:0.00}).\n\n이대로 치면 공산 전투 꼴이 날 수 있습니다. 병종 상성을 맞추거나 병력을 더 모은 뒤에 치시지요.\n\n그래도 강행하시겠습니까?",
                new[]
                {
                    new ModalButton("그래도 친다", "결정은 주군의 몫", true, () => { HideModal(); proceed(); }),
                    new ModalButton("물러선다", null, true, HideModal),
                });
        }

        // ───────── 계산 헬퍼 ─────────

        List<UnitCount> PlayerArmy() => Gm.OwnedProvinces.SelectMany(x => x.garrison).GroupBy(u => u.unitId)
            .Select(g => new UnitCount { unitId = g.Key, count = g.Sum(u => u.count) }).Where(u => u.count > 0).ToList();

        float PowerRatio(List<UnitCount> army, ProvinceState target)
        {
            Gm.ComputePowers(Gm.PlayerFactionId, army, target.provinceId, target.garrison, out float atk, out float def);
            return def <= 0f ? 99f : atk / def;
        }

        static string OddsText(float ratio) =>
            ratio >= 1.3f ? "우세 — 이길 가능성이 높다"
            : ratio >= 0.95f ? "백중 — 운에 달렸다"
            : "열세 — 지기 쉽다";

        static string Attitude(int loyalty) =>
            loyalty >= GameManager.SubmissionLoyalty ? "귀부할 뜻을 굳혔다 — 다음 계절에 성문을 연다"
            : loyalty >= 60 ? "마음이 기울고 있다 — 조금만 더"
            : loyalty >= 40 ? "관망하고 있다"
            : loyalty >= 30 ? "경계하고 있다"
            : "적의를 품었다 — 우리 고을을 습격할 수 있다";

        string Composition(List<UnitCount> garrison)
        {
            if (garrison == null || garrison.Count == 0) return "없음";
            return string.Join(", ", garrison.Where(u => u.count > 0).Select(u => $"{UnitName(u.unitId)} {u.count}"));
        }

        string UnitName(string unitId)
        {
            var u = Database.GetUnit(unitId);
            return u != null ? u.displayName : unitId;
        }

        static string UnitClassText(UnitClass c) => c switch
        {
            UnitClass.Spear => "창병 — 기병을 이기고 궁병에 약하다",
            UnitClass.Cavalry => "기병 — 궁병을 이기고 창병에 약하다",
            UnitClass.Archer => "궁병 — 창병을 이기고 기병에 약하다",
            _ => "보병 — 상성이 없다. 싸고 든든하다",
        };

        static string ConstructionName(string id) => id == "agriculture" ? "논 개간" : id == "irrigation" ? "저수지 정비" : id;
        static string Pct(float rate) => $"{Mathf.RoundToInt(rate * 100f)}%";
        static string Signed(int v) => v >= 0 ? $"+{v}" : v.ToString();

        // ───────── UI 헬퍼 ─────────

        static Label MakeLabel(string text, string cls)
        {
            var l = new Label(text);
            if (!string.IsNullOrEmpty(cls)) l.AddToClassList(cls);
            return l;
        }

        static VisualElement Spacer()
        {
            var s = new VisualElement();
            s.AddToClassList("spacer");
            return s;
        }

        static VisualElement Panel(string title, string desc)
        {
            var p = new VisualElement();
            p.AddToClassList("panel");
            if (!string.IsNullOrEmpty(title)) p.Add(MakeLabel(title, "panel-title"));
            if (!string.IsNullOrEmpty(desc)) p.Add(MakeLabel(desc, "panel-desc"));
            return p;
        }

        static Button SmallButton(string text, Action onClick)
        {
            var b = new Button(onClick) { text = text };
            b.AddToClassList("action-button");
            return b;
        }

        static VisualElement ActionCard(string title, string line1, string line2, string buttonText, bool enabled, Action onClick)
        {
            var card = new VisualElement();
            card.AddToClassList("card");
            card.AddToClassList("card-small");
            card.Add(MakeLabel(title, "card-title"));
            card.Add(MakeLabel(line1, "card-line"));
            card.Add(MakeLabel(line2, "card-hint"));
            var b = SmallButton(buttonText, onClick);
            b.SetEnabled(enabled);
            card.Add(b);
            return card;
        }

        VisualElement ProvincePicker(List<ProvinceState> provinces, string selectedId, Action<string> onPick)
        {
            var row = new VisualElement();
            row.AddToClassList("row-wrap");
            row.Add(MakeLabel("고을:", "picker-label"));
            foreach (var p in provinces)
            {
                string id = p.provinceId;
                var b = new Button(() => onPick(id)) { text = $"{Gm.ProvinceName(id)} ({Gm.GarrisonTotal(p)})" };
                b.AddToClassList("subtab-button");
                if (id == selectedId) b.AddToClassList("subtab-button-selected");
                row.Add(b);
            }
            return row;
        }

        VisualElement GarrisonRow(List<UnitCount> garrison)
        {
            var row = new VisualElement();
            row.AddToClassList("row-wrap");
            if (garrison == null || garrison.Count == 0) { row.Add(MakeLabel("주둔 병력 없음", "card-bad")); return row; }
            foreach (var u in garrison.Where(x => x.count > 0))
            {
                var def = Database.GetUnit(u.unitId);
                var tag = new VisualElement();
                tag.AddToClassList("unit-tag");
                if (def != null && def.icon != null) tag.Add(MakeIcon(def.icon, 40));
                tag.Add(MakeLabel($"{(def != null ? def.displayName : u.unitId)} {u.count}", "card-title"));
                row.Add(tag);
            }
            return row;
        }

        static VisualElement LoyaltyRow(int loyalty, string suffix)
        {
            var row = new VisualElement();
            row.AddToClassList("row");
            row.Add(MakeLabel("충성", "bar-label"));
            var color = loyalty < 35 ? new Color(0.8f, 0.25f, 0.2f) : loyalty < 60 ? new Color(0.85f, 0.65f, 0.25f) : new Color(0.35f, 0.7f, 0.35f);
            row.Add(MakeBar(loyalty, 100, 180f, color));
            row.Add(MakeLabel($"{loyalty} {suffix}", "card-line"));
            return row;
        }

        static VisualElement MakeBar(int value, int max, float width, Color color)
        {
            var bg = new VisualElement();
            bg.AddToClassList("bar-bg");
            bg.style.width = width;
            var fill = new VisualElement();
            fill.AddToClassList("bar-fill");
            fill.style.width = Length.Percent(max <= 0 ? 0f : Mathf.Clamp01((float)value / max) * 100f);
            fill.style.backgroundColor = color;
            bg.Add(fill);
            return bg;
        }

        /// <summary>정사각형 스프라이트 아이콘 하나.</summary>
        static VisualElement MakeIcon(Sprite sprite, int size)
        {
            var el = new VisualElement();
            el.AddToClassList("icon");
            el.style.width = size;
            el.style.height = size;
            el.style.minWidth = size;
            el.style.backgroundImage = new StyleBackground(sprite);
            el.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            return el;
        }

        /// <summary>자원 아이콘 + 수치 + 다음 계절 예상 증감.</summary>
        static VisualElement MakeResourceTag(ResourceDef res, int amount, int delta)
        {
            var row = new VisualElement();
            row.AddToClassList("resource-tag");
            if (res != null && res.icon != null) row.Add(MakeIcon(res.icon, 26));
            row.Add(MakeLabel($"{(res != null ? res.displayName + " " : "")}{amount}", "resource-label"));
            row.Add(MakeLabel($"({Signed(delta)})", delta < 0 ? "resource-delta-bad" : "resource-delta"));
            return row;
        }
    }
}
