using LitMotion;
using LitMotion.Extensions;
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
            _KeyText.color = _beforeColor;

        }

        public void ChangeFocusFX()
        {
            _focusFX.SetActive(true);
            LMotion.Create(_KeyText.color, _afterColor, 0f)
                .WithDelay(0.09f)
                .BindToColor(_KeyText)
                .AddTo(this);
            LMotion.Create(_fx_L.Offset, 0f, 0.09f)
                .WithEase(Ease.OutQuad)
                .Bind(x => _fx_L.Offset = x)
                .AddTo(this);
            LMotion.Create(_fx_L.Offset, 1f, 0.13f)
                .WithDelay(0.09f)
                .WithEase(Ease.InQuad)
                .Bind(x => _fx_L.Offset = x)
                .AddTo(this);
            LMotion.Create(_fx_R.Offset, 0f, 0.09f)
                .WithEase(Ease.OutQuad)
                .Bind(x => _fx_R.Offset = x)
                .AddTo(this);
            LMotion.Create(_fx_R.Offset, -1f, 0.13f)
                .WithDelay(0.09f)
                .WithEase(Ease.InQuad)
                .WithOnComplete(() => _focusFX.SetActive(false))
                .Bind(x => _fx_R.Offset = x)
                .AddTo(this);
        }


    }
}
