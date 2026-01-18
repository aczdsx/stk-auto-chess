using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace CookApps.AutoBattler
{
    public class LevelUpIncreaseDataSlot : MonoBehaviour
    {
        [SerializeField] private GameObject _focusFX;
        [SerializeField] private Color _beforeColor;
        [SerializeField] private Color _afterColor;
        [SerializeField] private TextMeshProUGUI _KeyText;
        [SerializeField] private Gradient2 _fx_R;
        [SerializeField] private Gradient2 _fx_L;


        private void OnEnable()
        {
            if(_fx_R !=null || _fx_L !=null)
            {
                _fx_L.Offset = 1f;
                _fx_R.Offset = -1;
            }
        }

        private void OnDisable()
        {
            _fx_L.Offset = 1f;
            _fx_R.Offset = -1;
            _KeyText.DOColor(_beforeColor, 0f);
        
        }

        public void ChangeFocusFX()
        {
            _focusFX.SetActive(true);
            _KeyText.DOColor(_afterColor, 0f).SetDelay(0.09f);
            DOTween.To(() => _fx_L.Offset, x => _fx_L.Offset = x, 0f, 0.09f).SetEase(Ease.OutQuad);
            DOTween.To(() => _fx_L.Offset, x => _fx_L.Offset = x, 1f, 0.13f).SetDelay(0.09f).SetEase(Ease.InQuad);
            DOTween.To(() => _fx_R.Offset, x => _fx_R.Offset = x, 0f, 0.09f).SetEase(Ease.OutQuad);
            DOTween.To(() => _fx_R.Offset, x => _fx_R.Offset = x, -1f, 0.13f).SetDelay(0.09f).SetEase(Ease.InQuad)
                .OnComplete(() => {
                    _focusFX.SetActive(false);
                });
        }

        
    }
}
