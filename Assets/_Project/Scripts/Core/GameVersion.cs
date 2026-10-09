namespace Husamguk.Core
{
    /// <summary>
    /// 게임 버전. 새 버전을 낼 때마다 숫자를 하나 올린다 (v1 → v2 → ...).
    /// 빌드는 BuildScript.OutputDir(사용자가 지정한 바탕화면 폴더)\{Version}\ 에 들어간다.
    /// </summary>
    public static class GameVersion
    {
        public const int Number = 2;
        public const string Version = "v2";
    }
}
