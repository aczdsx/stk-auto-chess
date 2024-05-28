using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;

namespace CookApps.AutoBattler
{
    public class GetNewCharacter : MonoBehaviour
    {
        [Header("Get New Chracter")]
        [SerializeField] private GameObject[] TimeLineObject;

        [SerializeField] private Image sigmaGlow;
        [SerializeField] private Image sigmaMat;
        [SerializeField] private TextMeshProUGUI OpenText;
        [SerializeField] private GameObject LowBodyStaticObject;
        [SerializeField] private GameObject UpperBodyStaticObject;

        [SerializeField] private GameObject[] CharaterIdleObjects;
        [SerializeField] private GameObject[] CharaterStaticObjects;

        [SerializeField] private TextMeshProUGUI[] NameText;
        [SerializeField] private TextMeshProUGUI[] CVText;
        [SerializeField] private TextMeshProUGUI[] DescText;

        [SerializeField] private TextMeshProUGUI[] SynergyText;
        [SerializeField] private Image[] SynergyImage;
        [SerializeField] private Image[] SynergyBGImage;
        [SerializeField] private TextMeshProUGUI[] ClassText;
        [SerializeField] private Image[] ClassImage;
        [SerializeField] private GameObject[] NewObjects;
        // piece
        [SerializeField] private Image PieceImage;
        [SerializeField] private Image PieceImageBG;
        [SerializeField] private GameObject[] GradeImage;
        [SerializeField] private TextMeshProUGUI PieceCharNameText;
        [SerializeField] private TextMeshProUGUI PieceAmountText;
        [SerializeField] private TextMeshProUGUI PieceAllText;
        [SerializeField] private GameObject NewObject;
        [SerializeField] private Image pieceSlider;

        [SerializeField] private GameObject TouchObject;
        private float aniTime = 0f;
        private int characterID;
        // 0 : New SSR
        // 1 : New SR
        // 2 : New R
        // 3 : New N
        // 4 : Own SSR
        // 5 : Own SR
        // 6 : Own R
        // 7 : Own N
        private int timeLineIdx = 0;

