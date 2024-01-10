using System.Collections.Generic;

namespace CookApps.TeamBattle.UIManagements
{
    public interface ISceneUIDataSource
    {
        public Dictionary<string, SceneUIManager.SceneData> SceneDataList { get; }
        public Dictionary<string, SceneUIManager.UIData> UIDataList { get; }
    }
}
