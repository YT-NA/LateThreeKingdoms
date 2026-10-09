# LateThreeKingdoms (후삼국통일) — 클라우드 작업 참고사항

이 저장소를 Claude Code 클라우드 세션에서 작업할 때 참고할 것. 작업 시작 전에 읽을 것.

## 이 저장소의 내력

이 프로젝트는 원래 `stone_cat`(룰더킹덤과 같은 저장소) 안에 `LateThreeKingdoms/` 폴더로 들어있었다. 서로 다른 두 게임이 한 저장소에 섞여 있어 사용자가 다운로드/빌드 과정에서 반복적으로 혼란을 겪었고, 그래서 커밋 히스토리를 보존한 채 이 저장소로 분리했다. **이제부터 이 게임 관련 작업은 전부 이 저장소에서만 한다.** `stone_cat`은 더 이상 이 프로젝트와 무관하다.

## 사용자 특성 (가장 중요)

- **사용자는 비개발자다.** 터미널, git 명령어, 파일 경로 탐색에 익숙하지 않다.
- 모든 로컬 작업 안내는 **GUI 클릭 단위로, 번호 매겨서, 틀릴 여지 없이** 구체적으로 줄 것. 축약된 지시는 반드시 혼란을 일으킨다.
- 무엇을 받았는지/어디 있는지/뭐가 설치돼 있는지 모르면 추측하지 말고 먼저 확인할 것.
- 사용자에게 저장소 링크를 줄 때는 항상 `https://github.com/YT-NA/LateThreeKingdoms` (브랜치는 `main`)를 명시해서 줄 것.

## 클라우드 샌드박스에는 Unity가 없다

- 이 환경(Claude Code 클라우드 샌드박스)에는 Unity Editor/CLI가 설치되어 있지 않다. C# 코드를 작성해도 **컴파일 테스트가 불가능**하다 — 중괄호 짝, 필드명 교차 참조, API 사용법을 수동으로 검토하는 것이 할 수 있는 전부.
- 실제 빌드는 **사용자의 Windows PC에서 Unity 6(6000.x)로만 가능**하다. 작업 시작 전 사용자 PC에 Unity Hub + Unity 6 에디터가 설치되어 있는지 먼저 확인할 것.
- 빌드 파이프라인: `Assets/_Project/Scripts/Editor/GameSetup.cs`(샘플 데이터+씬 생성) + `BuildScript.cs`(`-batchmode -executeMethod` 헤드리스 빌드) + 저장소 루트의 `후삼국_Unity로_열기.bat`(에디터로 열기)/`후삼국_만들기.bat`(배치모드 빌드+실행, 로그를 `build_husamguk.log`에 남김). **빌드 실패 시 반드시 이 로그 내용을 받아서 진단할 것 — 추측으로 고치려 하지 말 것.**

## 빌드/세이브 설정

- 빌드 산출물 경로는 하드코딩되어 있음: `C:\Users\sjart\OneDrive\바탕 화면\후삼국시대` (exe 이름: `후삼국통일.exe`). `BuildScript.OutputDir`.
- 세이브 파일은 exe와 같은 폴더에 생성됨 (`Scripts/Core/SaveSystem.cs`, `Application.dataPath` 기준 상대경로).
- `.bat` 파일들은 `%~dp0`(자기 자신이 있는 폴더, 곧 저장소 루트)를 프로젝트 경로로 바로 쓴다. 폴더 구조를 바꿀 일이 생기면, `"%PROJ%"`처럼 따옴표 바로 앞에 백슬래시가 오지 않도록 끝 백슬래시를 잘라내는 처리(`if "%PROJ:~-1%"=="\" set "PROJ=%PROJ:~0,-1%"`)를 유지할 것 — 안 하면 Windows 커맨드라인 파싱이 깨진다.

## 기획서

- `docs/GDD.md` — 메인 기획서
- `docs/CAMPAIGN_ACTS.md` — 3개 장(후백제→신라→고려) 15막 전체 표
- `docs/CAMPAIGN_SCRIPT.md` — 하이라이트 막 컷신 대사 초안
- `docs/ART_LIST.md` — PixelLab 아트 에셋 목록