        // Start is called before the first frame update
        private GameObject lowObj;
        private GameObject upperObj;
        private GameObject idleObj;
        private GameObject staticObj;
        private PlayableDirector playObj;
        private Action EndAction;
        //private CharacterEnhanceMetaData _curEnhanceMetaData;
        public void SetPiece(int chID, int amount, Action actoin = null)
        {
            EndAction = actoin;
            TouchObject.SetActive(false);
            aniTime = 1.5f;
            for (int i = 0; i < TimeLineObject.Length; i++)
            {
                TimeLineObject[i].SetActive(false);
            }

            for (int i = 0; i < GradeImage.Length; i++)
            {
                GradeImage[i].SetActive(false);
            }
            Character idxCharcater = SpecDataManager.Instance.Character.Get(chID);

            //_curEnhanceMetaData = SpecDataManager.Instance.GetCurrentCharacterEnhanceMetaData((int)idxCharcater.MetaData.grade_value, idxCharcater.Star);
            // if (idxCharcater.IsShowFX == false)
            // {
            //     NewObject.SetActive(true);
            //     if(GachaFxByTen.Instance.pieceTempNew.Contains(idxCharcater.ID))
            //         NewObject.SetActive(false);
            //     GachaFxByTen.Instance.pieceTempNew.Add(idxCharcater.ID);
            // }
            // else
            // {
            //     NewObject.SetActive(false);
            // }
            NewObject.SetActive(false);

            switch (idxCharcater.grade)
            {
                case Grade.LEGEND:
                    GradeImage[3].SetActive(true);
                    break;
                case Grade.EPIC:
                    GradeImage[2].SetActive(true);
                    break;
                case Grade.RARE:
                    GradeImage[1].SetActive(true);
                    break;
                case Grade.COMMON:
                    GradeImage[0].SetActive(true);
                    break;
            }

            TimeLineObject[10].SetActive(true);
            playObj = TimeLineObject[10].GetComponent<PlayableDirector>();
            playObj.Play();
            PieceImageBG.sprite = ImageManager.Instance.GetSprite(Defines.STELLA_ICON_ATLAS_NAME,
                $"Common_ChaPiece_{idxCharcater.grade.ToString()}");
            PieceImage.sprite = ImageManager.Instance.GetSprite(Defines.CHAR_INVENTORY_ATLAS_NAME, $"{chID}");
            //PieceCharNameText.text = Localization.GetLocalizedString($"HEROES_NAME_{chID}");
            PieceAmountText.text = "x" + amount.ToString();
            // if (dataManager.UserData.isFirstGacha == true)
            // {
            //     int temp = GachaFxByTen.Instance.TempDatas[idxCharcater.ID];
            //     if (idxCharcater.Level == 0)
            //     {
            //         if (temp > 20)
            //             temp = GachaFxByTen.Instance.TempDatas[idxCharcater.ID] - 20;
            //     }
            //
            //     PieceAllText.text =  (temp ).ToString() + "/" + _curEnhanceMetaData.piece;
            //     float rate = (float)(temp ) / (float)_curEnhanceMetaData.piece;
            //     if (rate > 1)
            //         rate = 1;
            //     pieceSlider.fillAmount = rate;
            // }
            // else
            // {
            //     PieceAllText.text =  (idxCharcater.Piece ).ToString() + "/" + _curEnhanceMetaData.piece;
            //     float rate = (float)(idxCharcater.Piece ) / (float)_curEnhanceMetaData.piece;
            //     if (rate > 1)
            //         rate = 1;
            //     pieceSlider.fillAmount = rate;
            // }



            // dataManager.AddKnightPieceByGacha(chID,amount, false, EventLocation.GACHA_CHRACTER,null);
            Run.After(System.TimeSpan.FromSeconds(aniTime).Seconds, () =>
            {
                TouchObject.SetActive(true);
            });
        }

