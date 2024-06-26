using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Overlay, "Prefabs/UI/Title/TitleMain.prefab")]
    public class TitleMain : UILayer
    {
        public static int SessionCount { get; private set; }

        private Dictionary<int, float> progressDict = new ();

        [SerializeField] private GameObject touchToStart;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            SessionCount++;
            progressDict.Clear();
            touchToStart.SetActive(false);
            RunAllTasks().Forget();

            // bgm on
            SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_splash_001);
        }

        private async UniTask RunAllTasks()
        {
            await UniTask.NextFrame();
            // get list of all ITitleTask implementations in this assembly
            Type[] types = InheritHelper.GetAllImplementations<ITitleTask>();
            var allTasks = new List<ITitleTask>();

            foreach (Type type in types)
            {
                var titleTask = Activator.CreateInstance(type) as ITitleTask;
                if (titleTask == null)
                {
                    continue;
                }

                allTasks.Add(titleTask);
                progressDict.Add(titleTask.GetHashCode(), 0);
            }

            // run tasks in order
            for (var priority = ITitleTaskPriority.Step_0; priority < ITitleTaskPriority.MAX; priority++)
            {
                ITitleTaskPriority p = priority;
                ITitleTask[] tasks = allTasks.Where(x => x.Priority == p).ToArray();
                if (tasks.Length == 0)
                {
                    continue;
                }

                await RunSubTasks(tasks);

                touchToStart.SetActive(true);
            }
        }

        private async UniTask RunSubTasks(ITitleTask[] tasks)
        {
            foreach (ITitleTask task in tasks)
            {
                task.Initialize(this, UpdateProgress);
            }

            await UniTask.WhenAll(
                tasks.Select(x => x.RunTask())
            );

            foreach (ITitleTask task in tasks)
            {
                (bool, string) error = task.HasError();
                if (error.Item1)
                {
                    await task.HandleError();
                }
            }
        }

        private void UpdateProgress(int hashCode, float progress)
        {
            progressDict[hashCode] = progress;
            float totalProgress = progressDict.Values.Sum() / progressDict.Count;
            // TODO: update progress bar
        }

        public void OnClickTouchToStart()
        {
            // if (LocalUserDataManager.Instance.GetIsFirstGame() == true)
            // {
            //     LocalUserDataManager.Instance.SaveFirstGame();
            //     SceneUILayerManager.Instance.ChangeScene("Intro", null);
            // }
            // else
            {
                //var transition = SceneTransition_FadeInOut.Create();
                // [TODO] lastChapter에 로비에 진입할 챕터 넣어주세요.

                // 초반 플로우 체크 및 진행
                var lastTutoStageData = SpecDataManager.Instance.GetLastStageData(1, DifficultyType.NORMAL);
                if (UserDataManager.Instance.IsClearStage(lastTutoStageData.stage_id) == false)
                {
                    int lastStageID = UserDataManager.Instance.GetLastPlayStageID();
                    var specStageData = SpecDataManager.Instance.GetStageData(lastStageID);
                    if (UserDataManager.Instance.IsClearStage(lastStageID))
                    {
                        specStageData = SpecDataManager.Instance.GetNextStageData(lastStageID);
                    }

                    SceneLoading.GoToNextScene("InGame", ((int)specStageData.chapter_id, (int)specStageData.stage_number, DifficultyType.NORMAL)).Forget();
                }
                else
                {
                    var transition = SceneTransition_FadeInOut.Create();

                    int lastChapterID = UserDataManager.Instance.GetLastPlayStageID();
                    var specStageData = SpecDataManager.Instance.GetStageData(lastChapterID);
                    SceneLoading.GoToNextScene("Lobby", (int)specStageData.chapter_id, transition).Forget();
                }

                // int lastChapter = 1;
                // SceneLoading.GoToNextScene("Lobby", lastChapter, transition).Forget();
            }
        }
    }
}
