using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookApps.TeamBattle.Utility
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        // private const string SPEC_OPTION_SAFE_AREA_MARGIN_RATIO_KEY = "SAFE_AREA_MARGIN_RATIO";
        [SerializeField] private bool isControlTop = true;
        [SerializeField] private bool isControlBottom = true;
        [SerializeField] private bool isControlLeft = true;
        [SerializeField] private bool isControlRight = true;

        private Rect processedSafeArea;
        private Vector2 processedResolution;

        private static (float left, float right, float top, float bottom) marginRatio;
        public static (float left, float right, float top, float bottom) MarginRatio
        {
            get
            {
                if (!Mathf.Approximately(marginRatio.left, 0f))
                    return marginRatio;

                // var specData = SpecDataManager.Instance.GetOptionData(SPEC_OPTION_SAFE_AREA_MARGIN_RATIO_KEY);
                // List<float> specList = null;
                // if (specData != null)
                // {
                //     specList = specData
                //         .Split(new[] { '|' })
                //         .Select(x =>
                //         {
                //             if (!float.TryParse(x, out var margin))
                //                 margin = 0f;
                //             return margin;
                //         })
                //         .ToList();
                // }
                //
                // if (specList?.Count == 4)
                // {
                //     marginRatio.left = specList[0];
                //     marginRatio.right = specList[1];
                //     marginRatio.top = specList[2];
                //     marginRatio.bottom = specList[3];
                // }
                // else
                {
                    marginRatio.left = 1f;
                    marginRatio.right = 1f;
                    marginRatio.top = 0.5f;
                    marginRatio.bottom = 0.5f;
                }

                return marginRatio;
            }
        }

        private void Awake()
        {
            var rectTr = GetComponent<RectTransform>();
            var safeArea = Screen.safeArea;
            var resolution = Screen.fullScreen ? new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) : new Vector2(Screen.width, Screen.height);

            if (processedSafeArea == safeArea && processedResolution == resolution)
                return;
            processedSafeArea = safeArea;
            processedResolution = resolution;

            var leftAnchorDiff = safeArea.x / resolution.x * MarginRatio.left;
            var bottomAnchorDiff = safeArea.y / resolution.y * MarginRatio.bottom;

            rectTr.anchorMin = new Vector2(
                isControlLeft ? leftAnchorDiff: rectTr.anchorMin.x,
                isControlBottom ? bottomAnchorDiff : rectTr.anchorMin.y
            );

            var rightAnchorDiff = (1f - ((safeArea.x + safeArea.width) / resolution.x)) * MarginRatio.right;
            var topAnchorDiff = (1f - ((safeArea.y + safeArea.height) / resolution.y)) * MarginRatio.top;

            rectTr.anchorMax = new Vector2(
                isControlRight ? 1f - rightAnchorDiff : rectTr.anchorMax.x,
                isControlTop ? 1f - topAnchorDiff : rectTr.anchorMax.y
            );
        }
    }
}
