using System.Collections.Generic;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// TutorialTarget 컴포넌트들을 관리하는 정적 레지스트리.
    /// 딕셔너리를 사용해 O(1)로 빠르게 검색합니다.
    /// </summary>
    public static class TutorialTargetRegistry
    {
        private static readonly Dictionary<string, TutorialTarget> _targets = new();

        /// <summary>
        /// TutorialTarget 등록 (OnEnable에서 자동 호출)
        /// </summary>
        public static void Register(TutorialTarget target)
        {
            if (target == null || string.IsNullOrEmpty(target.TargetId))
                return;

            if (_targets.ContainsKey(target.TargetId))
            {
                // 같은 ID가 이미 있으면 경고 후 덮어쓰기
                Debug.LogWarning($"[TutorialTargetRegistry] 중복 ID 등록: {target.TargetId}. 기존 타겟을 덮어씁니다.");
            }

            _targets[target.TargetId] = target;
        }

        /// <summary>
        /// TutorialTarget 등록 해제 (OnDisable/OnDestroy에서 자동 호출)
        /// </summary>
        public static void Unregister(TutorialTarget target)
        {
            if (target == null || string.IsNullOrEmpty(target.TargetId))
                return;

            // 현재 등록된 타겟이 같은 인스턴스인 경우에만 제거
            if (_targets.TryGetValue(target.TargetId, out var registered) && registered == target)
            {
                _targets.Remove(target.TargetId);
            }
        }

        /// <summary>
        /// ID로 TutorialTarget 찾기
        /// </summary>
        /// <param name="targetId">찾을 타겟 ID</param>
        /// <returns>찾은 TutorialTarget, 없으면 null</returns>
        public static TutorialTarget Find(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
                return null;

            _targets.TryGetValue(targetId, out var target);
            return target;
        }

        /// <summary>
        /// ID로 GameObject 찾기 (기존 GameObject.Find 대체용)
        /// </summary>
        /// <param name="targetId">찾을 타겟 ID</param>
        /// <returns>찾은 GameObject, 없으면 null</returns>
        public static GameObject FindGameObject(string targetId)
        {
            var target = Find(targetId);
            return target != null ? target.gameObject : null;
        }

        /// <summary>
        /// 특정 ID가 등록되어 있는지 확인
        /// </summary>
        public static bool Contains(string targetId)
        {
            return !string.IsNullOrEmpty(targetId) && _targets.ContainsKey(targetId);
        }

        /// <summary>
        /// 모든 등록된 타겟 수
        /// </summary>
        public static int Count => _targets.Count;

        /// <summary>
        /// 레지스트리 초기화 (씬 전환 시 필요한 경우)
        /// </summary>
        public static void Clear()
        {
            _targets.Clear();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 디버그용: 등록된 모든 타겟 출력
        /// </summary>
        public static void DebugPrintAll()
        {
            Debug.Log($"[TutorialTargetRegistry] 등록된 타겟 수: {_targets.Count}");
            foreach (var kvp in _targets)
            {
                Debug.Log($"  - {kvp.Key}: {(kvp.Value != null ? kvp.Value.gameObject.name : "null")}");
            }
        }
#endif
    }
}
