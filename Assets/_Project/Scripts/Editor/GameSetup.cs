using System.Collections.Generic;
using System.IO;
using Husamguk.Core;
using Husamguk.Data;
using Husamguk.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Husamguk.EditorTools
{
    /// <summary>
    /// 메뉴 [Husamguk > MVP 샘플 세팅] 한 번으로 완산주 일대 샘플 데이터와 Main 씬을 만든다.
    /// RuleTheKingdom의 KingdomSetup과 같은 역할. 다시 실행하면 샘플 데이터 값은 기본값으로 덮어쓴다.
    /// </summary>
    public static class GameSetup
    {
        const string Root = "Assets/_Project";

        /// <summary>true면 확인 창 없이 진행 (실행 파일 자동 빌드에서 쓴다).</summary>
        public static bool Silent;

        [MenuItem("Husamguk/MVP 샘플 세팅")]
        public static void Run()
        {
            PlayerSettings.productName = "후삼국통일";
            foreach (var dir in new[] { "Data/Resources", "Data/Tiers", "Data/Units", "Data/Offices", "Data/Retainers", "Data/Provinces", "Data/Factions", "Data/Scenarios", "Scenes" })
                Directory.CreateDirectory(Path.Combine(Root, dir));
            AssetDatabase.Refresh();

            // ── 자원 (§5.1) ──
            var grain = ResourceAsset("grain", "석", new Color(0.82f, 0.68f, 0.32f));
            var coin = ResourceAsset("coin", "엽전", new Color(0.85f, 0.75f, 0.35f));
            var prestige = ResourceAsset("prestige", "권위", new Color(0.7f, 0.3f, 0.3f));

            // ── 거점 등급 (§6.1) — 촌→현→군→주→경 ──
            var tierVillage = TierAsset("village", "촌", "村", "촌주", 0, 0, 0);
            var tierTownship = TierAsset("township", "현", "縣", "현령", 80, 3, 200);
            var tierDistrict = TierAsset("district", "군", "郡", "태수", 200, 8, 600);
            var tierProvince = TierAsset("province", "주", "州", "도독", 500, 20, 1500);
            var tierCapital = TierAsset("capital", "경", "京", "직할", 1000, 40, 4000);

            // ── 병종 (§3.9, §3.11) — 창병 > 기병 > 궁병 > 창병 ──
            var uInfantry = UnitAsset("infantry", "보병", UnitClass.None, 10,
                new[] { new ResourceAmount(grain, 4) }, 1, 3);
            var uSpear = UnitAsset("spear", "창병", UnitClass.Spear, 12,
                new[] { new ResourceAmount(grain, 5), new ResourceAmount(coin, 2) }, 1, 4);
            var uArcher = UnitAsset("archer", "궁병", UnitClass.Archer, 11,
                new[] { new ResourceAmount(grain, 5), new ResourceAmount(coin, 3) }, 1, 4);
            var uCavalry = UnitAsset("cavalry", "기병", UnitClass.Cavalry, 16,
                new[] { new ResourceAmount(grain, 8), new ResourceAmount(coin, 5) }, 2, 6);

            // ── 관직 (§3.3) ──
            var officeAdmin = OfficeAsset("daesangseo", "대상서", OfficeField.Administration, 0.15f, "내정 효율 +15%");
            var officeMilitary = OfficeAsset("daejanggun", "대장군", OfficeField.Military, 0.20f, "중앙군 훈련 속도 +20%");
            var officeDiplomacy = OfficeAsset("naesaryeong", "내사령", OfficeField.Diplomacy, 0.25f, "외교 성공률 +25%");

            // ── 세력 (§2) ──
            var playerFaction = FactionAsset("isung_clan", "이서 호족", new Color(0.5f, 0.25f, 0.2f),
                "완산주 일대의 말단 호족. 플레이어의 시작 세력.");

            // ── 신하: MVP 시나리오 주인공 ──
            var lordIeok = RetainerAsset("ieok", "김억", "", 40, 55, 45, 30, 35, 40, 50);

            // ── 지역 (§3.0) — 완산주를 중심에 두고 인접 호족 7곳 ──
            var wansanju = ProvinceAsset("wansanju", "완산주", new Vector2(0, 0), tierProvince, null, "견훤 세력권",
                new[] { "gimje", "imsil", "jinan", "geumgu" }, 0.1f, "호남 평야의 중심 거점. 식량 생산력이 뛰어나다.");
            var iseo = ProvinceAsset("iseo", "이서", new Vector2(-3, 1), tierVillage, playerFaction, null,
                new[] { "gobu", "geumgu" }, 0f, null);
            var gobu = ProvinceAsset("gobu", "고부", new Vector2(-4, -1), tierTownship, null, "고부 호족",
                new[] { "iseo", "gimje", "taein" }, 0f, null);
            var gimje = ProvinceAsset("gimje", "김제", new Vector2(-1, -2), tierTownship, null, "김제 호족",
                new[] { "gobu", "wansanju" }, 0.05f, "곡창 지대.");
            var taein = ProvinceAsset("taein", "태인", new Vector2(-3, -3), tierVillage, null, "태인 호족",
                new[] { "gobu", "imsil" }, 0f, null);
            var imsil = ProvinceAsset("imsil", "임실", new Vector2(0, -3), tierVillage, null, "임실 호족",
                new[] { "taein", "jinan", "wansanju" }, 0f, null);
            var jinan = ProvinceAsset("jinan", "진안", new Vector2(2, -2), tierVillage, null, "진안 호족",
                new[] { "imsil", "wansanju" }, 0f, null);
            var geumgu = ProvinceAsset("geumgu", "금구", new Vector2(0, 2), tierVillage, null, "금구 호족",
                new[] { "iseo", "wansanju" }, 0f, null);

            // ── 시나리오 (§3.18) ──
            var scenario900 = Asset<ScenarioDef>("Data/Scenarios/Scenario_900.asset", s =>
            {
                s.id = "scenario_900";
                s.displayName = "후삼국 초기 — 완산주의 이름 없는 호족";
                s.year = 900;
                s.description = "말단 호족으로 시작해 완산주 일대를 평정하라.";
                s.playableCharacters = new List<ScenarioDef.PlayableCharacter>
                {
                    new()
                    {
                        retainer = lordIeok,
                        startingFaction = playerFaction,
                        startingProvince = iseo,
                        startingSituation = "이서의 작은 성 하나를 다스리는 촌주. 완산주의 그늘 아래서 세력을 키워야 한다.",
                    },
                };
            });

            var db = Asset<GameDatabase>("Data/GameDatabase.asset", d =>
            {
                d.scenarios = new List<ScenarioDef> { scenario900 };
                d.factions = new List<FactionDef> { playerFaction };
                d.settlementTiers = new List<SettlementTierDef> { tierVillage, tierTownship, tierDistrict, tierProvince, tierCapital };
                d.provinces = new List<ProvinceDef> { wansanju, iseo, gobu, gimje, taein, imsil, jinan, geumgu };
                d.units = new List<UnitDef> { uInfantry, uSpear, uArcher, uCavalry };
                d.offices = new List<OfficeDef> { officeAdmin, officeMilitary, officeDiplomacy };
                d.retainers = new List<RetainerDef> { lordIeok };
                d.grain = grain;
                d.coin = coin;
                d.prestige = prestige;
            });

            AssetDatabase.SaveAssets();
            // Art/Sprites에 PixelLab 그림이 있으면 자동으로 연결 (docs/ART_LIST.md 이름 규칙)
            SpriteLinker.Run();
            AssetDatabase.SaveAssets();
            BuildScene(db);
            AssetDatabase.SaveAssets();
            Debug.Log("[GameSetup] 완료! Main 씬에서 Play를 누르고 시나리오를 골라 시작하세요.");
        }

        static void BuildScene(GameDatabase db)
        {
            var scenePath = $"{Root}/Scenes/Main.unity";
            if (!Silent && File.Exists(scenePath) &&
                !EditorUtility.DisplayDialog("Husamguk", "Main 씬이 이미 있습니다. 새로 만들까요?", "새로 만들기", "취소"))
                return;
            if (!Silent && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string dbPath = AssetDatabase.GetAssetPath(db);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            db = AssetDatabase.LoadAssetAtPath<GameDatabase>(dbPath);

            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 0, -10);
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.08f, 0.06f);

            var gmGo = new GameObject("GameManager", typeof(GameManager));
            var so = new SerializedObject(gmGo.GetComponent<GameManager>());
            so.FindProperty("database").objectReferenceValue = db;
            so.ApplyModifiedPropertiesWithoutUndo();

            var panelSettings = Asset<PanelSettings>("UI/PanelSettings.asset", p =>
            {
                p.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                p.referenceResolution = new Vector2Int(1920, 1080);
                p.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                p.match = 0.5f;
            });
            var uiGo = new GameObject("GameUI", typeof(UIDocument), typeof(GameUI));
            uiGo.GetComponent<UIDocument>().panelSettings = panelSettings;
            var uso = new SerializedObject(uiGo.GetComponent<GameUI>());
            uso.FindProperty("styleSheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<StyleSheet>($"{Root}/UI/Shell.uss");
            uso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        }

        // ───────── 헬퍼 ─────────

        static ResourceDef ResourceAsset(string id, string name, Color color) =>
            Asset<ResourceDef>($"Data/Resources/Res_{id}.asset", r => { r.id = id; r.displayName = name; r.placeholderColor = color; });

        static SettlementTierDef TierAsset(string id, string name, string hanja, string office, int pop, int mil, int econ) =>
            Asset<SettlementTierDef>($"Data/Tiers/Tier_{id}.asset", t =>
            {
                t.id = id; t.displayName = name; t.hanja = hanja; t.officeTitle = office;
                t.populationThreshold = pop; t.militaryThreshold = mil; t.economyThreshold = econ;
            });

        static UnitDef UnitAsset(string id, string name, UnitClass unitClass, int power, ResourceAmount[] cost, int seasons, int foodUpkeep) =>
            Asset<UnitDef>($"Data/Units/Unit_{id}.asset", u =>
            {
                u.id = id; u.displayName = name; u.unitClass = unitClass; u.power = power;
                u.recruitCost = cost; u.recruitSeasons = seasons; u.foodUpkeepPerSoldier = foodUpkeep;
            });

        static OfficeDef OfficeAsset(string id, string name, OfficeField field, float value, string desc) =>
            Asset<OfficeDef>($"Data/Offices/Office_{id}.asset", o =>
            { o.id = id; o.displayName = name; o.field = field; o.effectValue = value; o.effectDescription = desc; });

        static FactionDef FactionAsset(string id, string name, Color color, string desc) =>
            Asset<FactionDef>($"Data/Factions/Faction_{id}.asset", f =>
            { f.id = id; f.displayName = name; f.mapColor = color; f.description = desc; });

        static RetainerDef RetainerAsset(string id, string name, string hanja, int leadership, int might, int intellect, int scholarship, int politics, int charm, int virtue) =>
            Asset<RetainerDef>($"Data/Retainers/Retainer_{id}.asset", r =>
            {
                r.id = id; r.displayName = name; r.hanjaName = hanja;
                r.leadership = leadership; r.might = might; r.intellect = intellect;
                r.scholarship = scholarship; r.politics = politics; r.charm = charm; r.virtue = virtue;
            });

        static ProvinceDef ProvinceAsset(string id, string name, Vector2 pos, SettlementTierDef tier, FactionDef owner, string hojokName,
            string[] adjacent, float grainBonus, string specialty) =>
            Asset<ProvinceDef>($"Data/Provinces/Province_{id}.asset", p =>
            {
                p.id = id; p.displayName = name; p.mapPosition = pos; p.startingTier = tier;
                p.startingFaction = owner; p.startingHojokName = hojokName;
                p.adjacentProvinceIds = new List<string>(adjacent);
                p.grainBonus = grainBonus; p.specialty = specialty;
                p.startingPopulation = 50;
            });

        static T Asset<T>(string relativePath, System.Action<T> configure) where T : ScriptableObject
        {
            var path = $"{Root}/{relativePath}";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            configure(asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
