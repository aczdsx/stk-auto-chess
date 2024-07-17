using CookApps.BattleSystem;

namespace CookApps.AutoBattler
{
    public class InGameMainStateUIStageUI : IGameStateUI
    {
        private SpecStage _specStage;

        public void SetInGameBottomUI()
        {
        }

        public void SetInGameTopUI()
        {
        }

        public void Initialize(int id)
        {
            _specStage = SpecDataManager.Instance.GetStageData(id);
            InGameManager.Instance.StartInGame<FlowStateStageReady>(_specStage, _specStage);

            // 최근 플레이 스테이지 저장
            UserDataManager.Instance.SetLastPlayStageID(_specStage.stage_id, true);

            // 유저 레벨업 체크용 이전 레벨 데이터 저장
            UserDataManager.Instance.PrevAccountLevel = UserDataManager.Instance.UserBasicData.Level;
        }

        public void PlayBGM()
        {
            switch (_specStage.chapter_id)
            {
                case 1:
                    SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_chapter0);
                    break;
                case 2:
                    SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_chapter1);
                    break;
                case 3:
                    SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_chapter2);
                    break;
            }
        }

        public string GetStageName()
        {
            return $"스테이지 {_specStage.chapter_id}-{_specStage.stage_number}";
        }
    }
}
