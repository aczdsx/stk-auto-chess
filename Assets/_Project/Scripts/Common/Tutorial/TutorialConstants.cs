/// <summary>
/// 튜토리얼 관련 상수 및 Enum 정의
/// - 인게임 튜토리얼: stage_id와 동일한 ID 사용
/// - 아웃게임 튜토리얼: ID 20000번대 사용
/// </summary>
public static class TutorialConstants
{
    /// <summary>
    /// 아웃게임 튜토리얼 ID 구분용 오프셋 (20000 이상이면 아웃게임)
    /// </summary>
    public const int OUTGAME_TUTORIAL_ID_OFFSET = 20000;

    /// <summary>
    /// 아웃게임 튜토리얼 ID인지 확인
    /// </summary>
    public static bool IsOutgameTutorial(int tutorialId) => tutorialId >= OUTGAME_TUTORIAL_ID_OFFSET;

    /// <summary>
    /// 챕터1 아웃게임 튜토리얼 시퀀스
    /// 순서대로 진행됨
    /// </summary>
    public enum Chapter1Tutorial
    {
        None = 0,
        HubbleIntro = 20001,      // 허블 연출
        Observation10 = 20002,    // 관측 10회
        DormitoryRepair = 20003,  // 숙소 복구
        UnitGrowth = 20004,       // 유닛 성장
    }

    /// <summary>
    /// Chapter1Tutorial의 첫 번째 튜토리얼
    /// </summary>
    public const Chapter1Tutorial CHAPTER1_FIRST = Chapter1Tutorial.HubbleIntro;

    /// <summary>
    /// Chapter1Tutorial의 마지막 튜토리얼
    /// </summary>
    public const Chapter1Tutorial CHAPTER1_LAST = Chapter1Tutorial.UnitGrowth;

    /// <summary>
    /// 다음 챕터1 튜토리얼 가져오기
    /// </summary>
    public static Chapter1Tutorial GetNextChapter1Tutorial(Chapter1Tutorial current)
    {
        return current switch
        {
            Chapter1Tutorial.None => Chapter1Tutorial.HubbleIntro,
            Chapter1Tutorial.HubbleIntro => Chapter1Tutorial.Observation10,
            Chapter1Tutorial.Observation10 => Chapter1Tutorial.DormitoryRepair,
            Chapter1Tutorial.DormitoryRepair => Chapter1Tutorial.UnitGrowth,
            Chapter1Tutorial.UnitGrowth => Chapter1Tutorial.None, // 완료
            _ => Chapter1Tutorial.None
        };
    }
}
