using Naninovel;
using Naninovel.UI;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// DialogueLogButton을 통해서만 열리도록 제한하는 커스텀 BacklogPanel
    /// - ShowBacklog 키/스와이프 입력 바인딩 제거
    /// - Show()는 ShowFromButton()을 통해서만 동작
    /// </summary>
    public class CustomBacklogPanel : BacklogPanel
    {
        private bool _allowShow;

        public override UniTask Initialize()
        {
            // ShowBacklog 입력 바인딩하지 않음 (base.Initialize() 호출 X)
            // Cancel 키로 닫기만 유지
            BindInput(InputNames.Cancel, Hide, new CustomUI.BindInputOptions { OnEnd = true });

            // ShowBacklog 입력 sampler 비활성화
            var inputManager = Engine.GetService<IInputManager>();
            var showBacklogSampler = inputManager?.GetSampler(InputNames.ShowBacklog);
            if (showBacklogSampler != null)
            {
                showBacklogSampler.Enabled = false;
            }

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// DialogueLogButton에서 호출하는 명시적 Show
        /// </summary>
        public void ShowFromButton()
        {
            _allowShow = true;
            Show();
            _allowShow = false;
        }

        public override void Show()
        {
            if (!_allowShow) return;
            base.Show();
        }
    }
}
