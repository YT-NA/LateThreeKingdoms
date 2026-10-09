using System.IO;
using Husamguk.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Husamguk.EditorTools
{
    /// <summary>
    /// Windows 실행 파일 만들기. 샘플 세팅(데이터·씬)을 새로 한 뒤 사용자가 지정한 바탕화면 폴더
    /// (OutputDir\{GameVersion.Version}\후삼국통일.exe)로 빌드한다.
    /// - 메뉴 [Husamguk > Windows 실행 파일 만들기]
    /// - 후삼국_만들기.bat → Unity를 창 없이(-batchmode) 실행해 BuildWindowsBatch를 부른다
    /// 빌드가 끝나면 exe 경로를 저장소 최상위 last_build_husamguk.txt에 적는다 (bat가 읽어서 실행).
    /// (RuleTheKingdom의 last_build.txt와 같은 저장소를 쓰므로 파일 이름을 분리했다.)
    /// 세이브 파일은 실행 파일과 같은 폴더에 생긴다 (Core/SaveSystem.cs).
    /// </summary>
    public static class BuildScript
    {
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        const string ExeName = "후삼국통일.exe";

        static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        /// <summary>클라우드에서 개발하고 실행 파일만 내려받아 테스트하는 사용자가 지정한 폴더.</summary>
        public static string OutputDir => Path.Combine(@"C:\Users\sjart\OneDrive\바탕 화면\후삼국시대", GameVersion.Version);

        [MenuItem("Husamguk/Windows 실행 파일 만들기")]
        public static void BuildWindowsMenu()
        {
            bool ok = BuildWindows();
            EditorUtility.DisplayDialog("Husamguk", ok ? $"완료!\n{OutputDir}\\{ExeName}" : "빌드 실패. Console을 확인하세요.", "확인");
        }

        /// <summary>배치 모드 진입점. 성공하면 종료 코드 0, 실패하면 1.</summary>
        public static void BuildWindowsBatch()
        {
            bool ok = false;
            try { ok = BuildWindows(); }
            catch (System.Exception e) { Debug.LogException(e); }
            EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool BuildWindows()
        {
            // 이 버전 게임이 켜져 있으면 파일이 잠겨 폴더가 반쯤 지워진다. 아무것도 건드리기 전에 멈춘다.
            if (IsRunning(Path.Combine(OutputDir, ExeName)))
            {
                Debug.LogError($"[BuildScript] {GameVersion.Version} 게임이 켜져 있어 빌드하지 않음. 게임을 끄거나 GameVersion을 올리세요.");
                return false;
            }

            GameSetup.Silent = true;
            try { GameSetup.Run(); }
            finally { GameSetup.Silent = false; }

            PlayerSettings.companyName = "YT_NA Games";
            PlayerSettings.productName = "후삼국통일";
            PlayerSettings.bundleVersion = GameVersion.Number.ToString();
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            // 임시 폴더에 다 만든 뒤 버전 폴더로 옮긴다 (빌드가 실패해도 기존 버전 폴더는 멀쩡)
            var staging = Path.Combine(RepoRoot, "Build", "_staging");
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            Directory.CreateDirectory(staging);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(staging, ExeName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[BuildScript] {GameVersion.Version} 결과: {report.summary.result}, 오류 {report.summary.totalErrors}개 → {OutputDir}");
            if (report.summary.result != BuildResult.Succeeded) return false;

            // 배포하면 안 되는 디버그 폴더 정리
            foreach (var dir in Directory.GetDirectories(staging, "*_DoNotShip")) Directory.Delete(dir, true);

            // 같은 버전을 다시 빌드하면 기존 폴더를 바꿔 끼운다
            var exePath = Path.Combine(OutputDir, ExeName);
            if (Directory.Exists(OutputDir)) Directory.Delete(OutputDir, true);
            Directory.CreateDirectory(Path.GetDirectoryName(OutputDir));
            Directory.Move(staging, OutputDir);
            File.WriteAllText(Path.Combine(RepoRoot, "last_build_husamguk.txt"), exePath, new System.Text.UTF8Encoding(false));
            return true;
        }

        /// <summary>실행 파일이 켜져 있어 잠겨 있는지.</summary>
        static bool IsRunning(string exePath)
        {
            if (!File.Exists(exePath)) return false;
            try
            {
                using (File.Open(exePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                return false;
            }
            catch (IOException) { return true; }
            catch (System.UnauthorizedAccessException) { return true; }
        }
    }
}
