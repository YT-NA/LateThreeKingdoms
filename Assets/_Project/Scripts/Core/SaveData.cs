using System;
using System.Collections.Generic;

namespace Husamguk.Core
{
    [Serializable]
    public class SaveData
    {
        public int version = GameVersion.Number;
        public string scenarioId;
        public string playerFactionId;
        public int year;
        public int season; // 0=봄 1=여름 2=가을 3=겨울

        public List<ProvinceState> provinces = new();
        public List<FactionState> factions = new();
        public List<RetainerState> retainers = new();

        public long savedAtUnixSeconds;
    }
}
