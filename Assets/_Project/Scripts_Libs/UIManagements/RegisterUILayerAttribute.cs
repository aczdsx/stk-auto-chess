using System;
using CookApps.TeamBattle.Utility;

namespace CookApps.TeamBattle.UIManagements
{
    public class RegisterUILayerAttribute : Attribute
    {
        public UILayerType LayerType { get; }
        public string AddressableName { get; }

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
}
