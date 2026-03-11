using System;
using CookApps.AutoBattler;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// CharacterInfoInGamePopup의 단일 인스턴스를 관리.
    /// 보드 유닛 탭, 벤치 슬롯 탭 등 여러 곳에서 동일한 팝업을 공유한다.
    /// 새 유닛을 선택하면 이전 선택의 onDeselected가 먼저 호출된 뒤 팝업이 교체된다.
    /// </summary>
    public static class InGameCharacterPopupHelper
    {
        private static CharacterInfoInGamePopup _popup;
        private static int _selectedEntityId = UnitData.InvalidId;
        private static Action _onDeselected;

        public static int SelectedEntityId => _selectedEntityId;

        /// <summary>
        /// 유닛을 선택하여 팝업을 표시한다.
        /// 같은 entityId 재선택 시 토글(닫기).
        /// 다른 entityId 선택 시 이전 onDeselected 호출 후 팝업 Refresh 또는 새로 Push.
        /// </summary>
        /// <param name="entityId">선택할 유닛 entityId</param>
        /// <param name="param">팝업에 전달할 파라미터</param>
        /// <param name="onDeselected">이 선택이 해제될 때 호출되는 콜백 (다른 유닛 선택, 닫기 등)</param>
        public static void Select(int entityId, CharacterInfoInGamePopup.PopupParam param, Action onDeselected = null)
        {
            // 같은 유닛 재선택 → 토글 닫기
            if (_selectedEntityId == entityId)
            {
                Close();
                return;
            }

            // 이전 선택 해제 콜백
            InvokeDeselected();

            _selectedEntityId = entityId;
            _onDeselected = onDeselected;

            if (_popup != null)
            {
                _popup.Refresh(param);
            }
            else
            {
                OpenAsync(param).Forget();
            }
        }

        /// <summary>현재 팝업을 닫고 선택을 해제한다.</summary>
        public static void Close()
        {
            InvokeDeselected();

            _selectedEntityId = UnitData.InvalidId;

            if (_popup != null)
            {
                SceneUILayerManager.Instance.PopUILayer(_popup);
                _popup = null;
            }
        }

        private static void InvokeDeselected()
        {
            var cb = _onDeselected;
            _onDeselected = null;
            cb?.Invoke();
        }

        private static async UniTaskVoid OpenAsync(CharacterInfoInGamePopup.PopupParam param)
        {
            int capturedId = _selectedEntityId;

            var popup = await SceneUILayerManager.Instance.PushUILayerAsync<CharacterInfoInGamePopup>(param, _ =>
            {
                // 팝업이 외부에서 닫힐 때 (닫기 버튼 등)
                _popup = null;
                InvokeDeselected();
                _selectedEntityId = UnitData.InvalidId;
            });

            // await 중 선택이 바뀌지 않았으면 팝업 보관
            if (_selectedEntityId == capturedId && _selectedEntityId != UnitData.InvalidId)
            {
                _popup = popup;
            }
            else
            {
                // 로딩 중 다른 선택이 발생 → 이 팝업 즉시 닫기
                SceneUILayerManager.Instance.PopUILayer(popup);
            }
        }
    }
}
