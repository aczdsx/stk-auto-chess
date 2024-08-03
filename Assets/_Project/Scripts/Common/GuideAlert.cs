using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class GuideAlert : MonoBehaviour
    {
        private const float GUIDE_ALERT_TIME = 2.0f;    // 가이드 알림 노출 시간

        [SerializeField] private GuideMissionType _guideMissionType;

        [Space(10)]
        [SerializeField] private GameObject _guideAlertObject;

        private bool _isPlayingGuideAlert = false;

        private void Start()
        {
            if (GuideMissionManager.Instance != null)
            {
                GuideMissionManager.Instance.OnGuideAlertUpdated += UpdateAlert;
            }

            bool isActive = false;
            var userGuideMissionData = UserDataManager.Instance.GetCurrentGuideMissionData();
            if (userGuideMissionData.MissionStateType == (int)MissionStateType.NONE)
            {
                var specGuideMissionData = SpecDataManager.Instance.SpecGuideMission.Get(userGuideMissionData.MissionId);
                if (specGuideMissionData != null && specGuideMissionData.guide_mission_type == _guideMissionType)
                {
               
                    isActive = true;
                }
            }

            _guideAlertObject.SetActive(isActive);
        }

        private void OnDestroy()
        {
            if (GuideMissionManager.Instance != null)
            {
                GuideMissionManager.Instance.OnGuideAlertUpdated -= UpdateAlert;
            }
        }

        private void UpdateAlert()
        {
            if (_guideAlertObject == null) return;
            if (_isPlayingGuideAlert) return;

            _guideAlertObject.SetActive(false);

            var currentGuideMissionData = UserDataManager.Instance.GetCurrentGuideMissionData();
            var specGuideMissionData = SpecDataManager.Instance.GetGuideMissionDataByOrder(currentGuideMissionData.MissionId);

            if (specGuideMissionData != null)
            {
                if (specGuideMissionData.guide_mission_type == _guideMissionType)
                {
                    _isPlayingGuideAlert = true;

                    _guideAlertObject.SetActive(true);

                    Invoke(nameof(OffAlert), GUIDE_ALERT_TIME);

                    //SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_splash);
                }
            }
        }

        private void OffAlert()
        {
            _isPlayingGuideAlert = false;

            _guideAlertObject.SetActive(false);
        }
    }
}