        private bool isPlayCharacterBGM = false;
        public void SetChracater(int chID, Action actoin = null, bool isTutorial = false)
        {
            EndAction = actoin;
            TouchObject.SetActive(false);
            characterID = chID;
            isPlayCharacterBGM = false;
            for (int i = 0; i < TimeLineObject.Length; i++)
            {
                TimeLineObject[i].SetActive(false);
            }

            Character idxCharcater = SpecDataManager.Instance.Character.Get(chID);
            // if (idxCharcater.IsShowFX == false)
            // {
            //     // if (dataManager.UserData.isFirstGacha == false)
            //     // {
            //     //     idxCharcater.IsShowFX = true;
            //     // }
            //
            //     switch (idxCharcater.grade)
            //     {
            //         case Grade.LEGEND:
            //             timeLineIdx = 0;
            //             aniTime = 6f;
            //             // string bgmName = dataManager.GetCharacterBGMName(characterID);
            //             // if (!string.IsNullOrEmpty(bgmName))
            //             // {
            //             //     SoundManager.Instance.StopAllSound();
            //             //     SoundManager.Instance.PlayBGM(bgmName);
            //             //     isPlayCharacterBGM = true;
            //             // }
            //             //PlayVoice();
            //             break;
            //         case Grade.EPIC:
            //             timeLineIdx = 1;
            //             aniTime = 1.5f;
            //             break;
            //         case Grade.RARE:
            //             timeLineIdx = 2;
            //             aniTime = 1.5f;
            //             break;
            //         case Grade.COMMON:
            //             timeLineIdx = 3;
            //             aniTime = 1.5f;
            //             break;
            //     }
            //     NewObjects[timeLineIdx].SetActive(true);
            // }
            // else
            // {
            //     switch (idxCharcater.grade)
            //     {
            //         case Grade.LEGEND:
            //             timeLineIdx = 0;
            //             aniTime = 6f;
            //             // string bgmName = dataManager.GetCharacterBGMName(characterID);
            //             // if (!string.IsNullOrEmpty(bgmName))
            //             // {
            //             //     SoundManager.Instance.StopAllSound();
            //             //     SoundManager.Instance.PlayBGM(bgmName);
            //             //     isPlayCharacterBGM = true;
            //             // }
            //             //PlayVoice();
            //             break;
            //         case Grade.EPIC:
            //             timeLineIdx = 5;
            //             aniTime = 1.2f;
            //             break;
            //         case Grade.RARE:
            //             timeLineIdx = 6;
            //             aniTime = 1.2f;
            //             break;
            //         case Grade.COMMON:
            //             timeLineIdx = 7;
            //             aniTime = 1.2f;
            //             break;
            //     }
            //     NewObjects[timeLineIdx].SetActive(false);
            // }
            switch (idxCharcater.grade)
            {
                case Grade.LEGEND:
                    timeLineIdx = 0;
                    aniTime = 6f;
                    // string bgmName = dataManager.GetCharacterBGMName(characterID);
                    // if (!string.IsNullOrEmpty(bgmName))
                    // {
                    //     SoundManager.Instance.StopAllSound();
                    //     SoundManager.Instance.PlayBGM(bgmName);
                    //     isPlayCharacterBGM = true;
                    // }
                    //PlayVoice();
                    break;
                case Grade.EPIC:
                    timeLineIdx = 5;
                    aniTime = 1.2f;
                    break;
                case Grade.RARE:
                    timeLineIdx = 6;
                    aniTime = 1.2f;
                    break;
                case Grade.COMMON:
                    timeLineIdx = 7;
                    aniTime = 1.2f;
                    break;
            }
            NewObjects[timeLineIdx].SetActive(false);



            // if (chID == 100700) //유니  R
            // {
            //     AppEventManager.Instance.SendInitial_Launch_Funnel(40100);
            //      if (isTutorial)
            //      {
            //          timeLineIdx = 8;
            //          aniTime = 6f;
            //          string bgmName = dataManager.GetCharacterBGMName(characterID);
            //          if (!string.IsNullOrEmpty(bgmName))
            //          {
            //              SoundManager.Instance.StopBGM(1);
            //              SoundManager.Instance.PlayBGM(bgmName);
            //              isPlayCharacterBGM = true;
            //          }
            //          PlayVoice();
            //      }
            // }
            // if (chID == 100600) // 필리아 SR
            // {
            //     AppEventManager.Instance.SendInitial_Launch_Funnel(70110);
            //     AppEventManager.Instance.SendInitial_Launch_Funnel(70120);
            //     if (isTutorial)
            //     {
            //         timeLineIdx = 9;
            //         aniTime = 6f;
            //         string bgmName = dataManager.GetCharacterBGMName(characterID);
            //         if (!string.IsNullOrEmpty(bgmName))
            //         {
            //             SoundManager.Instance.StopBGM(1);
            //             SoundManager.Instance.PlayBGM(bgmName);
            //             isPlayCharacterBGM = true;
            //         }
            //
            //         PlayVoice();
            //     }
            // }


            sigmaMat.sprite = ImageManager
                .Instance.GetSprite(Defines.STIGMA_ATLAS_NAME, $"StigmaIcon_{characterID}");
            sigmaGlow.sprite = ImageManager
                .Instance.GetSprite(Defines.STIGMA_ATLAS_NAME, $"StigmaIcon_{characterID}");
            // OpenText.text = Localization.GetLocalizedString($"Character_Dialog_Appearance_{characterID}");
            //play
            TimeLineObject[timeLineIdx].SetActive(true);
            playObj = TimeLineObject[timeLineIdx].GetComponent<PlayableDirector>();
            playObj.Play();
            // NameText[timeLineIdx].text = Localization.GetLocalizedString($"HEROES_NAME_{characterID}");
            // CVText[timeLineIdx].text = Localization.GetLocalizedString($"HEROES_CV_{characterID}");
            // DescText[timeLineIdx].text = Localization.GetLocalizedString($"Character_Dialog_Archieve_{characterID}");

            // SynergyImage[timeLineIdx].sprite = ImageManager.Instance.GetSprite(Defines.ICON_ATLAS_NAME,
            //     $"Icon_{dataManager.GetCharacterSynergy(characterID)}");
            // SynergyBGImage[timeLineIdx].sprite = ImageManager.Instance.GetSprite(Defines.ICON_ATLAS_NAME,
            //     $"BG_{dataManager.GetCharacterSynergy(characterID)}");
            // SynergyText[timeLineIdx].text =
            //     Localization.GetLocalizedString($"SYNERGY_NAME_{dataManager.GetCharacterSynergyUpper(characterID)}");
            // ClassImage[timeLineIdx].sprite = ImageManager.Instance.GetSprite(Defines.ICON_ATLAS_NAME,
            //     $"Icon_{dataManager.GetCharacterClass(characterID)}");
            // ClassText[timeLineIdx].text =
            //     Localization.GetLocalizedString($"SYNERGY_NAME_{dataManager.GetCharacterClassUpper(characterID)}");

            if(lowObj!= null)
                Destroy(lowObj);
            if(upperObj!= null)
                Destroy(upperObj);
            if(staticObj!= null)
                Destroy(staticObj);
            if(idleObj!= null)
                Destroy(idleObj);
            //
            lowObj = AddressablesUtil.Instantiate($"{characterID}_Static", LowBodyStaticObject.transform);
            upperObj = AddressablesUtil.Instantiate($"{characterID}_Static", UpperBodyStaticObject.transform);
            staticObj = AddressablesUtil.Instantiate($"{characterID}_Static", CharaterStaticObjects[timeLineIdx].transform);

            // var offsetScript = CharaterIdleObjects[timeLineIdx].GetComponentInChildren<NormalSkillCharacterOffset>();
            // idleObj = AddressablesUtil.Instantiate($"{characterID}_LD", offsetScript.transform);
            // idleObj.transform.localPosition = offsetScript.GetOffset(characterID);



            // dataManager.AddKnightPieceByGacha(characterID,20, true, EventLocation.GACHA_CHRACTER,null);
            Run.After(System.TimeSpan.FromSeconds(aniTime).Seconds, () =>
            {
                TouchObject.SetActive(true);
            });
        }

