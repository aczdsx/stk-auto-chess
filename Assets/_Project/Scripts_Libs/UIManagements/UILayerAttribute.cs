using System;
using System.Linq;

namespace CookApps.TeamBattle.UIManagements
{
    public class RegisterUILayerAttribute : Attribute
    {
        public UILayerType LayerType { get; }
        public string AddressableName { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="layerType"></param>
        /// <param name="addressableName"></param>
        /// <param name="sceneNamesWithUILayer">씬이 뜰 때 이 UILayer가 기본으로 떠야한다면 해당 씬 이름을 적으면 됨.</param>
        public RegisterUILayerAttribute(UILayerType layerType, string addressableName)
        {
            LayerType = layerType;
            AddressableName = addressableName;
        }
    }

    public static class RegisterUILayerAttributeHelper
    {
        public static RegisterUILayerAttribute GetAttribute(Type type)
        {
            object[] attributes = type.GetCustomAttributes(typeof(RegisterUILayerAttribute), false);
            if (attributes.Length == 0)
            {
                return null;
            }

            return attributes[0] as RegisterUILayerAttribute;
        }
    }

    public class SceneNameWithUILayerAttribute : Attribute
    {
        public string SceneName { get; }
        public Type[] SubUILayers { get; }

        public SceneNameWithUILayerAttribute(string sceneName, params Type[] subUILayers)
        {
            SceneName = sceneName;
            SubUILayers = subUILayers;
        }
    }

    public static class SceneNameWithUILayerAttributeHelper
    {
        public static SceneNameWithUILayerAttribute[] GetAttribute(Type type)
        {
            object[] attributes = type.GetCustomAttributes(typeof(SceneNameWithUILayerAttribute), false);
            if (attributes.Length == 0)
            {
                return null;
            }

            return attributes.Select(x => x as SceneNameWithUILayerAttribute).ToArray();
        }
    }
}
