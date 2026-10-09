@echo off
chcp 65001 >nul
rem 후삼국통일 프로젝트를 Unity Hub 없이 바로 연다.
rem 설치된 Unity 6(6000.x) 에디터를 찾아 LateThreeKingdoms 폴더를 프로젝트로 연다.

set "PROJ=%~dp0"
if "%PROJ:~-1%"=="\" set "PROJ=%PROJ:~0,-1%"
set "UNITY="

for %%R in ("C:\Program Files\Unity\Hub\Editor" "D:\Program Files\Unity\Hub\Editor" "C:\Program Files\Unity Hub\Editor" "D:\Unity\Hub\Editor" "C:\Unity\Hub\Editor") do (
    for /d %%D in ("%%~R\6000*") do (
        if exist "%%D\Editor\Unity.exe" set "UNITY=%%D\Editor\Unity.exe"
    )
)

if not defined UNITY (
    echo.
    echo Unity 6 에디터를 찾지 못했습니다.
    echo Unity Hub 왼쪽 [설치] 메뉴에서 Unity 6 을 설치한 뒤 다시 실행하세요.
    echo 다른 위치에 설치했다면 그 경로를 Claude 에게 알려 주세요.
    echo.
    pause
    exit /b 1
)

echo 여는 중: "%UNITY%"
echo 프로젝트: "%PROJ%"
echo 처음에는 몇 분 걸립니다. 이 창은 닫아도 됩니다.
start "" "%UNITY%" -projectPath "%PROJ%"
