using System;
using System.Runtime.InteropServices.WindowsRuntime;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class SynergyTooltipImageSlot : CachedMonoBehaviour
    {
        private static readonly int GrayscaleAmountID = Shader.PropertyToID("_GrayscaleAmount");

        [SerializeField] private Image _characterImage;
        [SerializeField] private SimpleImageColorSwapper _characterGradeColorSwapper;
        [SerializeField] private SpriteLoader _spriteLoader;

        private Material _materialInstance;

        private void Awake()
        {
            if (_characterImage != null && _characterImage.material != null)
            {
                _materialInstance = new Material(_characterImage.material);
                _characterImage.material = _materialInstance;
            }
        }

        private void OnDestroy()
        {
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
                _materialInstance = null;
            }
        }

        /// <summary>
        /// 캐릭터 아이콘, 등급 색상, 활성 상태 설정
        /// </summary>
        public void SetCharacter(int prefabId, GradeType grade, bool inBattle)
        {
            _spriteLoader.SetSprite(SpriteNameParser.GetCharacterSmallItemSprite(prefabId)).Forget();
            _characterGradeColorSwapper.Swap(GradeTypeToSwapType(grade));
            _materialInstance.SetFloat(GrayscaleAmountID, inBattle ? 0f : 1f);
        }

        public void SetActive(bool active) => gameObject.SetActive(active);

        /// <summary>
        /// GradeType → SimpleSwapType 변환 (Grade_0 = 10 기준으로 매핑)
        /// </summary>
        private static SimpleSwapType GradeTypeToSwapType(GradeType grade)
        {
            SimpleSwapType retVal = SimpleSwapType.Grade_0;
            switch (grade)
            {
                case GradeType.RARE:
                    retVal = SimpleSwapType.Grade_2;
                    break;
                case GradeType.EPIC:
                    retVal = SimpleSwapType.Grade_3;
                    break;
                case GradeType.LEGENDARY:
                    retVal = SimpleSwapType.Grade_4;
                    break;
                case GradeType.UNIQUE:
                case GradeType.ANCIENT:
                case GradeType.MYTHIC:
                case GradeType.MAGIC:
                case GradeType.NORMAL:
                case GradeType.COMMON:
                case GradeType.UNCOMMON:
                default:
                    Debug.LogError("Unknown grade type: " + grade);
                    break;
            }

            return retVal;
        }
    }
}