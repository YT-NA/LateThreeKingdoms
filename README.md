# 후삼국통일 (후삼국 대란: 천하통일)

후삼국시대 배경 턴제(계절 단위) 역사 전략 롤플레잉 시뮬레이션 (Unity 6, UI Toolkit)

- **처음이라면 / 이어서 작업한다면: [docs/HANDOFF.md](docs/HANDOFF.md)** — 현재 상태, 실행 방법, 결정이 필요한 질문
- 기획서: [docs/GDD.md](docs/GDD.md) · 캠페인 15막 표: [docs/CAMPAIGN_ACTS.md](docs/CAMPAIGN_ACTS.md) · 대사 초안: [docs/CAMPAIGN_SCRIPT.md](docs/CAMPAIGN_SCRIPT.md) · 아트 목록: [docs/ART_LIST.md](docs/ART_LIST.md)
- 플레이테스트 기록: [docs/PLAYTEST_FEEDBACK.md](docs/PLAYTEST_FEEDBACK.md)

## 게임 하기 (실행 파일)

1. Unity Hub에 **Unity 6 (6000.x)** 가 설치돼 있어야 한다.
2. 이 저장소를 받아(Code → Download ZIP → 압축 풀기) **`후삼국_만들기.bat`** 더블클릭 → Unity 창 없이 빌드한 뒤 게임을 바로 켠다 (처음 5~15분).
3. 결과물: **바탕 화면\후삼국시대\v2\후삼국통일.exe** (`C:\Users\sjart\OneDrive\바탕 화면`이 있으면 그곳, 없으면 그 컴퓨터의 바탕 화면). 버전은 `Scripts/Core/GameVersion.cs`.
4. 세이브는 **실행 파일과 같은 폴더**의 `save.json` — 매 계절 자동 저장, 시작 화면의 "이어하기".
5. 빌드가 실패하면 이 폴더의 `build_husamguk.log` 확인.

## 개발 (Unity 에디터)

1. **`후삼국_Unity로_열기.bat`** 더블클릭 (또는 Unity Hub → Add project from disk → 이 폴더)
2. 상단 메뉴 **Husamguk → MVP 샘플 세팅** — 샘플 데이터·`Main` 씬 생성, 그림 자동 연결
3. `Assets/_Project/Scenes/Main.unity` 열고 **Play**

## 지금 들어 있는 것 (v2)

- **시나리오**: 900년, 완산주 일대 여덟 고을. 시작 인물 2명 — 김억(이서, 회유형) / 김총(고부, 무력형)
- **사건 21종**: 초상화·상황 서술·딜레마형 선택지 (견훤의 사신과 침공, 민란, 혼담, 가뭄, 궁예의 밀사, 견훤 집안의 불화, 엔딩 등)
- **목표 7단계** + **책사 최응의 조언**(상황 진단 → 다음 수 제안 → 바로가기)
- **판도 지도**, 영지 개요·경제·농업(공사·세율·장터), 호족 판도(회유·사성·토벌 → 귀부), 신하, 병영·징집, 출진(승산 예측), 연대기
- 계절 리듬(가을 풍년·겨울 흉작), 군량, 민심, 고을 승격(촌→현→군→주→경), 병종 상성, 성곽

다음 작업 후보와 결정이 필요한 사항은 [docs/HANDOFF.md](docs/HANDOFF.md) 참고.

## 폴더 구조

```
Assets/_Project/
  Scripts/Data      정의 (ScriptableObject): ScenarioDef, FactionDef, ProvinceDef, SettlementTierDef,
                     RetainerDef, OfficeDef, UnitDef, ResourceDef, GameDatabase
  Scripts/Core      GameManager(계절 진행·경제·민심·호족·전투·AI·저장), Story(사건), Objectives(목표),
                     Advisor(책사 조언), SaveSystem, GameState·SaveData
  Scripts/UI        GameUI (UI Toolkit: 시작 화면, 메인 셸, 지도, 대화 창)
  Scripts/Editor    GameSetup(샘플 데이터·씬), BuildScript(실행 파일), SpriteLinker·PixelArtImporter(그림)
  UI/               Shell.uss
  Art/Sprites/      PixelLab 그림 37장 (docs/ART_LIST.md)
  Fonts/            갈무리
tools/headless/     Unity 없이 컴파일 검사·게임 자동 플레이 (클라우드 세션용, Unity가 무시함)
tools/pixelart/     PixelLab 그림 생성 스크립트
```
