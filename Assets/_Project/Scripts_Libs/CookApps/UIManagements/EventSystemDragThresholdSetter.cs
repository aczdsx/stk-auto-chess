using UnityEngine;
using UnityEngine.EventSystems;

namespace CookApps.TeamBattle.UIManagements
{
    [RequireComponent(typeof(EventSystem))]
    public class EventSystemDragThresholdSetter : MonoBehaviour
    {
        [SerializeField] private EventSystem eventSystem;
        
        private void Awake()
        {
            eventSystem.pixelDragThreshold = UIManagementsConst.Default.DragThreshold;
        }
    }
}
