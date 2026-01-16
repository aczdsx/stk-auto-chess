using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class GuideAlert : CachedMonoBehaviour, IRegistrable
    {
        private const float GUIDE_ALERT_TIME = 2.0f;    // 가이드 알림 노출 시간

        [SerializeField] private bool _useOnEnable = true;
        [SerializeField] private GuideMissionType _guideMissionType;

        public int TargetSubKey { get; set; } = 0;

        [Space(10)]
        [SerializeField] private GameObject _guideAlertObject;

        private bool _isPlayingGuideAlert = false;

        #region IRegistrable
        
        public RegistryKey Key => RegistryKey.GuideAlert;
        private void Awake()
        {
            ObjectRegistry.Register(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ObjectRegistry.Unregister(this);
        }
        
        #endregion

        private void OnEnable()
        {
            UpdateAlert();
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

        public void UpdateAlert()
        {
            if (_guideAlertObject == null) return;
            if (_isPlayingGuideAlert) return;

            _guideAlertObject.SetActive(false);

            var guideMission = ServerDataManager.Instance.GuideMission;
            if (guideMission.Data == null) return;

            var specGuideMissionData = SpecDataManager.Instance.GetGuideMissionDataByOrder((int)guideMission.GuideMissionId);

            if (specGuideMissionData != null)
            {
                bool isValidType = specGuideMissionData.guide_mission_type == _guideMissionType;
                bool isValidState = !guideMission.CanClaimReward && !guideMission.IsCompleted;
                
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
