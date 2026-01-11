using CookApps.BattleSystem;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 캐릭터 배치 튜토리얼 액션.
    /// 특정 캐릭터를 특정 타일에 배치하도록 유도합니다.
    /// 마스크 홀 시스템을 활용하여 타겟 타일을 하이라이트합니다.
    ///
    /// tutorial_action_key 형식:
    /// - "캐릭터ID" : 해당 캐릭터만 선택 가능, 배치 위치는 자유
    /// - "캐릭터ID_타일ID" : 해당 캐릭터를 해당 타일에만 배치 가능
    ///
    /// 배치가 완료되면 자동으로 다음 튜토리얼로 진행됩니다.
    /// </summary>
    public class TutorialActionCharacterPlacement : ITutorialActionStrategy
    {
        /// <summary>
        /// 배치 완료 시 호출되는 콜백 (TutorialController에서 설정)
        /// </summary>
        public static System.Action OnCharacterPlacementCompleted;

        /// <summary>
        /// 현재 튜토리얼에서 배치해야 할 캐릭터 ID
        /// </summary>
        public static int TargetCharacterId { get; private set; }

        /// <summary>
        /// 현재 튜토리얼에서 배치해야 할 타일 ID (-1이면 제한 없음)
        /// </summary>
        public static int TargetTileId { get; private set; } = -1;

        /// <summary>
        /// 현재 캐릭터 배치 튜토리얼 진행 중인지 여부
        /// </summary>
        public static bool IsActive { get; private set; }

        public void OnShow(TutorialActionContext context)
        {
            // 화살표는 타겟 타일 위치에 표시할 예정
            context.ArrowRectTransform.gameObject.SetActive(false);

            // tutorial_action_key 파싱 (형식: "캐릭터ID" 또는 "캐릭터ID_타일ID")
            ParseActionKey(context.CurrentTutorial.tutorial_action_key);

            IsActive = true;

            // 마스크 홀 타겟 설정 (타일 또는 캐릭터)
            SetUnmaskTarget(context);
        }

        public void OnNext(TutorialActionContext context)
        {
            // 배치 튜토리얼에서는 "다음" 버튼을 표시하지 않음
            // 캐릭터 배치가 완료되면 자동으로 진행됨
            context.NextObj.SetActive(false);
        }

        public bool CanProceedOnDimmedClick(TutorialActionContext context)
        {
            // 딤드 클릭으로 진행 불가 - 반드시 캐릭터를 배치해야 함
            return false;
        }

        public void OnClear(TutorialActionContext context)
        {
            // 상태 초기화
            IsActive = false;
            TargetCharacterId = 0;
            TargetTileId = -1;
            OnCharacterPlacementCompleted = null;
            context.TargetUnmaskObj = null;
        }

        /// <summary>
        /// tutorial_action_key 파싱
        /// </summary>
        private void ParseActionKey(string actionKey)
        {
            TargetCharacterId = 0;
            TargetTileId = -1;

            if (string.IsNullOrEmpty(actionKey))
            {
                Debug.LogWarning("[TutorialActionCharacterPlacement] action_key가 비어있습니다.");
                return;
            }

            string[] parts = actionKey.Split('_');

            // 캐릭터 ID 파싱
            if (parts.Length >= 1 && int.TryParse(parts[0], out int characterId))
            {
                TargetCharacterId = characterId;
            }
            else
            {
                Debug.LogWarning($"[TutorialActionCharacterPlacement] 캐릭터 ID 파싱 실패: {actionKey}");
            }

            // 타일 ID 파싱 (있는 경우)
            if (parts.Length >= 2 && int.TryParse(parts[1], out int tileId))
            {
                TargetTileId = tileId;
            }
        }

        /// <summary>
        /// 마스크 홀 타겟 설정 (타일 또는 캐릭터)
        /// </summary>
        private void SetUnmaskTarget(TutorialActionContext context)
        {
            // 타일이 지정된 경우 타일을 타겟으로 (마스크 홀이 타일 위치에 뚫림)
            if (TargetTileId >= 0)
            {
                var tile = InGameObjectManager.Instance?.GetInGameTile(TargetTileId);
                if (tile?.View != null)
                {
                    context.TargetUnmaskObj = tile.View.gameObject;
                    return;
                }
            }

            // 캐릭터 TutorialTarget이 있으면 그것을 타겟으로
            var characterTarget = TutorialTargetRegistry.FindGameObject(TargetCharacterId.ToString());
            if (characterTarget != null)
            {
                context.TargetUnmaskObj = characterTarget;
            }
        }

        /// <summary>
        /// 캐릭터 배치 완료 시 외부에서 호출
        /// </summary>
        public static void NotifyPlacementCompleted()
        {
            if (IsActive)
            {
                OnCharacterPlacementCompleted?.Invoke();
            }
        }

        /// <summary>
        /// 지정된 캐릭터만 선택 가능한지 확인
        /// </summary>
        /// <param name="characterId">선택하려는 캐릭터 ID</param>
        /// <returns>선택 가능 여부</returns>
        public static bool CanSelectCharacter(int characterId)
        {
            if (!IsActive || TargetCharacterId == 0)
            {
                return true; // 튜토리얼이 아니거나 제한이 없으면 모든 캐릭터 선택 가능
            }

            return characterId == TargetCharacterId;
        }

        /// <summary>
        /// 지정된 타일로 배치 가능한지 확인
        /// </summary>
        /// <param name="tileId">배치하려는 타일 ID</param>
        /// <returns>배치 가능 여부</returns>
        public static bool CanPlaceOnTile(int tileId)
        {
            if (!IsActive || TargetTileId < 0)
            {
                return true; // 튜토리얼이 아니거나 타일 제한이 없으면 모든 타일 배치 가능
            }

            return tileId == TargetTileId;
        }

        /// <summary>
        /// 타겟 타일의 월드 좌표 반환 (화살표 표시용)
        /// </summary>
        public static Vector3? GetTargetTilePosition()
        {
            if (!IsActive || TargetTileId < 0) return null;

            var tile = InGameObjectManager.Instance?.GetInGameTile(TargetTileId);
            return tile?.View?.Position;
        }
    }
}
