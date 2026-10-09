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
        public string capitalId;
        public int year;
        public int season; // 0=봄 1=여름 2=가을 3=겨울
        public int turnNumber = 1;
        public List<string> completedObjectiveIds = new();
        public string pendingEventId;
        public string eventTarget;
        public string lastSubmittedId;
        public int conquestCount;

        public List<ProvinceState> provinces = new();
        public List<FactionState> factions = new();
        public List<RetainerState> retainers = new();

        public List<string> flags = new();
        public List<string> firedEventIds = new();
        // JsonUtility는 Dictionary를 못 쓰므로 키/값 리스트 두 개로 나눠 저장한다
        public List<string> lastFiredEventIds = new();
        public List<int> lastFiredTurns = new();
        public List<LogEntry> log = new();

        public long savedAtUnixSeconds;
    }
}
