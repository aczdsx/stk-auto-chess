using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 튜토리얼에서 타겟으로 지정할 오브젝트에 붙이는 컴포넌트.
    /// Inspector에서 TargetId를 설정하거나, 런타임에 SetTargetId()로 설정합니다.
    /// </summary>
    public class TutorialTarget : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("튜토리얼에서 이 오브젝트를 찾을 때 사용하는 고유 ID (동적 생성 시 비워두고 SetTargetId 사용)")]
        private string _targetId;

        private bool _isRegistered;

        public string TargetId => _targetId;

        /// <summary>
        /// 런타임에 TargetId를 설정합니다. (동적 생성 오브젝트용)
        /// 기존 ID가 있으면 해제 후 새 ID로 등록합니다.
        /// </summary>
        /// <param name="newId">새로운 타겟 ID</param>
        public void SetTargetId(string newId)
        {
            // 기존 등록 해제
            if (_isRegistered && !string.IsNullOrEmpty(_targetId))
            {
                TutorialTargetRegistry.Unregister(this);
                _isRegistered = false;
            }

            _targetId = newId;

            // 새 ID로 등록 (오브젝트가 활성화 상태일 때만)
            if (gameObject.activeInHierarchy && !string.IsNullOrEmpty(_targetId))
            {
                Debug.LogColor($"TutorialTarget 등록: {_targetId}", "green");
                TutorialTargetRegistry.Register(this);
                _isRegistered = true;
            }
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(_targetId) && !_isRegistered)
            {
                TutorialTargetRegistry.Register(this);
                _isRegistered = true;
            }
        }

        private void OnDisable()
        {
            if (_isRegistered)
            {
                TutorialTargetRegistry.Unregister(this);
                _isRegistered = false;
            }
        }

        private void OnDestroy()
        {
            if (_isRegistered)
            {
                TutorialTargetRegistry.Unregister(this);
                _isRegistered = false;
            }
        }
    }
}
