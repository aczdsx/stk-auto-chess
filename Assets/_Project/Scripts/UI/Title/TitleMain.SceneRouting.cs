using CookApps.AutoBattler.Prologue;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    public partial class TitleMain
    {
        /// <summary>
        /// 튜토리얼 진행도에 따른 씬 전환 분기 처리
        /// 1. 첫 스테이지 미클리어 → 프롤로그
        /// 2. 1챕터 중간 → 못 깬 첫 스테이지로 직행
        /// 3. 1챕터 완료 → 로비
        /// </summary>
        private async UniTask RouteToNextSceneAsync()
        {
            // 튜토리얼(1챕터) 진입 분기 로직
            var firstStageData = SpecDataManager.Instance.GetStageList(1, DifficultyType.NORMAL)?[0];
            if (ServerDataManager.Instance.Battle.CurrentChapterId <= 1)
            {
                // 튜토리얼(1챕터) 진입 분기 로직
                var lastTutoStageData = SpecDataManager.Instance.GetLastStageData(1, DifficultyType.NORMAL);

                // 1. 첫 스테이지를 못 깬 경우 → 프롤로그 진행 (현재 비활성화 — 프롤로그 스킵)
                // if (firstStageData != null && ServerDataManager.Instance.Battle.IsStageCleared((uint)firstStageData.stage_id) == false)
                // {
                //     SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
                //     await SceneTransition.FadeInAsync();
                //
                //     var inGameParams = new InGameMainParams(InGameType.PROLOGUE, new InGameMainStatePrologue(), 0);
                //     SceneLoading.GoToNextSceneWithSpecialTrigger("InGame", "PrologueStart", inGameParams);
                //     return;
                // }

                // 2. 1챕터 마지막 스테이지를 못 깬 경우 → 못 깬 첫 스테이지로 바로 진입
                if (lastTutoStageData != null && ServerDataManager.Instance.Battle.IsStageCleared((uint)lastTutoStageData.stage_id) == false)
                {
                    // 1챕터에서 못 깬 첫 스테이지 찾기
                    var stageList = SpecDataManager.Instance.GetStageList(1, DifficultyType.NORMAL);
                    StageInfo unclearedStage = null;
                    foreach (var stage in stageList)
                    {
                        if (ServerDataManager.Instance.Battle.IsStageCleared((uint)stage.stage_id) == false)
                        {
                            unclearedStage = stage;
                            break;
                        }
                    }

                    if (unclearedStage != null)
                    {
                        SceneTransition.Create<SceneTransition_FadeInOut>();
                        await SceneTransition.FadeInAsync();

                        var inGameParams = new InGameMainParams(InGameType.STAGE, new InGameMainStateStage(), unclearedStage.stage_id);
                        // var progressData = ClientProgressData.Get();
                        // if (unclearedStage.stage_id == 10003 && !progressData.hasNicknameSet)
                        // {
                        //     SceneLoading.GoToNextSceneViaNaninovelScript("InGame_New", "Chapter0_04", inGameParams);
                        // }
                        // else
                        {
                            SceneLoading.GoToNextScene("InGame_New", inGameParams);
                        }
                        return;
                    }
                }
            }

            // 3. 1챕터 모두 클리어 → 로비로 진입
            {
                SceneTransition.Create<SceneTransition_FadeInOut>();
                await SceneTransition.FadeInAsync();

                var lastStageID = (int)LocalDataManager.Instance.GetLastPlayStageId();
                var specStageData = SpecDataManager.Instance.GetStageData(lastStageID);
                SceneLoading.GoToNextScene("Lobby", specStageData.chapter_id);
            }
        }
    }
}
