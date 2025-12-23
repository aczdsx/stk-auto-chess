using UnityEngine;

namespace CookApps.TeamBattle.UIManagements
{
    public class UIManagementsConst : IUIManagementsConst
    {
        private float INCH_TO_CM = 2.54f;
        private float DRAG_THRESHORD_CM = 0.25f;

        public int DragThreshold => (int) (DRAG_THRESHORD_CM * Screen.dpi / INCH_TO_CM);

        public static IUIManagementsConst Default { get; set; } = new UIManagementsConst();
    }

    public interface IUIManagementsConst
    {
        int DragThreshold { get; }
    }
}
