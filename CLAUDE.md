# LateThreeKingdoms (후삼국통일) — 작업 참고사항

이 저장소를 Claude Code(클라우드든 로컬이든)에서 작업할 때 참고할 것. 작업 시작 전에 읽을 것.
**현재 상태·다음 할 일·사용자에게 물어볼 것은 `docs/HANDOFF.md`에 있다 — 그것부터 읽을 것.**

## 이 저장소의 내력

이 프로젝트는 원래 `stone_cat`(룰더킹덤과 같은 저장소) 안에 `LateThreeKingdoms/` 폴더로 들어있었다. 서로 다른 두 게임이 한 저장소에 섞여 있어 사용자가 다운로드/빌드 과정에서 반복적으로 혼란을 겪었고, 그래서 커밋 히스토리를 보존한 채 이 저장소로 분리했다. **이제부터 이 게임 관련 작업은 전부 이 저장소에서만 한다.** `stone_cat`은 더 이상 이 프로젝트와 무관하다.

## 사용자 특성 (가장 중요)

- **사용자는 비개발자다.** 터미널, git 명령어, 파일 경로 탐색에 익숙하지 않다.
- 모든 로컬 작업 안내는 **GUI 클릭 단위로, 번호 매겨서, 틀릴 여지 없이** 구체적으로 줄 것. 축약된 지시는 반드시 혼란을 일으킨다.
- 무엇을 받았는지/어디 있는지/뭐가 설치돼 있는지 모르면 추측하지 말고 먼저 확인할 것.
- 사용자에게 저장소 링크를 줄 때는 항상 `https://github.com/YT-NA/LateThreeKingdoms` (브랜치는 `main`)를 명시해서 줄 것.
- 사용자는 여러 컴퓨터를 오가며 작업한다 (집/다른 PC, 클라우드 세션/로컬 Claude Code). 특정 컴퓨터 경로를 가정하지 말 것.

## 이 게임에서 사용자가 가장 중시하는 것

v1 플레이테스트에서 사용자가 크게 실망한 이유: "역사 기반 전략 시뮬레이션의 핵심은 롤플레잉 — **몰입과 전개**. 계속 선택지를 주고 **무엇을 해야 하는지 알려주면서** 운영을 이어 가게 해야 하는데, 그냥 텍스트 덩어리였다." (`docs/PLAYTEST_FEEDBACK.md`)
→ 기능을 추가할 때는 항상 **사건·선택지·목표·책사 조언·진행 표시**와 연결되게 만들 것. 숫자만 바뀌는 버튼, "다음 작업에서 구현합니다" 같은 빈 탭은 넣지 말 것.

## 클라우드 샌드박스에는 Unity가 없다 → `tools/headless`로 검증할 것

- 클라우드 샌드박스에는 Unity Editor/CLI가 없다. 대신 **`tools/headless/`** 로 Unity 없이 검증한다 (`tools/headless/README.md`):
  - `dotnet build tools/headless/typecheck/TypeCheck.csproj -v q` — 전체 스크립트를 Unity API 스텁에 대고 컴파일 (실제로 `using UnityEngine` 누락 같은 오류를 잡음)
  - `dotnet run --project tools/headless/sim/Sim.csproj` — 게임 로직을 12판 자동 플레이 (예외·진행 막힘·저장 왕복·밸런스)
  - .NET SDK가 없으면: `apt-get update && apt-get install -y --no-install-recommends dotnet-sdk-8.0` (dot.net 설치 스크립트는 프록시가 막음)
- **게임 코드를 고쳤으면 푸시 전에 둘 다 돌릴 것.** 그래도 스텁은 진짜 Unity가 아니므로 화면 배치·USS·임포트 문제는 사용자 빌드로만 확인된다 — 사용자에게 "검증했다"고 과장하지 말 것.
- 실제 빌드는 **사용자의 Windows PC에서 Unity 6(6000.x)로만 가능**하다. 작업 시작 전 사용자 PC에 Unity Hub + Unity 6 에디터가 설치되어 있는지 먼저 확인할 것.
- 빌드 파이프라인: `Assets/_Project/Scripts/Editor/GameSetup.cs`(샘플 데이터+씬 생성, 그림 임포트 설정 바로잡기·자동 연결) + `BuildScript.cs`(`-batchmode -executeMethod` 헤드리스 빌드) + 저장소 루트의 `후삼국_Unity로_열기.bat`(에디터로 열기)/`후삼국_만들기.bat`(배치모드 빌드+실행, 로그를 `build_husamguk.log`에 남김). **빌드 실패 시 반드시 이 로그 내용을 받아서 진단할 것 — 추측으로 고치려 하지 말 것.**

## 빌드/세이브 설정

- 실행 파일 경로: `바탕 화면\후삼국시대\{GameVersion.Version}\후삼국통일.exe`. `C:\Users\sjart\OneDrive\바탕 화면`이 있으면 그곳(사용자가 지정), 없으면 그 컴퓨터의 바탕 화면 (`BuildScript.OutputDir`). 새 버전을 낼 때는 `Core/GameVersion.cs`를 올린다(켜져 있는 이전 버전 exe와 충돌 방지).
- 세이브 파일은 exe와 같은 폴더의 `save.json` (`Scripts/Core/SaveSystem.cs`). 매 계절 자동 저장.
- `BuildScript.RepoRoot`는 Unity 프로젝트 폴더(=저장소 최상위). 예전 `stone_cat` 시절의 `..\..` 경로로 되돌리지 말 것 (그러면 `.bat`가 결과 파일을 못 찾아 "실패"라고 함).
- `.bat` 파일들은 `%~dp0`(자기 자신이 있는 폴더, 곧 저장소 루트)를 프로젝트 경로로 바로 쓴다. 폴더 구조를 바꿀 일이 생기면, `"%PROJ%"`처럼 따옴표 바로 앞에 백슬래시가 오지 않도록 끝 백슬래시를 잘라내는 처리(`if "%PROJ:~-1%"=="\" set "PROJ=%PROJ:~0,-1%"`)를 유지할 것 — 안 하면 Windows 커맨드라인 파싱이 깨진다.
- `.meta` 파일은 저장소에 없다(받을 때마다 Unity가 새로 만든다). 그래서 그림이 첫 임포트 때 일반 텍스처로 찍힐 수 있고, `SpriteLinker.FixImportSettings()`가 매 세팅/빌드 때 바로잡는다.

## 기획서

- `docs/HANDOFF.md` — **현재 상태·다음 할 일·질문** (작업 끝날 때마다 갱신할 것)
- `docs/GDD.md` — 메인 기획서
- `docs/CAMPAIGN_ACTS.md` — 3개 장(후백제→신라→고려) 15막 전체 표
- `docs/CAMPAIGN_SCRIPT.md` — 하이라이트 막 컷신 대사 초안
- `docs/ART_LIST.md` — PixelLab 아트 에셋 목록
- `docs/PLAYTEST_FEEDBACK.md` — 플레이테스트 기록
