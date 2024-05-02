using System.Collections.Generic;

namespace CookApps.TeamBattle.UIManagements
{
    public class SelectableBlockerManager : Singleton<SelectableBlockerManager>
    {
        private List<ISelectableBlocker> blockers = new List<ISelectableBlocker>();

        public void AddBlocker(ISelectableBlocker blocker)
        {
            if (blockers.Contains(blocker))
                return;
            blockers.Add(blocker);
            blockers.Sort((x, y) => x.GetPriority() - y.GetPriority());
        }

        public void RemoveBlocker(ISelectableBlocker blocker)
        {
            blockers.Remove(blocker);
        }

        public bool IsAllowSelectable(string selectableName)
        {
            for (var i = 0; i < blockers.Count; i++)
            {
                if (!blockers[i].IsAllowSelectable(selectableName))
                {
                    return false;
                }
            }

            return true;
        }

        public void OnClicked(string selectableName)
        {
            for (var i = 0; i < blockers.Count; i++)
            {
                blockers[i].OnClicked(selectableName);
            }
        }
    }
}
