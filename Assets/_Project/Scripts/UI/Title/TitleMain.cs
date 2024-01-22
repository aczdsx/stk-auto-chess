using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.SampleTeamBattle
{
    public class TitleMain : UIBase
    {
        public static int SessionCount { get; private set; }

        private Dictionary<int, float> progressDict = new ();

        [SerializeField] private GameObject touchToStart;

        public override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            SessionCount++;
            progressDict.Clear();
            touchToStart.SetActive(false);
            RunAllTasks().Forget();
        }

        private async UniTask RunAllTasks()
        {
            await UniTask.NextFrame();
            // get list of all ITitleTask implementations in this assembly
            Type[] types = InterfaceHelper.GetAllImplementations<ITitleTask>();
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
            //     SceneUIManager.Instance.ChangeScene("Intro", null);
            // }
            // else
            {
                var transition = SceneTransition_FadeInOut.Create();
                SceneLoading.GoToNextScene("Lobby", null, transition).Forget();
            }
        }
    }
}
