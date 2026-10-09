@echo off
chcp 65001 >nul
rem 후삼국통일 실행 파일(.exe)을 만들고 바로 켠다. Unity 창은 뜨지 않는다.
rem 결과: C:\Users\sjart\OneDrive\바탕 화면\후삼국시대\v번호\후삼국통일.exe (경로는 last_build_husamguk.txt)  /  기록: build_husamguk.log

set "ROOT=%~dp0"
set "PROJ=%ROOT%"
if "%PROJ:~-1%"=="\" set "PROJ=%PROJ:~0,-1%"
set "LAST=%ROOT%last_build_husamguk.txt"
set "EXE="
set "LOG=%ROOT%build_husamguk.log"
set "UNITY="

for %%R in ("C:\Program Files\Unity\Hub\Editor" "D:\Program Files\Unity\Hub\Editor" "C:\Program Files\Unity Hub\Editor" "D:\Unity\Hub\Editor" "C:\Unity\Hub\Editor") do (
    for /d %%D in ("%%~R\6000*") do (
        if exist "%%D\Editor\Unity.exe" set "UNITY=%%D\Editor\Unity.exe"
    )
)

if not defined UNITY (
    echo Unity 6 에디터를 찾지 못했습니다. Unity Hub [설치]에서 Unity 6 을 설치하세요.
    pause
    exit /b 1
)

if exist "%PROJ%\Temp\UnityLockfile" (
    echo Unity 창이 열려 있으면 먼저 닫아 주세요. (같은 버전 게임도 꺼 주세요) 닫았으면 아무 키나 누르세요.
    pause >nul
)

echo 게임을 만드는 중입니다. 처음에는 5~15분 걸릴 수 있어요. 이 창을 닫지 마세요...
if exist "%LAST%" del /q "%LAST%"
start "" /wait "%UNITY%" -batchmode -quit -projectPath "%PROJ%" -executeMethod Husamguk.EditorTools.BuildScript.BuildWindowsBatch -logFile "%LOG%"

if exist "%LAST%" set /p EXE=<"%LAST%"
if defined EXE if exist "%EXE%" (
    echo.
    echo 완료! 게임을 켭니다.  다음부터는 아래 파일을 바로 실행해도 됩니다.
    echo %EXE%
    start "" "%EXE%"
    timeout /t 5 >nul
    exit /b 0
)

echo.
echo 만들기에 실패했습니다. 아래 오류 줄을 캡처해서 Claude 에게 보내 주세요.
echo ------------------------------------------------------------
findstr /i /l /c:"[BuildScript]" /c:"error CS" /c:"Exception" /c:"Build Finished, Result" /c:"Aborting batchmode" "%LOG%"
echo ------------------------------------------------------------
echo 전체 기록: %LOG%
pause
exit /b 1
