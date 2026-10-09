# 헤드리스 검증 도구 (Unity 없이)

클라우드 세션에는 Unity가 없어서, 예전엔 C# 코드를 한 번도 컴파일해 보지 못한 채 사용자에게 넘겼고 실제로 컴파일 오류(`using UnityEngine` 누락)로 빌드가 깨졌다.
이 폴더는 **Unity 없이** 두 가지를 확인한다.

| 도구 | 하는 일 |
|---|---|
| `typecheck/` | `Assets/_Project/Scripts` 전체(Data/Core/UI/Editor)를 최소 Unity API 스텁(`stubs/`)에 대고 컴파일 — 문법·타입·없는 멤버 오류를 잡는다 |
| `sim/` | 게임 로직(Data/Core)을 실제로 실행해 두 인물 × 두 플레이 성향(회유/토벌) × 시드 3개 = 12판을 자동 플레이 — 예외, 진행 막힘(고를 수 없는 사건), 저장/불러오기 왕복, 밸런스(몇 계절 만에 이기는지)를 본다 |

## 준비 (클라우드 세션 1회)

```bash
apt-get update && apt-get install -y --no-install-recommends dotnet-sdk-8.0
```
(`dot.net` 설치 스크립트는 프록시가 막는다. Ubuntu 저장소의 `dotnet-sdk-8.0`은 된다.)

## 실행

```bash
dotnet build tools/headless/typecheck/TypeCheck.csproj -v q     # "Build succeeded" 가 떠야 함
dotnet run --project tools/headless/sim/Sim.csproj               # 12판 요약
dotnet run --project tools/headless/sim/Sim.csproj -- -v         # 전투·나쁜 소식·목표 달성까지 자세히
```

**게임 코드를 고친 뒤 푸시하기 전에 두 개 다 돌릴 것.**

## 한계 (중요)

- 스텁은 실제 Unity가 아니다. 타입 검사를 통과해도 **Unity에서만 드러나는 문제**(UI 배치·글자 넘침·USS 경고·스프라이트 임포트·직렬화 차이 등)는 잡지 못한다. 그건 사용자의 실제 빌드/플레이로만 확인된다.
- 게임 코드가 스텁에 없는 Unity API를 새로 쓰면 typecheck가 "없는 멤버" 오류를 낸다 — 그때는 실제 Unity 문서와 **시그니처(매개변수 이름까지)를 똑같이** 스텁에 추가할 것. 스텁을 틀리게 만들면 Unity에서 깨질 코드가 여기선 통과한다.
- `sim/Program.cs`의 `BuildDb()`는 `Editor/GameSetup.cs` 샘플 데이터를 **손으로 옮긴 것**이다. GameSetup 값을 바꾸면 여기도 같이 바꿔야 시뮬레이션이 의미가 있다.
- 시뮬레이션의 "플레이어"는 단순한 규칙 봇이다. 봇이 곡식을 바닥내는 등 사람보다 서툴다 — 봇이 이길 수 있으면 사람도 이길 수 있다는 하한선 정도로 본다.

## 마지막 결과 (v2, 2026-10-09)

12판 모두 승리. 김총·토벌 ≈ 15계절(약 4년), 김억·토벌 15~19, 김총·회유 23~27, 김억·회유 31~51. 막힘·예외·저장 불일치 없음.
