using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 전투 연출(BATTLE START / VICTORY / DEFEAT) 공통 베이스.
    /// Animator StartEnter 완료 시 자동 Pop + UniTask 완료 신호.
    /// </summary>
    public class BattleCutsceneUIBase : UILayer
    {
        private UniTaskCompletionSource _animationTcs;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            _animationTcs = new UniTaskCompletionSource();
        }

        protected override void OnPostEnter()
        {
            base.OnPostEnter();
            // StartEnter 애니메이션 완료 신호
            _animationTcs?.TrySetResult();
            // OnPostEnter 내에서 바로 Pop하면 OnEndEnterAnimation의 uiLayerStacks 인덱스가 깨짐
            // 다음 프레임에서 Pop
            PopNextFrameAsync().Forget();
        }

        private async UniTaskVoid PopNextFrameAsync()
        {
            await UniTask.Yield();
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        /// <summary>연출 완료까지 대기</summary>
        public UniTask WaitForAnimationCompleteAsync()
        {
            return _animationTcs?.Task ?? UniTask.CompletedTask;
        }
    }
}
