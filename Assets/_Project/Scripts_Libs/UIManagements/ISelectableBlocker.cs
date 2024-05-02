namespace CookApps.TeamBattle.UIManagements
{
    /// <summary>
    /// PGButton에서 클릭 가능한 버튼들을 선별하는 인터페이스
    /// </summary>
    public interface ISelectableBlocker
    {
        bool IsAllowSelectable(string selectableName);
        void OnClicked(string selectableName);

        int GetPriority();
    }
}
