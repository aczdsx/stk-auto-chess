using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class GachaItem : MonoBehaviour
    {
        [Header("뒷 이미지")]
        [SerializeField] private Image BackImage;
        [SerializeField] private Image PieceImage;
        [SerializeField] private Image PieceBackImage;
        [SerializeField] private Image KnightImage;

        [Header("열렸을때 기사")]
        [SerializeField] private GameObject KnightOnObject;
        [SerializeField] private Image KnighBackImage;
        [SerializeField] private Image KnighImage;
        [SerializeField] private Image KnighColorImage;
        [SerializeField] private Image KnighSoulImage;
        [SerializeField] private List<GameObject> starObjects;

        [SerializeField] private GameObject SliderObject;
        [SerializeField] private Slider pieceSlider;
        [SerializeField] private TextMeshProUGUI countText;

        [Header("열렸을때 조각")]
        [SerializeField] private GameObject PieceOnObject;
        [SerializeField] private Image PieceOnBackImage;
        [SerializeField] private Image PieceOnImage;

        [SerializeField] private Slider piece2Slider;
        [SerializeField] private TextMeshProUGUI count2Text;


        [SerializeField] private GameObject showParticle;
        [SerializeField] Image SigmaImage;

        [Header("완제 소녀 연출")]
        [SerializeField] private GameObject[] SSRFxObjects;
        [SerializeField] private GameObject[] SRFxObjects;

        private int pieceCount = 0;
        private SpecCharacter characterData;

        private RewardItem _rewardItemData;

        // private CharacterEnhanceMetaData _curEnhanceMetaData;
        public void InitItem(RewardItem _rewardItem)
        {
            this.gameObject.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            //_curEnhanceMetaData = SpecDataManager.Instance.GetCurrentCharacterEnhanceMetaData((int)_characterData.MetaData.grade_value, _characterData.Star);

            _rewardItemData = _rewardItem;

            if (_rewardItem.Type == ItemType.CHARACTER_PIECE)
            {
                characterData = SpecDataManager.Instance.GetCharacterData(_rewardItem.Key);
            }

            showParticle.SetActive(false);

            pieceCount = _rewardItem.Count;
            BackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Gray");
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
            if (pieceCount == 20)
            {
                PieceBackImage.gameObject.SetActive(false);
                KnightImage.gameObject.SetActive(true);

                KnightImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Icon_Gray");

            }
            else
            {
                PieceBackImage.gameObject.SetActive(true);
                KnightImage.gameObject.SetActive(false);

                PieceBackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Piece_Gray");
                PieceImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Icon_Gray");
            }

            if (characterData != null)
            {
                SigmaImage.sprite = ImageManager.Instance.GetSprite(Defines.STIGMA_ATLAS_NAME, $"StigmaIcon_{characterData.prefab_id}");
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

            if (pieceCount == 20)
            {
                if (resultGradeType == GradeType.COMMON)
                {
                    BackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Green");
                    KnightImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Icon_Green");
                    KnighBackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_GreenOn_Bg");

                }
                else if (resultGradeType == GradeType.RARE)
                {
                    BackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Blue");
                    KnightImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Icon_Blue");
                    KnighBackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_BlueOn_Bg");
                }
                else if (resultGradeType == GradeType.EPIC)
                {
                    BackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Purple");
                    KnightImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Icon_Purple");
                    KnighBackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_PurpleOn_Bg");
                }
                else if (resultGradeType == GradeType.LEGEND)
                {
                    BackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Gold");
                    KnightImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Icon_Gold");
                    KnighBackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_GoldOn_Bg");
                }
            }
            else
            {
                if (resultGradeType == GradeType.COMMON)
                {
                    BackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Green");
                    PieceImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Icon_Green");
                    PieceBackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Piece_Green");

                    PieceOnBackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Piece_Green");
                }
                else if (resultGradeType == GradeType.RARE)
                {
                    BackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Blue");
                    PieceImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Icon_Blue");
                    PieceBackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Piece_Blue");

                    PieceOnBackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Piece_Blue");
                }
                else if (resultGradeType == GradeType.EPIC)
                {
                    BackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Purple");
                    PieceImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Icon_Purple");
                    PieceBackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Piece_Purple");

                    PieceOnBackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Piece_Purple");
                }
                else if (resultGradeType == GradeType.LEGEND)
                {
                    BackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Gold");
                    PieceImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Icon_Gold");
                    PieceBackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Piece_Gold");

                    PieceOnBackImage.sprite = ImageManager.Instance.GetSprite(Defines.GACHA_ATLAS_NAME, "Gacha_Img_Piece_Gold");
                }
            }
        }

        public void ShowItem()
        {
            // showParticle.SetActive(true);
            // showParticle.GetComponent<ParticleSystem>().Stop();
            // showParticle.GetComponent<ParticleSystem>().Play();
            if (pieceCount == 20)
            {
                KnightOnObject.SetActive(true);
                PieceOnObject.SetActive(false);

                KnighImage.sprite = ImageManager.Instance.GetCharacterSubIllustSprite(characterData.prefab_id);
                KnighColorImage.color = ImageManager.Instance.GetGradeTypeColor(characterData.grade_type);
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

                for (int i = 0; i < (int)characterData.grade_type; i++)
                {
                    starObjects[i].SetActive(true);
                }

                if (characterData.grade_type == GradeType.LEGEND)
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

                if (characterData != null)
                {
                    PieceOnImage.sprite = ImageManager.Instance.GetCharacterPieceSprite(characterData.prefab_id);
                }
                else
                {
                    PieceOnImage.sprite = ImageManager.Instance.GetItemSprite(_rewardItemData.Type);
                }
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
