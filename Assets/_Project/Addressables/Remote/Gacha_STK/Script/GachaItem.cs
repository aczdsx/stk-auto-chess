using System.Collections.Generic;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class GachaItem : MonoBehaviour
    {
        [Header("뒷 이미지")]
        [SerializeField] private Image BackImage;
        [SerializeField] private SpriteLoader BackImageSpriteLoader;
        [SerializeField] private Image PieceImage;
        [SerializeField] private SpriteLoader PieceImageSpriteLoader;
        [SerializeField] private Image PieceBackImage;
        [SerializeField] private SpriteLoader PieceBackImageSpriteLoader;
        [SerializeField] private Image KnightImage;
        [SerializeField] private SpriteLoader KnightImageSpriteLoader;

        [Header("열렸을때 기사")]
        [SerializeField] private GameObject KnightOnObject;
        [SerializeField] private Image KnighBackImage;
        [SerializeField] private SpriteLoader KnighBackImageSpriteLoader;
        [SerializeField] private Image KnighImage;
        [SerializeField] private SpriteLoader KnighImageSpriteLoader;
        [SerializeField] private Image KnighColorImage;
        [SerializeField] private Image KnighSoulImage;
        [SerializeField] private List<GameObject> starObjects;

        [SerializeField] private GameObject SliderObject;
        [SerializeField] private Slider pieceSlider;
        [SerializeField] private TextMeshProUGUI countText;

        [Header("열렸을때 조각")]
        [SerializeField] private GameObject PieceOnObject;
        [SerializeField] private Image PieceOnBackImage;
        [SerializeField] private SpriteLoader PieceOnBackImageSpriteLoader;
        [SerializeField] private Image PieceOnImage;
        [SerializeField] private SpriteLoader PieceOnImageSpriteLoader;
        [SerializeField] private Image ItemOnImage;
        [SerializeField] private SpriteLoader ItemOnImageSpriteLoader;
        [SerializeField] private TextMeshProUGUI amountText;

        [SerializeField] private Slider piece2Slider;
        [SerializeField] private TextMeshProUGUI count2Text;


        [SerializeField] private GameObject showParticle;
        [SerializeField] Image SigmaImage;

        [Header("완제 소녀 연출")]
        [SerializeField] private GameObject[] SSRFxObjects;
        [SerializeField] private GameObject[] SRFxObjects;

        private CharacterInfo characterData;

        private RewardItem _rewardItemData;

        // private CharacterEnhanceMetaData _curEnhanceMetaData;
        public void InitItem(RewardItem _rewardItem)
        {
            this.gameObject.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            //_curEnhanceMetaData = SpecDataManager.Instance.GetCurrentCharacterEnhanceMetaData((int)_characterData.MetaData.grade_value, _characterData.Star);

            _rewardItemData = _rewardItem;

            if (_rewardItem.Id.GetCharacterId(out var charId))
            {
                characterData = SpecDataManager.Instance.GetCharacterData(charId);
            }

            showParticle.SetActive(false);

            BackImageSpriteLoader.SetSprite("Gacha_Img_Gray").Forget();
            KnightOnObject.SetActive(false);
            PieceOnObject.SetActive(false);
            for (int i = 0; i < SSRFxObjects.Length; i++)
            {
                SSRFxObjects[i].SetActive(false);
            }
            for (int i = 0; i < SRFxObjects.Length; i++)
            {
                SRFxObjects[i].SetActive(false);
            }
            if (_rewardItem.Id.IsCharacter())
            {
                PieceBackImage.gameObject.SetActive(false);
                KnightImage.gameObject.SetActive(true);
                KnightImageSpriteLoader.SetSprite("Gacha_Img_Icon_Gray").Forget();

            }
            else
            {
                PieceBackImage.gameObject.SetActive(true);
                KnightImage.gameObject.SetActive(false);

                PieceBackImageSpriteLoader.SetSprite("Gacha_Img_Piece_Gray").Forget();
                PieceImageSpriteLoader.SetSprite("Gacha_Img_Icon_Gray").Forget();
            }
        }

        public void ChangeItem()
        {
            // this.transform.DOShakeScale(0.5f, 0.1f, 1).SetEase(Ease.Linear).OnComplete(() =>
            // {
            //     this.transform.localScale = new Vector3(1, 1, 1);
            // });

            GradeType resultGradeType = GradeType.COMMON;
            if (characterData != null)
            {
                resultGradeType = characterData.grade_type;
            }

            if (_rewardItemData.Id.IsCharacter())
            {
                if (resultGradeType == GradeType.COMMON)
                {
                    BackImageSpriteLoader.SetSprite("Gacha_Img_Green").Forget();
                    KnightImageSpriteLoader.SetSprite("Gacha_Img_Icon_Green").Forget();
                    KnighBackImageSpriteLoader.SetSprite("Gacha_Img_GreenOn_Bg").Forget();

                }
                else if (resultGradeType == GradeType.RARE)
                {
                    BackImageSpriteLoader.SetSprite("Gacha_Img_Blue").Forget();
                    KnightImageSpriteLoader.SetSprite("Gacha_Img_Icon_Blue").Forget();
                    KnighBackImageSpriteLoader.SetSprite("Gacha_Img_BlueOn_Bg").Forget();
                }
                else if (resultGradeType == GradeType.EPIC)
                {
                    BackImageSpriteLoader.SetSprite("Gacha_Img_Purple").Forget();
                    KnightImageSpriteLoader.SetSprite("Gacha_Img_Icon_Purple").Forget();
                    KnighBackImageSpriteLoader.SetSprite("Gacha_Img_PurpleOn_Bg").Forget();
                }
                else if (resultGradeType == GradeType.LEGENDARY)
                {
                    BackImageSpriteLoader.SetSprite("Gacha_Img_Gold").Forget();
                    KnightImageSpriteLoader.SetSprite("Gacha_Img_Icon_Gold").Forget();
                    KnighBackImageSpriteLoader.SetSprite("Gacha_Img_GoldOn_Bg").Forget();
                }
            }
            else
            {
                if (resultGradeType == GradeType.COMMON)
                {
                    BackImageSpriteLoader.SetSprite("Gacha_Img_Green").Forget();
                    PieceImageSpriteLoader.SetSprite("Gacha_Img_Icon_Green").Forget();
                    PieceBackImageSpriteLoader.SetSprite("Gacha_Img_Piece_Green").Forget();

                    PieceOnBackImageSpriteLoader.SetSprite("Gacha_Img_Piece_Green").Forget();
                }
                else if (resultGradeType == GradeType.RARE)
                {
                    BackImageSpriteLoader.SetSprite("Gacha_Img_Blue").Forget();
                    PieceImageSpriteLoader.SetSprite("Gacha_Img_Icon_Blue").Forget();
                    PieceBackImageSpriteLoader.SetSprite("Gacha_Img_Piece_Blue").Forget();

                    PieceOnBackImageSpriteLoader.SetSprite("Gacha_Img_Piece_Blue").Forget();
                }
                else if (resultGradeType == GradeType.EPIC)
                {
                    BackImageSpriteLoader.SetSprite("Gacha_Img_Purple").Forget();
                    PieceImageSpriteLoader.SetSprite("Gacha_Img_Icon_Purple").Forget();
                    PieceBackImageSpriteLoader.SetSprite("Gacha_Img_Piece_Purple").Forget();

                    PieceOnBackImageSpriteLoader.SetSprite("Gacha_Img_Piece_Purple").Forget();
                }
                else if (resultGradeType == GradeType.LEGENDARY)
                {
                    BackImageSpriteLoader.SetSprite("Gacha_Img_Gold").Forget();
                    PieceImageSpriteLoader.SetSprite("Gacha_Img_Icon_Gold").Forget();
                    PieceBackImageSpriteLoader.SetSprite("Gacha_Img_Piece_Gold").Forget();

                    PieceOnBackImageSpriteLoader.SetSprite("Gacha_Img_Piece_Gold").Forget();
                }
            }
        }

        public void ShowItem()
        {
            // showParticle.SetActive(true);
            // showParticle.GetComponent<ParticleSystem>().Stop();
            // showParticle.GetComponent<ParticleSystem>().Play();
            if (_rewardItemData.Id.IsCharacter())
            {
                KnightOnObject.SetActive(true);
                PieceOnObject.SetActive(false);

                KnighImageSpriteLoader.SetSprite(SpriteNameParser.GetCharacterSubIllustSprite(characterData.prefab_id)).Forget();
                KnighColorImage.color = characterData.grade_type.GetGradeTypeColor();

                amountText.text = $"x{_rewardItemData.Count}";

                // 슬라이더 처리
                pieceSlider.maxValue = characterData.need_piece;
                var characterPieceId = ItemIdExtensions.GetCharacterPieceId(characterData.id);
                var peiceCount = ServerDataManager.Instance.Inventory.GetCurrency(characterPieceId);
                pieceSlider.value = peiceCount;
                countText.text = $"{peiceCount}/{characterData.need_piece}";

                // if (DataManager.Instance.UserData.isFirstGacha)
                // {
                //     // int tempPiece = 0;
                //     // for (int i = 0; i < _datas.Count; i++)
                //     // {
                //     //     if (i <= _idx)
                //     //     {
                //     //         if (_datas[i].id == characterData.ID)
                //     //         {
                //     //             tempPiece += _datas[i].piece;
                //     //         }
                //     //     }
                //     // }
                //
                //     if (characterData.Level == 0)
                //     {
                //
                //         countText.text = (characterData.Piece+GachaFxByTen.Instance.TempDatas[characterData.ID] - _datas[_idx].piece ).ToString() + "/" + _curEnhanceMetaData.piece;
                //         float rate = (float)(characterData.Piece+GachaFxByTen.Instance.TempDatas[characterData.ID] - _datas[_idx].piece ) / (float)_curEnhanceMetaData.piece;
                //         if (rate > 1)
                //             rate = 1;
                //         pieceSlider.value = rate;
                //     }
                //     else
                //     {
                //         countText.text = (GachaFxByTen.Instance.TempDatas[characterData.ID]+characterData.Piece ).ToString() + "/" + _curEnhanceMetaData.piece;
                //         float rate = (float)(GachaFxByTen.Instance.TempDatas[characterData.ID]+characterData.Piece ) / (float)_curEnhanceMetaData.piece;
                //         if (rate > 1)
                //             rate = 1;
                //         pieceSlider.value = rate;
                //     }
                //
                // }
                // else
                // {
                //     countText.text = (characterData.Piece ).ToString() + "/" + _curEnhanceMetaData.piece;
                //     float rate = (float)(characterData.Piece ) / (float)_curEnhanceMetaData.piece;
                //     if (rate > 1)
                //         rate = 1;
                //     pieceSlider.value = rate;
                // }


                foreach (var star in starObjects)
                {
                    star.SetActive(false);
                }

                // for (int i = 0; i < (int)characterData.grade_type; i++)
                // {
                //     starObjects[i].SetActive(true);
                // }

                if (characterData.grade_type == GradeType.LEGENDARY)
                {
                    for (int i = 0; i < SSRFxObjects.Length; i++)
                    {
                        SSRFxObjects[i].SetActive(true);
                    }
                }
                else if (characterData.grade_type == GradeType.EPIC)
                {
                    for (int i = 0; i < SRFxObjects.Length; i++)
                    {
                        SRFxObjects[i].SetActive(true);
                    }
                }
                //todo : 성흔 이미지 나중에는 캐릭터마다 붙히기
                // KnighSoulImage.sprite = ImageManager.Instance.GetSprite("Illust_Character_Inventory", characterData.ID.ToString());

            }
            else
            {
                KnightOnObject.SetActive(false);
                PieceOnObject.SetActive(true);

                bool isCharacter = characterData != null;

                PieceOnImage.gameObject.SetActive(isCharacter);
                ItemOnImage.gameObject.SetActive(!isCharacter);

                if (isCharacter)
                {
                    PieceOnImageSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPieceSprite(characterData.id)).Forget();
                }
                else
                {
                    ItemOnImageSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(_rewardItemData.Id)).Forget();
                }

                amountText.text = $"x{_rewardItemData.Count}";

                // if (DataManager.Instance.UserData.isFirstGacha)
                // {
                //
                //     if (characterData.Level == 0)
                //     {
                //         int tempPiece = 0;
                //         for (int i = 0; i < _datas.Count; i++)
                //         {
                //             if (_datas[i].id == characterData.ID && _datas[i].piece == 20)
                //             {
                //                 tempPiece += _datas[i].piece;
                //                 break;
                //             }
                //         }
                //         count2Text.text = (GachaFxByTen.Instance.TempDatas[characterData.ID] - tempPiece + characterData.Piece ).ToString() + "/" + _curEnhanceMetaData.piece;
                //         float rate = (float)(GachaFxByTen.Instance.TempDatas[characterData.ID] - tempPiece + characterData.Piece ) / (float)_curEnhanceMetaData.piece;
                //         if (rate > 1)
                //             rate = 1;
                //         piece2Slider.value = rate;
                //     }
                //     else
                //     {
                //         count2Text.text = (GachaFxByTen.Instance.TempDatas[characterData.ID]+characterData.Piece ).ToString() + "/" + _curEnhanceMetaData.piece;
                //         float rate = (float)(GachaFxByTen.Instance.TempDatas[characterData.ID]+characterData.Piece ) / (float)_curEnhanceMetaData.piece;
                //         if (rate > 1)
                //             rate = 1;
                //         piece2Slider.value = rate;
                //     }
                //
                // }
                // else
                // {
                //     count2Text.text = (characterData.Piece ).ToString() + "/" + _curEnhanceMetaData.piece;
                //     float rate = (float)(characterData.Piece ) / (float)_curEnhanceMetaData.piece;
                //     if (rate > 1)
                //         rate = 1;
                //     piece2Slider.value = rate;
                // }
            }
        }
    }
}
