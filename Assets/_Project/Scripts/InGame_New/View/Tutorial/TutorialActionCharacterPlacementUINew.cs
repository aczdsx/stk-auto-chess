using CookApps.AutoBattler;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// InGame_New용 캐릭터 배치 튜토리얼 액션.
    /// 레거시 InGameObjectManager 대신 TutorialSimBridge를 통해 벤치에 유닛을 생성.
    ///
    /// tutorial_action_key 형식: "Slot_3401->7"
    ///   - Slot_3401: UI 슬롯 이름 (3401 = championSpecId)
    ///   - 7: 배치 대상 타일 인덱스 (BoardHelper.FromIndex로 col,row 변환)
    /// </summary>
    public class TutorialActionCharacterPlacementUINew : ITutorialActionStrategy
    {
        public void OnShow(TutorialActionContext context)
        {
            var bridge = TutorialSimBridge.Instance;
            if (bridge == null)
            {
                Debug.LogWarning("[TutorialActionCharacterPlacementUINew] TutorialSimBridge가 없습니다.");
                context.OnCompleted?.Invoke();
                return;
            }

            string actionKey = context.CurrentTutorial.tutorial_action_key;
            var (champSpecId, tileIndex) = ParseActionKey(actionKey);

            // 벤치에 유닛 생성 (중복 시 스킵)
            if (champSpecId > 0)
            {
                int entityId = bridge.SpawnBenchUnit(champSpecId);
                Debug.Log($"[TutorialActionCharacterPlacementUINew] 벤치 유닛 생성: champSpecId={champSpecId}, entityId={entityId}");
            }

            // 화살표 비활성화
            context.ArrowRectTransform.gameObject.SetActive(false);

            // 딤드 클릭으로 다음 진행 허용
            context.NextObj.SetActive(true);
        }

        public void OnNext(TutorialActionContext context)
        {
            context.NextObj.SetActive(true);
        }

        public bool CanProceedOnDimmedClick(TutorialActionContext context) => true;

        public void OnClear(TutorialActionContext context) { }

        /// <summary>
        /// "Slot_3401->7" 파싱 → (champSpecId=3401, tileIndex=7)
        /// </summary>
        private static (int champSpecId, int tileIndex) ParseActionKey(string actionKey)
        {
            if (string.IsNullOrEmpty(actionKey)) return (0, 0);

            var parts = actionKey.Split(new[] { "->" }, System.StringSplitOptions.None);
            int champSpecId = 0;
            int tileIndex = 0;

            if (parts.Length >= 1)
            {
                string slotName = parts[0].Trim();
                int underscoreIdx = slotName.LastIndexOf('_');
                if (underscoreIdx >= 0 && underscoreIdx < slotName.Length - 1)
                    int.TryParse(slotName.Substring(underscoreIdx + 1), out champSpecId);
            }

            if (parts.Length >= 2)
                int.TryParse(parts[1].Trim(), out tileIndex);

            return (champSpecId, tileIndex);
        }
    }
}
