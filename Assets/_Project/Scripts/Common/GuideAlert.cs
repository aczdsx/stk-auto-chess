using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class GuideAlert : MonoBehaviour
    {
        private const float GUIDE_ALERT_TIME = 2.0f;    // 가이드 알림 노출 시간

        [SerializeField] private bool _useOnEnable = true;
        [SerializeField] private GuideMissionType _guideMissionType;

        public int TargetSubKey { get; set; } = 0;

        [Space(10)]
        [SerializeField] private GameObject _guideAlertObject;

        private bool _isPlayingGuideAlert = false;

        private void OnEnable()
        {
            if (_useOnEnable == false) return;
            
            if (GuideMissionManager.Instance != null)
            {
                GuideMissionManager.Instance.OnGuideAlertUpdated += UpdateAlert;
            }

            UpdateAlert();
        }

        private void OnDestroy()
        {
            if (GuideMissionManager.Instance != null)
            {
                GuideMissionManager.Instance.OnGuideAlertUpdated -= UpdateAlert;
            }
        }

        public void InitAlert()
        {
            gameObject.SetActive(true);
            
            UpdateAlert();
        }
        
        public void InitAlertWithSubKey(int subKey)
        {
            TargetSubKey = subKey;

            InitAlert();
        }

        private void UpdateAlert()
        {
            if (_guideAlertObject == null) return;
            if (_isPlayingGuideAlert) return;

            _guideAlertObject.SetActive(false);

            var userGuideMissionData = UserDataManager.Instance.GetCurrentGuideMissionData();
            if (userGuideMissionData == null) return;
            
            var specGuideMissionData = SpecDataManager.Instance.GetGuideMissionDataByOrder(userGuideMissionData.MissionId);

            if (specGuideMissionData != null)
            {
                bool isValidType = specGuideMissionData.guide_mission_type == _guideMissionType;
                bool isValidState = userGuideMissionData.MissionStateType != (int)MissionStateType.REWARD && userGuideMissionData.MissionStateType != (int)MissionStateType.CLEAR;
                
                bool isHaveSubKey = TargetSubKey > 0;
                bool isValidSubKey = isHaveSubKey && specGuideMissionData.sub_key == TargetSubKey;
                
                if (isValidType && isValidState)
                {
                    if (isHaveSubKey)
                    {
                        if (isValidSubKey)
                        {
                            OnAlert();    
                        }
                    }
                    else
                    {
                        OnAlert();
                    }
                }
            }
        }

        private void OnAlert()
        {
            _isPlayingGuideAlert = true;

            _guideAlertObject.SetActive(true);

            Invoke(nameof(OffAlert), GUIDE_ALERT_TIME);

            //SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_splash);
        }
        
        private void OffAlert()
        {
            _isPlayingGuideAlert = false;

            _guideAlertObject.SetActive(false);
        }
    }
}
