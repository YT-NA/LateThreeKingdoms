using System.Collections.Generic;
using System.Linq;
using Husamguk.Core;
using Husamguk.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Husamguk.UI
{
    /// <summary>
    /// 전체 UI 셸. GDD §3 — 시나리오 선택(§3.18) 다음 메인 셸(상단 바·좌측 탭·중앙 내용·우측 알림·하단 상태바).
    /// MVP 1차 구현은 영지 개요/경제·농업/호족 판도만 실제로 동작하고, 나머지 탭은 "준비 중" 표시.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GameUI : MonoBehaviour
    {
        [SerializeField] StyleSheet styleSheet;

        UIDocument _doc;
        VisualElement _root;
        VisualElement _scenarioScreen;
        VisualElement _mainShell;
        VisualElement _content;
        VisualElement _alertPanel;
        Label _topBarLabel;
        Label _resourceLabel;
        Label _dateLabel;
        Label _statusLabel;

        GameDatabase Database => GameManager.Instance.Database;

        ScenarioDef _pickedScenario;
        ScenarioDef.PlayableCharacter _pickedCharacter;

        string _topGroup = "영지";
        string _subTab = "개요";
        string _selectedOwnProvinceId;
        string _selectedHojokProvinceId;

        static readonly Dictionary<string, string[]> TabGroups = new()
        {
            { "영지", new[] { "개요", "경제·농업" } },
            { "호족·신하", new[] { "호족 판도", "주요 관직", "전체 신하", "왕위 계승" } },
            { "군사", new[] { "군사·사병", "병영·징집" } },
            { "외교", new[] { "외교·책략" } },
        };

        // Start (아닌 OnEnable)에서 지어야 GameManager.Awake()가 먼저 끝나 있음을 보장할 수 있다.
        void Start()
        {
            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;
            _root.Clear();
            if (styleSheet != null) _root.styleSheets.Add(styleSheet);
            _root.AddToClassList("root");
            _root.style.flexGrow = 1;

            BuildScenarioScreen();
            BuildMainShell();
            ShowScenarioScreen();
        }

        // ───────── 시나리오 선택 (§3.18) ─────────

        void BuildScenarioScreen()
        {
            _scenarioScreen = new VisualElement();
            _scenarioScreen.AddToClassList("scenario-screen");

            var scenarioList = new VisualElement();
            scenarioList.AddToClassList("scenario-list");
            var characterList = new VisualElement();
            characterList.AddToClassList("character-list");

            var startButton = new Button { text = "시작하기" };
            startButton.AddToClassList("action-button");
            startButton.AddToClassList("primary-button");
            startButton.clicked += () =>
            {
                if (_pickedScenario == null || _pickedCharacter == null) return;
                GameManager.Instance.NewGame(_pickedScenario, _pickedCharacter);
                ShowMainShell();
            };

            void RefreshCharacters()
            {
                characterList.Clear();
                characterList.Add(new Label(_pickedScenario.displayName) { style = { fontSize = 16, marginBottom = 8 } });
                foreach (var ch in _pickedScenario.playableCharacters)
                {
                    var card = new VisualElement();
                    card.AddToClassList("character-card");
                    if (ch == _pickedCharacter) card.AddToClassList("character-card-selected");
                    card.Add(new Label(ch.retainer != null ? ch.retainer.displayName : "(이름 없음)") { style = { fontSize = 15, unityFontStyleAndWeight = FontStyle.Bold } });
                    card.Add(new Label(ch.startingFaction != null ? ch.startingFaction.displayName : ""));
                    if (!string.IsNullOrEmpty(ch.startingSituation)) card.Add(new Label(ch.startingSituation) { style = { whiteSpace = WhiteSpace.Normal } });
                    card.RegisterCallback<ClickEvent>(_ => { _pickedCharacter = ch; RefreshCharacters(); });
                    characterList.Add(card);
                }
            }

            void RefreshScenarios()
            {
                scenarioList.Clear();
                scenarioList.Add(new Label("시나리오(시대) 선택") { style = { fontSize = 18, marginBottom = 8 } });
                foreach (var sc in Database.scenarios)
                {
                    var item = new VisualElement();
                    item.AddToClassList("scenario-item");
                    if (sc == _pickedScenario) item.AddToClassList("scenario-item-selected");
                    item.Add(new Label($"{sc.year}년 — {sc.displayName}") { style = { fontSize = 15, unityFontStyleAndWeight = FontStyle.Bold } });
                    if (!string.IsNullOrEmpty(sc.description)) item.Add(new Label(sc.description) { style = { whiteSpace = WhiteSpace.Normal } });
                    item.RegisterCallback<ClickEvent>(_ =>
                    {
                        _pickedScenario = sc;
                        _pickedCharacter = sc.playableCharacters.FirstOrDefault();
                        RefreshScenarios();
                        RefreshCharacters();
                    });
                    scenarioList.Add(item);
                }
            }

            RefreshScenarios();
            if (_pickedScenario != null) RefreshCharacters();

            var rightColumn = new VisualElement { style = { flexGrow = 1 } };
            rightColumn.Add(characterList);
            rightColumn.Add(startButton);

            _scenarioScreen.Add(scenarioList);
            _scenarioScreen.Add(rightColumn);
            _root.Add(_scenarioScreen);
        }

        void ShowScenarioScreen()
        {
            _scenarioScreen.RemoveFromClassList("hidden");
            _mainShell.AddToClassList("hidden");
        }

        void ShowMainShell()
        {
            _scenarioScreen.AddToClassList("hidden");
            _mainShell.RemoveFromClassList("hidden");
            RefreshTopBar();
            RefreshTabRail();
            RefreshContent();
        }

        // ───────── 메인 셸 ─────────

        void BuildMainShell()
        {
            _mainShell = new VisualElement { style = { flexGrow = 1 } };

            var topBar = new VisualElement();
            topBar.AddToClassList("top-bar");
            _topBarLabel = new Label();
            _topBarLabel.AddToClassList("faction-label");
            _resourceLabel = new Label();
            _dateLabel = new Label();
            var nextTurnButton = new Button { text = "다음 턴" };
            nextTurnButton.AddToClassList("action-button");
            nextTurnButton.AddToClassList("primary-button");
            nextTurnButton.clicked += () =>
            {
                GameManager.Instance.AdvanceSeason();
                RefreshTopBar();
                RefreshContent();
            };
            topBar.Add(_topBarLabel);
            topBar.Add(_resourceLabel);
            var spacer = new VisualElement(); spacer.AddToClassList("spacer");
            topBar.Add(spacer);
            topBar.Add(_dateLabel);
            topBar.Add(nextTurnButton);

            var bodyRow = new VisualElement();
            bodyRow.AddToClassList("body-row");

            _tabRail = new VisualElement();
            _tabRail.AddToClassList("tab-rail");

            _content = new VisualElement();
            _content.AddToClassList("content");

            _alertPanel = new VisualElement();
            _alertPanel.AddToClassList("alert-panel");
            _alertPanel.Add(new Label("조정 칙령 및 보고") { style = { fontSize = 15, marginBottom = 8 } });

            bodyRow.Add(_tabRail);
            bodyRow.Add(_content);
            bodyRow.Add(_alertPanel);

            var statusBar = new VisualElement();
            statusBar.AddToClassList("status-bar");
            _statusLabel = new Label("완산주 일대를 평정하세요.");
            statusBar.Add(_statusLabel);

            _mainShell.Add(topBar);
            _mainShell.Add(bodyRow);
            _mainShell.Add(statusBar);
            _mainShell.AddToClassList("hidden");
            _root.Add(_mainShell);
        }

        VisualElement _tabRail;

        void RefreshTabRail()
        {
            _tabRail.Clear();
            foreach (var group in TabGroups.Keys)
            {
                var btn = new Button { text = group };
                btn.AddToClassList("tab-button");
                if (group == _topGroup) btn.AddToClassList("tab-button-selected");
                btn.clicked += () =>
                {
                    _topGroup = group;
                    _subTab = TabGroups[group][0];
                    RefreshTabRail();
                    RefreshContent();
                };
                _tabRail.Add(btn);
            }
        }

        void RefreshTopBar()
        {
            var gm = GameManager.Instance;
            var factionDef = Database.GetFaction(gm.PlayerFactionId);
            var monarch = gm.GetRetainer(gm.PlayerFaction?.monarchRetainerId);
            var monarchDef = monarch != null ? Database.GetRetainer(monarch.retainerId) : null;
            _topBarLabel.text = $"{factionDef?.displayName}   군주: {monarchDef?.displayName}";

            var f = gm.PlayerFaction;
            _resourceLabel.text = f == null ? "" : $"석 {f.grain}    엽전 {f.coin}    권위 {f.prestige}";
            _dateLabel.text = $"{gm.Year}년 {GameManager.SeasonNames[gm.Season]}";

            if (_statusLabel != null)
                _statusLabel.text = gm.IsVictory
                    ? "완산주 일대를 통일했습니다! (§3.17 엔딩 화면은 다음 작업)"
                    : "완산주 일대를 평정하세요.";
        }

        void RefreshContent()
        {
            _content.Clear();

            var subTabs = TabGroups[_topGroup];
            if (!subTabs.Contains(_subTab)) _subTab = subTabs[0];

            var subRow = new VisualElement();
            subRow.AddToClassList("subtab-row");
            foreach (var st in subTabs)
            {
                var b = new Button { text = st };
                b.AddToClassList("subtab-button");
                if (st == _subTab) b.AddToClassList("subtab-button-selected");
                b.clicked += () => { _subTab = st; RefreshContent(); };
                subRow.Add(b);
            }
            _content.Add(subRow);

            switch (_subTab)
            {
                case "개요": BuildOverviewTab(); break;
                case "경제·농업": BuildEconomyTab(); break;
                case "호족 판도": BuildHojokTab(); break;
                default: BuildPlaceholderTab(); break;
            }
        }

        // ───────── 영지 개요 (§3.1) ─────────

        void BuildOverviewTab()
        {
            _content.Add(new Label("영지 개요") { style = { fontSize = 20, marginBottom = 10 } });
            var gm = GameManager.Instance;
            var owned = gm.ProvincesOf(gm.PlayerFactionId).ToList();

            var panel = new VisualElement(); panel.AddToClassList("panel");
            panel.Add(new Label($"보유 지역 {owned.Count}곳"));
            foreach (var p in owned)
            {
                var def = gm.Def(p);
                var tier = gm.Tier(p);
                panel.Add(new Label($"· {def?.displayName} — {tier?.displayName}, 충성도 {p.loyalty}, 농업 Lv.{p.agricultureLevel}, 저수지 Lv.{p.irrigationLevel}"));
            }
            _content.Add(panel);

            var actionRow = new VisualElement(); actionRow.AddToClassList("row");
            var expandBtn = new Button { text = "영지 확대" }; expandBtn.AddToClassList("action-button");
            expandBtn.clicked += () => { _topGroup = "호족·신하"; _subTab = "호족 판도"; RefreshTabRail(); RefreshContent(); };
            var taxBtn = new Button { text = "경제·농업" }; taxBtn.AddToClassList("action-button");
            taxBtn.clicked += () => { _subTab = "경제·농업"; RefreshContent(); };
            actionRow.Add(expandBtn);
            actionRow.Add(taxBtn);
            _content.Add(actionRow);
        }

        // ───────── 경제·농업 (§3.6) ─────────

        void BuildEconomyTab()
        {
            _content.Add(new Label("경제·농업") { style = { fontSize = 20, marginBottom = 10 } });
            var gm = GameManager.Instance;
            var owned = gm.ProvincesOf(gm.PlayerFactionId).ToList();
            if (owned.Count == 0) { _content.Add(new Label("보유한 지역이 없습니다.")); return; }
            if (string.IsNullOrEmpty(_selectedOwnProvinceId) || owned.All(p => p.provinceId != _selectedOwnProvinceId))
                _selectedOwnProvinceId = owned[0].provinceId;

            var pickerRow = new VisualElement(); pickerRow.AddToClassList("row");
            pickerRow.Add(new Label("지역"));
            foreach (var p in owned)
            {
                var b = new Button { text = gm.Def(p).displayName };
                b.AddToClassList("action-button");
                b.clicked += () => { _selectedOwnProvinceId = p.provinceId; RefreshContent(); };
                pickerRow.Add(b);
            }
            _content.Add(pickerRow);

            var province = gm.GetProvince(_selectedOwnProvinceId);
            var panel = new VisualElement(); panel.AddToClassList("panel");
            panel.Add(new Label($"농업 Lv.{province.agricultureLevel}    저수지 Lv.{province.irrigationLevel}"));
            panel.Add(new Label(string.IsNullOrEmpty(province.constructionId) ? "진행 중인 건설 없음" : $"건설 중: {province.constructionId} ({province.constructionSeasonsLeft}턴 남음)"));

            var upgradeRow = new VisualElement(); upgradeRow.AddToClassList("row");
            var agriBtn = new Button { text = "논 개간" }; agriBtn.AddToClassList("action-button");
            agriBtn.clicked += () => { GameManager.Instance.TryUpgradeAgriculture(province.provinceId); RefreshContent(); };
            var irriBtn = new Button { text = "저수지 정비" }; irriBtn.AddToClassList("action-button");
            irriBtn.clicked += () => { GameManager.Instance.TryUpgradeIrrigation(province.provinceId); RefreshContent(); };
            upgradeRow.Add(agriBtn);
            upgradeRow.Add(irriBtn);
            panel.Add(upgradeRow);

            var taxRow = new VisualElement(); taxRow.AddToClassList("row");
            taxRow.Add(new Label($"세율 {Mathf.RoundToInt(province.taxRate * 100)}%"));
            var taxSlider = new Slider(0f, 0.5f) { value = province.taxRate, style = { width = 200 } };
            taxSlider.RegisterValueChangedCallback(evt =>
            {
                GameManager.Instance.SetTaxRate(province.provinceId, evt.newValue);
                RefreshContent();
            });
            taxRow.Add(taxSlider);
            panel.Add(taxRow);

            _content.Add(panel);
        }

        // ───────── 호족 판도 (§3.2) ─────────

        void BuildHojokTab()
        {
            _content.Add(new Label("호족 판도") { style = { fontSize = 20, marginBottom = 10 } });
            var gm = GameManager.Instance;
            var hojokProvinces = gm.Provinces.Where(p => string.IsNullOrEmpty(p.ownerFactionId)).ToList();
            if (hojokProvinces.Count == 0) { _content.Add(new Label("완산주 일대를 모두 평정했습니다!")); return; }

            var listPanel = new VisualElement(); listPanel.AddToClassList("panel");
            foreach (var p in hojokProvinces)
            {
                var def = gm.Def(p);
                var row = new VisualElement(); row.AddToClassList("row");
                row.Add(new Label($"{def?.displayName} ({p.grievance})"));
                row.Add(new Label($"충성도 {p.loyalty}"));
                var selectBtn = new Button { text = p.provinceId == _selectedHojokProvinceId ? "선택됨" : "선택" };
                selectBtn.AddToClassList("action-button");
                selectBtn.clicked += () => { _selectedHojokProvinceId = p.provinceId; RefreshContent(); };
                row.Add(selectBtn);
                listPanel.Add(row);
            }
            _content.Add(listPanel);

            if (string.IsNullOrEmpty(_selectedHojokProvinceId) || hojokProvinces.All(p => p.provinceId != _selectedHojokProvinceId))
                return;

            var selected = gm.GetProvince(_selectedHojokProvinceId);
            var actionPanel = new VisualElement(); actionPanel.AddToClassList("panel");
            actionPanel.Add(new Label($"대상: {gm.Def(selected).displayName}"));

            var row2 = new VisualElement(); row2.AddToClassList("row");
            var appeaseBtn = new Button { text = $"호족 회유 (엽전 {gm.Database.appeaseCoinCost})" };
            appeaseBtn.AddToClassList("action-button");
            appeaseBtn.clicked += () => { GameManager.Instance.TryAppeaseHojok(selected.provinceId); RefreshContent(); };
            var surnameBtn = new Button { text = $"사성 (권위 {gm.Database.grantSurnamePrestigeCost})" };
            surnameBtn.AddToClassList("action-button");
            surnameBtn.clicked += () => { GameManager.Instance.TryGrantSurname(selected.provinceId); RefreshContent(); };
            var purgeBtn = new Button { text = "숙청 명령 (전투)" };
            purgeBtn.AddToClassList("action-button");
            purgeBtn.clicked += () =>
            {
                GameManager.Instance.TryPurge(selected.provinceId, out var report);
                RefreshContent();
                ShowBattleAlert(report);
            };
            row2.Add(appeaseBtn);
            row2.Add(surnameBtn);
            row2.Add(purgeBtn);
            actionPanel.Add(row2);
            _content.Add(actionPanel);
        }

        void ShowBattleAlert(GameManager.BattleReport report)
        {
            if (report == null) return;
            var item = new VisualElement(); item.AddToClassList("alert-item");
            item.Add(new Label(report.victory ? "전투 승리" : "전투 패배") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            item.Add(new Label($"아군 {report.attackerPower:0} vs 적 {report.defenderPower:0}"));
            _alertPanel.Insert(1, item);
        }

        // ───────── 준비 중 ─────────

        void BuildPlaceholderTab()
        {
            _content.Add(new Label($"{_subTab} 화면은 다음 작업에서 구현합니다 (docs/GDD.md 참고)."));
        }
    }
}
