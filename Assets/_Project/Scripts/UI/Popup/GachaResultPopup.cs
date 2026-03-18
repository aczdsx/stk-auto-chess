using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 가챠 결과 팝업 파라미터
    /// </summary>
    public class GachaResultPopupParam
    {
        /// <summary>가챠 결과 아이템 목록</summary>
        public List<RewardItem> ResultItems;

        /// <summary>연속 뽑기 버튼 표시에 사용할 스펙 데이터 (비용 아이콘/텍스트)</summary>
        public GachaInfo SpecGachaData;

        /// <summary>연속 뽑기 버튼 콜백. null이면 버튼 비활성화</summary>
        public Action OnContinueGacha;
    }

    /// <summary>
    /// 가챠 연출(VFX) 완료 후 결과를 카드 형태로 보여주는 팝업.
    /// GachaResultItemSlot을 동적으로 생성/해제하며, 확인/연속 뽑기 버튼을 제공한다.
    /// </summary>
    public class GachaResultPopup : UILayerPopupBase
    {
        #region Serialized Fields

        [Header("Result Items")]
        [SerializeField] private Transform _listGroupTransform;
        [SerializeField] private GachaResultItemSlot _templateSlot;

        [Header("Footer")]
        [SerializeField] private CAButton _confirmButton;
        [SerializeField] private CAButton _continueGachaButton;
        [SerializeField] private TextMeshProUGUI _continueButtonText;
        [SerializeField] private Image _costIcon;
        [SerializeField] private SpriteLoader _costIconSpriteLoader;
        [SerializeField] private TextMeshProUGUI _costValueText;

        #endregion

        #region Private Fields

        private GachaResultPopupParam _param;
        private readonly List<GachaResultItemSlot> _spawnedSlots = new();

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            _confirmButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickConfirm())
                .AddTo(this);

            _continueGachaButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickContinueGacha())
                .AddTo(this);
        }

        #endregion

        #region Lifecycle Overrides

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            _param = param as GachaResultPopupParam;
            if (_param == null)
            {
                Debug.LogError("[GachaResultPopup] param이 GachaResultPopupParam 타입이 아닙니다.");
                return;
            }

            // 템플릿 슬롯은 항상 비활성 상태 유지
            _templateSlot.gameObject.SetActive(false);

            SetResultItems(_param.ResultItems);
            SetContinueButton(_param.SpecGachaData, _param.OnContinueGacha);
        }

        protected override void OnPreExit()
        {
            ClearSlots();
            base.OnPreExit();
        }

        #endregion

        #region Private Methods

        private void SetResultItems(List<RewardItem> items)
        {
            ClearSlots();

            if (items == null) return;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!item.Id.GetCharacterId(out int charId)) continue;

                var charInfo = SpecDataManager.Instance.CharacterInfo.Get(charId);
                if (charInfo == null) continue;

                var slotObj = Instantiate(_templateSlot.gameObject, _listGroupTransform);
                slotObj.SetActive(true);

                var slot = slotObj.GetComponent<GachaResultItemSlot>();
                slot.SetData(item, charInfo);
                _spawnedSlots.Add(slot);
            }
        }

        private void ClearSlots()
        {
            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                _spawnedSlots[i].Release();
                Destroy(_spawnedSlots[i].gameObject);
            }
            _spawnedSlots.Clear();
        }

        private void SetContinueButton(GachaInfo specGachaData, Action onContinueGacha)
        {
            bool hasContinue = onContinueGacha != null;
            _continueGachaButton.gameObject.SetActive(hasContinue);

            if (!hasContinue || specGachaData == null) return;

            // 비용 텍스트
            if (_costValueText != null)
                _costValueText.text = specGachaData.gacha_cost.ToString();

            // 비용 아이콘 (SpriteLoader 사용)
            // if (_costIconSpriteLoader != null)
            //     _costIconSpriteLoader.SetSprite(
            //         SpriteNameParser.GetItemSprite(specGachaData.gacha_cost_item_id)
            //     ).Forget();
        }

        private void OnClickConfirm()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void OnClickContinueGacha()
        {
            _param?.OnContinueGacha?.Invoke();
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        #endregion
    }
}
