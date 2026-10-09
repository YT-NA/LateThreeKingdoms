# 후삼국통일 (후삼국 대란: 천하통일)

후삼국시대 배경 턴제(계절 단위) 역사 전략 시뮬레이션 (Unity 6, UI Toolkit)

- 기획서: [docs/GDD.md](docs/GDD.md)

## 시작하기

1. Unity Hub → **Add → Add project from disk** → 저장소 안의 `LateThreeKingdoms` 폴더 선택 → Unity 6 버전 선택
2. 처음 열면 `Packages/`와 나머지 `ProjectSettings/` 파일이 자동 생성됩니다. (커밋해 주세요)
3. 상단 메뉴 **Husamguk → MVP 샘플 세팅** 실행
   - 완산주 일대 샘플 지역(§GDD 3.0)·세력·신하·시나리오 데이터와 `Main` 씬이 만들어집니다.
4. `Assets/_Project/Scenes/Main.unity`를 열고 **Play**
5. 시나리오(900년) → 인물(김억) 선택 → **시작하기**

## 실행 파일로 테스트하기

개발은 클라우드(Claude)에서 하고, 테스트는 내려받은 실행 파일로 한다는 전제로 빌드 파이프라인을 맞춰 뒀다.

- 저장소 최상위의 **`후삼국_만들기.bat`** 더블클릭 → Unity를 창 없이 실행해 빌드하고, 끝나면 바로 실행 파일을 켠다.
  (RuleTheKingdom의 `게임_만들기.bat`와 같은 패턴. Unity 6이 설치돼 있어야 한다.)
- 결과물 위치: `C:\Users\sjart\OneDrive\바탕 화면\후삼국시대\v1\후삼국통일.exe` (버전은 `Scripts/Core/GameVersion.cs`, 경로는 `BuildScript.OutputDir`에서 바꿀 수 있음)
- **세이브 파일은 실행 파일과 같은 폴더**에 `save.json`으로 생긴다(`Core/SaveSystem.cs`) — 에디터 Play 모드에서는 프로젝트 루트에 생긴다.
- 빌드가 실패하면 저장소 최상위의 `build_husamguk.log`를 확인.

## 현재 구현 범위 (1차 패스)

GDD의 화면 17개 중 아래만 실제로 동작합니다. 나머지는 좌측 탭에서 "다음 작업" placeholder로 뜹니다.

- 시나리오 선택(§3.18)
- 영지 개요(§3.1)
- 경제·농업 — 논 개간/저수지 정비/세율 슬라이더(§3.6)
- 호족 판도 — 회유/사성/숙청 명령(§3.2, 혼인·인질은 다음 작업)
- 계절 턴 진행, 단순 전투(병종 상성 포함, §5.5), 거점 등급 자동 승격(§5.6), AI의 선제 공격(§5.8), 군주 수명·자연 승계(§5.9, 1~2단계만)
- 저장은 아직 UI에 연결되지 않음 (`GameManager.SaveGame()/LoadGame()`은 구현됨)

다음 작업: 주요 관직·전체 신하·왕위 계승·외교·책략·병영·징집·출진 부대 편성·민심 관리·혼사 결정·자식 교육 화면, 전투 결과 팝업, 거점 승격 팝업, 엔딩 화면.

## 아트

실제 그림은 아직 없습니다 (PixelLab 연동 예정, §GDD 8). 지금은 USS 단색 패널로 레이아웃만 잡아 둔 상태입니다.

## 폴더 구조

```
Assets/_Project/
  Scripts/Data      정의 (ScriptableObject): ScenarioDef, FactionDef, ProvinceDef, SettlementTierDef,
                     RetainerDef, OfficeDef, UnitDef, ResourceDef, GameDatabase
  Scripts/Core      GameManager(턴 진행·경제·호족·전투·AI·승계), SaveSystem, 런타임 상태(GameState.cs)
  Scripts/UI        GameUI (UI Toolkit, 시나리오 선택 + 메인 셸)
  Scripts/Editor    GameSetup (샘플 데이터 + Main 씬 생성)
  UI/               Shell.uss
  Fonts/            갈무리 (RuleTheKingdom과 공유)
  Data/ Scenes/     (에디터 메뉴가 생성)
```
