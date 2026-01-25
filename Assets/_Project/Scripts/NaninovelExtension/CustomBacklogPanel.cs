using Naninovel;
using Naninovel.UI;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// DialogueLogButton을 통해서만 열리도록 제한하는 커스텀 BacklogPanel
    /// - ShowBacklog 키 입력 바인딩 제거
    /// - Show() 호출 시 디버그 로그 출력
    /// </summary>
    public class CustomBacklogPanel : BacklogPanel
    {
        public override UniTask Initialize()
        {
            // 기본 BacklogPanel은 여기서 ShowBacklog 키 입력을 바인딩하지만,
            // 우리는 DialogueLogButton을 통해서만 열리도록 하기 위해 바인딩하지 않음

            // Cancel 키로 닫기만 유지
            BindInput(InputNames.Cancel, Hide, new CustomUI.BindInputOptions { OnEnd = true });

            // ShowBacklog 입력 자체를 비활성화 (L키, 스와이프 업 등)
            var inputManager = Engine.GetService<IInputManager>();
            var showBacklogSampler = inputManager?.GetSampler(InputNames.ShowBacklog);
            if (showBacklogSampler != null)
            {
                showBacklogSampler.Enabled = false;
            }

            return UniTask.CompletedTask;
        }

        public override void Show()
        {
            Debug.Log($"[CustomBacklogPanel] Show() 호출됨\n{System.Environment.StackTrace}");
            base.Show();
        }
    }
}