        // private void PlayVoice()
        // {
        //     Observable.Timer(System.TimeSpan.FromSeconds(3f))
        //         .Subscribe(_ =>
        //         {
        //             SoundManager.Instance.PlayVOX("VO_1001_"+characterID+"_0_JP");
        //         }).AddTo(this);
        // }
        public void OnClickClose()
        {
            // if (isPlayCharacterBGM)
            // {
            //     SoundManager.Instance.StopAllSound();
            //     if (GameSceneManager.CurrentScene == Scene.Lobby)
            //     {
            //         SoundManager.Instance.PlayBGM(BGMIndex.cash_shop_001);
            //         SoundManager.Instance.PlaySFX(SFXIndex.gacha_result_ambient_001);
            //     }
            //
            //     else if(GameSceneManager.CurrentScene == Scene.InGame)
            //     {
            //         CharacterData characterData = DataManager.Instance.Characters.Find(c => c.ID == InGameUI.Instance.GetMVP());
            //         SoundManager.Instance.PlayVOXInGame("VO_1005_"+characterData.ID+"_0_JP");
            //         SoundManager.Instance.PlaySFX(SFXIndex.ingame_result_victory_001);
            //     }
            // }
            playObj = null;
            if (EndAction != null)
                EndAction();

            Destroy(this.gameObject);
        }

