using UnityEngine;

namespace Husamguk.Data
{
    public enum AmbitionTrait { Loyal, Cautious, Ambitious, Ruthless }

    /// <summary>
    /// 신하(장수·문관·왕자 통칭) 하나의 정의. GDD §7 — 삼국지류 5스탯(통솔·무력·지력·정치·매력)에
    /// 학문·덕망을 더한 7스탯. 역할별로 일부만 쓰여도 된다 (장수는 무력·통솔, 문관은 정치·학문 등).
    /// </summary>
    [CreateAssetMenu(menuName = "Husamguk/Retainer", fileName = "Retainer_")]
    public class RetainerDef : ScriptableObject
    {
        [Tooltip("저장 파일에 쓰이는 고유 ID. 한 번 정하면 바꾸지 말 것.")]
        public string id;
        public string displayName;
        [Tooltip("한자 표기 (선택)")]
        public string hanjaName;
        public Sprite portrait;

        [Header("7스탯")]
        [Range(0, 100)] public int leadership = 50;  // 통솔
        [Range(0, 100)] public int might = 50;        // 무력
        [Range(0, 100)] public int intellect = 50;    // 지력
        [Range(0, 100)] public int scholarship = 50;  // 학문
        [Range(0, 100)] public int politics = 50;     // 정치
        [Range(0, 100)] public int charm = 50;        // 매력
        [Range(0, 100)] public int virtue = 50;        // 덕망

        [Header("개성 (§5장)")]
        [Tooltip("출신 호족 id (해당 호족 가문 출신이면). 없으면 빈 값")]
        public string originHojokId;
        public AmbitionTrait ambition = AmbitionTrait.Loyal;

        [Header("왕족 (해당될 때만)")]
        public bool isHeir;
        [Tooltip("출생 순서. 1 = 장남/장녀")]
        [Min(0)] public int birthOrder;

        public int StatTotal => leadership + might + intellect + scholarship + politics + charm + virtue;
    }
}
