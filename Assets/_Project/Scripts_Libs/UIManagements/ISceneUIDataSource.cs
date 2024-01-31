using System.Collections.Generic;

namespace CookApps.TeamBattle.UIManagements
{
    public interface ISceneUIDataSource
    {
        public Dictionary<string, SceneUILayerManager.SceneData> SceneDataList { get; }
        public Dictionary<string, SceneUILayerManager.UILayerData> UIDataList { get; }
    }
}
