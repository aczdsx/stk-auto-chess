namespace CookApps.TeamBattle.UIManagements
{
    public sealed partial class SceneUILayerManager : ISelectableBlocker
    {
        #region ISelectableBlocker
        bool ISelectableBlocker.IsAllowSelectable(string selectableName)
        {
            // 띄울 유아이가 있을 때 누르는 버튼 차단
            if (isLoadingUI)
            {
                return false;
            }

            if (isSceneChanging)
            {
                return false;
            }

            // 유아이가 뜨거나 닫히고 있다면 버튼 차단
            for (var i = 0; i < uiLayerStacks.Count; i++)
            {
                if (uiLayerStacks[i].State == UILayerState.Entering || uiLayerStacks[i].State == UILayerState.Exiting)
                {
                    return false;
                }
            }

            return true;
        }

        void ISelectableBlocker.OnClicked(string selectableName)
        {
        }

        int ISelectableBlocker.GetPriority()
        {
            return 0;
        }
        #endregion
    }
}