        public void setCheatCharacter(int chID, Action actoin = null)
        {
             EndAction = actoin;
            TouchObject.SetActive(false);
            characterID = chID;
            for (int i = 0; i < TimeLineObject.Length; i++)
            {
                TimeLineObject[i].SetActive(false);
            }

            Character idxCharcater = SpecDataManager.Instance.Character.Get(chID);

            switch (idxCharcater.grade)
            {
                case Grade.LEGEND:
                    timeLineIdx = 0;
                    aniTime = 6f;
                    break;
                case Grade.EPIC:
                    timeLineIdx = 1;
                    aniTime = 3.2f;
                    break;
                case Grade.RARE:
                    timeLineIdx = 2;
                    aniTime = 3.2f;
                    break;
                case Grade.COMMON:
                    timeLineIdx = 3;
                    aniTime = 3.2f;
                    break;
            }
            /*
            if (idxCharcater.Have == false)
            {
                switch (idxCharcater.MetaData.grade)
                {
                    case Grade.LEGEND:
                        timeLineIdx = 0;
                        aniTime = 6f;
                        break;
                    case Grade.EPIC:
                        timeLineIdx = 1;
                        aniTime = 3.2f;
                        break;
                    case Grade.RARE:
                        timeLineIdx = 2;
                        aniTime = 3.2f;
                        break;
                    case Grade.COMMON:
                        timeLineIdx = 3;
                        aniTime = 3.2f;
                        break;
                }

            }
            else
            {
                switch (idxCharcater.MetaData.grade)
                {
                    case Grade.LEGEND:
                        timeLineIdx = 4;
                        break;
                    case Grade.EPIC:
                        timeLineIdx = 5;
                        break;
                    case Grade.RARE:
                        timeLineIdx = 6;
                        break;
                    case Grade.COMMON:
                        timeLineIdx = 7;
                        break;
                }
                aniTime = 2.4f;
            }
            */


            sigmaMat.sprite = ImageManager
                .Instance.GetSprite(Defines.STIGMA_ATLAS_NAME, $"StigmaIcon_{characterID}");
            sigmaGlow.sprite = ImageManager
                .Instance.GetSprite(Defines.STIGMA_ATLAS_NAME, $"StigmaIcon_{characterID}");
            // OpenText.text = Localization.GetLocalizedString($"Character_Dialog_Appearance_{characterID}");
            //play
            TimeLineObject[timeLineIdx].SetActive(true);
            playObj = TimeLineObject[timeLineIdx].GetComponent<PlayableDirector>();
            playObj.Play();
            // NameText[timeLineIdx].text = Localization.GetLocalizedString($"HEROES_NAME_{characterID}");
            // CVText[timeLineIdx].text = Localization.GetLocalizedString($"HEROES_CV_{characterID}");
            // DescText[timeLineIdx].text = Localization.GetLocalizedString($"Character_Dialog_Archieve_{characterID}");

            if(lowObj!= null)
                Destroy(lowObj);
            if(upperObj!= null)
                Destroy(upperObj);
            if(staticObj!= null)
                Destroy(staticObj);
            if(idleObj!= null)
                Destroy(idleObj);

            lowObj = AddressablesUtil.Instantiate($"{characterID}_Static", LowBodyStaticObject.transform);
            upperObj = AddressablesUtil.Instantiate($"{characterID}_Static", UpperBodyStaticObject.transform);
            staticObj = AddressablesUtil.Instantiate($"{characterID}_Static", CharaterStaticObjects[timeLineIdx].transform);

            // var offsetScript = CharaterIdleObjects[timeLineIdx].GetComponentInChildren<NormalSkillCharacterOffset>();
            // idleObj = AddressablesUtil.Instantiate($"{characterID}_LD", offsetScript.transform);
            // idleObj.transform.localPosition = offsetScript.GetOffset(characterID);

            Run.After(System.TimeSpan.FromSeconds(aniTime).Seconds, () =>
            {
                TouchObject.SetActive(true);
            });
        }

    }

}
