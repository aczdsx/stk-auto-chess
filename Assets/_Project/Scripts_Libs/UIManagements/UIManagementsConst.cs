using UnityEngine;

namespace CookApps.TeamBattle.UIManagements
{
    public static class UIManagementsConst
    {
        public static float UIDefault_EnterDuration { get; set; } = 0.5f;
        public static float UIDefault_ExitDuration { get; set; } = 0.3f;

        public static float UIDefault_InDistance { get; set; } = 75f;
        public static float UIDefault_OutDistance { get; set; } = 75f;

        static float INCH_TO_CM = 2.54f;
        static float DRAG_THRESHORD_CM = 0.25f;

        public static int DragThreshold
        {
            get => (int) (DRAG_THRESHORD_CM * Screen.dpi / INCH_TO_CM);
        }
    }
}
