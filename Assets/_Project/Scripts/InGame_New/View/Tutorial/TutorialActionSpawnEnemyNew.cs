using CookApps.AutoBattler;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// InGame_New용 적 스폰 튜토리얼 액션.
    /// 레거시 InGameObjectManager 대신 TutorialSimBridge를 통해
    /// 시뮬레이션 커맨드로 적을 스폰.
    ///
    /// tutorial_action_key 형식:
    /// - "monsterSpecId,col,row" : 단일 스폰
    /// - "monsterSpecId,col,row;monsterSpecId,col,row;..." : 복수 스폰
    /// </summary>
    public class TutorialActionSpawnEnemyNew : ITutorialActionStrategy
    {
        public void OnShow(TutorialActionContext context)
        {
            // 전체 화면 마스크 설정
            context.SetFullScreenMask();

            // 튜토리얼 UI 숨김 (스폰 중에는 말풍선 등 표시 안함)
            context.ArrowRectTransform.gameObject.SetActive(false);

            var bridge = TutorialSimBridge.Instance;
            if (bridge == null)
            {
                Debug.LogWarning("[TutorialActionSpawnEnemyNew] TutorialSimBridge가 없습니다.");
                context.OnCompleted?.Invoke();
                return;
            }

            string actionKey = context.CurrentTutorial.tutorial_action_key;
            if (string.IsNullOrEmpty(actionKey))
            {
                Debug.LogWarning("[TutorialActionSpawnEnemyNew] tutorial_action_key가 비어있습니다.");
                context.OnCompleted?.Invoke();
                return;
            }

            // "monsterSpecId,col,row;monsterSpecId,col,row;..." 파싱
            var entries = actionKey.Split(';');
            int spawnCount = 0;

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i].Trim();
                if (string.IsNullOrEmpty(entry)) continue;

                var parts = entry.Split(',');
                if (parts.Length >= 3 &&
                    int.TryParse(parts[0].Trim(), out int monsterSpecId) &&
                    int.TryParse(parts[1].Trim(), out int col) &&
                    int.TryParse(parts[2].Trim(), out int row))
                {
                    bridge.EnqueueSpawnCommand(monsterSpecId, col, row);
                    spawnCount++;
                    Debug.Log($"[TutorialActionSpawnEnemyNew] 스폰 커맨드: monster={monsterSpecId} col={col} row={row}");
                }
                else if (parts.Length == 1 && int.TryParse(parts[0].Trim(), out int singleMonsterId))
                {
                    // 레거시 호환: "monsterSpecId"만 있는 경우 기본 위치에 스폰
                    bridge.EnqueueSpawnCommand(singleMonsterId, 0, 0);
                    spawnCount++;
                    Debug.Log($"[TutorialActionSpawnEnemyNew] 스폰 커맨드 (기본 위치): monster={singleMonsterId}");
                }
                else
                {
                    Debug.LogWarning($"[TutorialActionSpawnEnemyNew] 파싱 실패: {entry}");
                }
            }

            if (spawnCount == 0)
            {
                Debug.LogWarning($"[TutorialActionSpawnEnemyNew] 스폰 대상 없음: {actionKey}");
            }

            // 스폰 커맨드는 다음 틱에서 처리되므로 즉시 완료
            context.OnCompleted?.Invoke();
        }

        public void OnNext(TutorialActionContext context)
        {
            // 스폰 튜토리얼에서는 "다음" 버튼을 표시하지 않음
            context.NextObj.SetActive(false);
        }

        public bool CanProceedOnDimmedClick(TutorialActionContext context)
        {
            // 딤드 클릭으로 진행 불가
            return false;
        }

        public void OnClear(TutorialActionContext context)
        {
            // 마스크 복원
            context.RestoreMask();
        }
    }
}
