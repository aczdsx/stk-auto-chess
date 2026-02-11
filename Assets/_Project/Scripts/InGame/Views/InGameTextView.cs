using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CookApps.AutoBattler
{
    public class InGameTextView : CachedMonoBehaviour
    {
        [SerializeField] private TMP_Text _damageText;
        [SerializeField] private TMP_Text _iconText;


        [SerializeField] private Transform _root;
        [SerializeField] private Animator _animator;

        [SerializeField] private float _heightOffset = 0.3f;
        [SerializeField] private float _xOffset = 0.0f;

        [SerializeField] private Color defaultColor;


        public enum DamageColorType
        {
            Normal, // 일반
            Heal, // 힐
            Miss, // 명중 테스트 실패
            Block, // 최종 방어력 산정에 블록
            DotDeal,//화상, 독
        }

        public SerializableDictionary<DamageColorType, TMP_ColorGradient> IconGradients;
        private static readonly int Normal = Animator.StringToHash("Normal"); // 일반 크리
        private static readonly int Heal = Animator.StringToHash("Heal"); // 힐 독
        private static readonly int Miss = Animator.StringToHash("Miss");// 미스, 블락

        private const string MissText = "MISS";

        
        // 재사용 가능한 리스트 (GC 할당 최소화)
        private readonly List<InGameTextViewSpriteFont.SpriteFontType> _reusableSpriteFontList = new List<InGameTextViewSpriteFont.SpriteFontType>(4);

        /// <summary>
        /// 스프라이트 폰트를 조합하여 최적화된 문자열을 생성합니다.
        /// </summary>
        private string BuildSpriteText(IReadOnlyList<InGameTextViewSpriteFont.SpriteFontType> spriteFonts)
        {
            if (spriteFonts == null || spriteFonts.Count == 0)
                return string.Empty;
            
            return InGameTextViewSpriteFont.GetSpriteFonts(spriteFonts);
        }
        
        /// <summary>
        /// 숫자 텍스트를 포맷팅하여 생성합니다.
        /// </summary>
        private string BuildNumberText(double value, string prefix = null)
        {
            using var sb = ZString.CreateStringBuilder();
            
            // 접두사 추가 (예: "+" for heal)
            if (!string.IsNullOrEmpty(prefix))
            {
                sb.Append(prefix);
            }
            
            // 숫자 값 추가 (천 단위 구분자 포함)
            sb.Append(value.ToString("N0"));
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 텍스트와 아이콘을 설정합니다.
        /// </summary>
        private void SetTextContent(string iconText, string damageText, DamageColorType colorType)
        {
            _iconText.text = iconText;
            _damageText.text = damageText;
            _damageText.colorGradientPreset = IconGradients[colorType];
        }
        
        /// <summary>
        /// 위치를 설정하고 애니메이션을 트리거합니다.
        /// </summary>
        private void SetupPositionAndAnimation(Vector3 position, float characterHeight, int triggerHash, bool useHeightOffset = true)
        {
            _xOffset = Random.Range(-0.5f, 0.5f);
            
            float height = useHeightOffset ? characterHeight + _heightOffset : characterHeight;
            Vector3 initialPosition = position + Vector3.up * height;
            initialPosition.x += _xOffset;
            
            _root.position = initialPosition;
            _animator.SetTrigger(triggerHash);
        }
        
        /// <summary>
        /// 데미지 사운드를 재생합니다.
        /// </summary>
        public void PlayDamageSound(bool isCritical)
        {
            if (SoundManager.Instance.IsPlayingGacha)
                return;
            
            SoundManager.Instance.PlaySFX(isCritical 
                ? SoundFX.snd_sfx_hit_critical1 
                : SoundFX.snd_sfx_hit_normal1);
        }
        
        /// <summary>
        /// 데미지 텍스트용 스프라이트 폰트를 구성합니다.
        /// </summary>
        private void BuildDamageSpriteFonts(bool isCritical)
        {
            _reusableSpriteFontList.Clear();

            if (isCritical)
            {
                _reusableSpriteFontList.Add(InGameTextViewSpriteFont.SpriteFontType.SPRITE_CRITICAL);
            }
        }

        public async UniTask ShowDamageText(Vector3 position, float characterHeight, double damage,
        bool isCritical)
        {
            // 스프라이트 폰트 구성
            BuildDamageSpriteFonts(isCritical);

            // 텍스트 설정
            SetTextContent(
                BuildSpriteText(_reusableSpriteFontList),
                BuildNumberText(damage),
                DamageColorType.Normal
            );

            // 위치 및 애니메이션 설정
            SetupPositionAndAnimation(position, characterHeight, Normal);

            // 사운드 재생
        }

        public async UniTask ShowBlockText(Vector3 position, float characterHeight, double damage)
        {
            _reusableSpriteFontList.Clear();
            _reusableSpriteFontList.Add(InGameTextViewSpriteFont.SpriteFontType.SPRITE_BLOCK);
            
            // 블록은 아이콘만 표시
            SetTextContent(
                BuildSpriteText(_reusableSpriteFontList),
                BuildNumberText(damage),
                DamageColorType.Block
            );
            
            // 블락 -> Miss
            SetupPositionAndAnimation(position, characterHeight, Miss);
        }
        public async UniTask ShowMissText(Vector3 position, float characterHeight)
        {
            // MISS는 아이콘 없이 텍스트만 표시
            SetTextContent(
                string.Empty,
                MissText,
                DamageColorType.Miss
            );
            
            // 미스 -> Miss
            SetupPositionAndAnimation(position, characterHeight, Miss);
        }

        public async UniTask ShowHealText(Vector3 position, float characterHeight, double healAmount)
        {
            _reusableSpriteFontList.Clear();
            _reusableSpriteFontList.Add(InGameTextViewSpriteFont.SpriteFontType.SPRITE_HEAL);
            
            // 힐 텍스트 설정 ("+" 접두사 포함)
            SetTextContent(
                BuildSpriteText(_reusableSpriteFontList),
                BuildNumberText(healAmount, null),
                DamageColorType.Heal
            );
            
            // 힐 독 -> Heal
            // 힐은 heightOffset 없이 표시
            SetupPositionAndAnimation(position, characterHeight, Heal, useHeightOffset: false);
        }

        public void ReturnTextView()
        {
            _reusableSpriteFontList.Clear();
            _iconText.text = string.Empty;
            _damageText.text = string.Empty;
            InGameTextViewPool.Instance.Return(this);
        }

    }

    public class InGameTextViewPool : Singleton<InGameTextViewPool>
    {
        private UnityPool<InGameTextView> _textViewPool;
        private GameObject _instance;

        public void InitializePool(GameObject instance)
        {
            _instance = instance;
            _textViewPool = new UnityPool<InGameTextView>();
            _textViewPool.Initialize(_instance);
        }

        public void ReleasePool()
        {
            _textViewPool.ClearPool();
            _textViewPool = null;
        }

        public InGameTextView Get()
        {
            return _textViewPool.Get(null);
        }

        public void Return(InGameTextView textView)
        {
            if (_textViewPool != null)
                _textViewPool.Return(textView);
        }
    }

    public static class InGameTextViewSpriteFont
    {
        public enum SpriteFontType
        {
            SPRITE_CRITICAL = 0,
            SPRITE_CRITICAL_BIG,
            SPRITE_BLOCK,
            SPRITE_HEAL,
        }

        /// <summary>
        /// 스프라이트 폰트 태그 문자열을 생성합니다.
        /// </summary>
        public static string GetSpriteFont(SpriteFontType spriteFontType)
        {
            return ZString.Format("<sprite={0}>", (int)spriteFontType);
        }
        
        /// <summary>
        /// 여러 스프라이트 폰트를 조합하여 문자열을 생성합니다.
        /// </summary>
        public static string GetSpriteFonts(IReadOnlyList<SpriteFontType> spriteFontTypes)
        {
            if (spriteFontTypes == null || spriteFontTypes.Count == 0)
                return string.Empty;
            
            using var sb = ZString.CreateStringBuilder();
            for (int i = 0; i < spriteFontTypes.Count; i++)
            {
                sb.Append(GetSpriteFont(spriteFontTypes[i]));
                if (i < spriteFontTypes.Count - 1)
                {
                    sb.Append(' ');
                }
            }
            return sb.ToString();
        }
    }
}
