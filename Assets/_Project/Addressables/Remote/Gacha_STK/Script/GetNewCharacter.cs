using System;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class GetNewCharacter : MonoBehaviour
    {
        [Header("Get New Chracter")]
        [SerializeField] private GameObject[] TimeLineObject;

        [SerializeField] private Image sigmaGlow;
        [SerializeField] private SpriteLoader sigmaGlowSpriteLoader;
        [SerializeField] private Image sigmaMat;
        [SerializeField] private SpriteLoader sigmaMatSpriteLoader;
        [SerializeField] private TextMeshProUGUI OpenText;
        [SerializeField] private GameObject LowBodyStaticObject;
        [SerializeField] private SpriteLoader lowBodySpriteLoader;
        [SerializeField] private GameObject UpperBodyStaticObject;
        [SerializeField] private SpriteLoader upperBodySpriteLoader;

        [SerializeField] private GameObject[] CharaterIdleObjects;
        [SerializeField] private SpriteLoader[] charaterIdleSpriteLoader;
        [SerializeField] private GameObject[] CharaterStaticObjects;
        [SerializeField] private SpriteLoader[] charaterStaticSpriteLoader;

        [SerializeField] private TextMeshProUGUI[] NameText;
        [SerializeField] private TextMeshProUGUI[] CVText;
        [SerializeField] private TextMeshProUGUI[] DescText;

        [SerializeField] private TextMeshProUGUI[] ElementText;
        [SerializeField] private Image[] SynergyImage;
        [SerializeField] private SpriteLoader[] SynergyImageSpriteLoader;
        [SerializeField] private Image[] SynergyBGImage;
        [SerializeField] private TextMeshProUGUI[] StellaText;
        [SerializeField] private Image[] ClassImage;
        [SerializeField] private SpriteLoader[] ClassImageSpriteLoader;
        [SerializeField] private GameObject[] NewObjects;
        // piece
        [SerializeField] private Image PieceImage;
        [SerializeField] private SpriteLoader PieceImageSpriteLoader;
        [SerializeField] private Image PieceImageBG;
        [SerializeField] private GameObject[] GradeImage;
        [SerializeField] private TextMeshProUGUI PieceCharNameText;
        [SerializeField] private TextMeshProUGUI PieceAmountText;
        [SerializeField] private TextMeshProUGUI PieceAllText;
        [SerializeField] private GameObject NewObject;
        [SerializeField] private Image pieceSlider;
        [SerializeField] private SpriteLoader pieceSliderSpriteLoader;

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
        private AsyncOperationHandle<GameObject> _staticHandle;
        private PlayableDirector playObj;
        private Action EndAction;

        private CharacterInfo _specCharacter;
        //private CharacterEnhanceMetaData _curEnhanceMetaData;

        public void SetPiece(CharacterInfo specCharacter, int amount, Action actoin = null)
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

            _specCharacter = specCharacter;

            //_curEnhanceMetaData = SpecDataManager.Instance.GetCurrentCharacterEnhanceMetaData((int)_specCharacter.MetaData.grade_value, _specCharacter.Star);
            // if (_specCharacter.IsShowFX == false)
            // {
            //     NewObject.SetActive(true);
            //     if(GachaFxByTen.Instance.pieceTempNew.Contains(_specCharacter.ID))
            //         NewObject.SetActive(false);
            //     GachaFxByTen.Instance.pieceTempNew.Add(_specCharacter.ID);
            // }
            // else
            // {
            //     NewObject.SetActive(false);
            // }
            NewObject.SetActive(false);

            switch (_specCharacter.grade_type)
            {
                case GradeType.SSR:
                    GradeImage[3].SetActive(true);
                    break;
                case GradeType.SR:
                    GradeImage[2].SetActive(true);
                    break;
                case GradeType.R:
                    GradeImage[1].SetActive(true);
                    break;
            }

            TimeLineObject[10].SetActive(true);
            playObj = TimeLineObject[10].GetComponent<PlayableDirector>();
            playObj.Play();
            //PieceImageBG.sprite = ImageManager.Instance.GetCharacterPieceSprite(_specCharacter.prefab_id);
            PieceImageSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPieceSprite(_specCharacter.id)).Forget();
            PieceCharNameText.text = LanguageManager.Instance.GetDefaultText(_specCharacter.name_token);
            PieceAmountText.text = "x" + amount.ToString();
            // if (dataManager.UserData.isFirstGacha == true)
            // {
            //     int temp = GachaFxByTen.Instance.TempDatas[_specCharacter.ID];
            //     if (_specCharacter.Level == 0)
            //     {
            //         if (temp > 20)
            //             temp = GachaFxByTen.Instance.TempDatas[_specCharacter.ID] - 20;
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
            //     PieceAllText.text =  (_specCharacter.Piece ).ToString() + "/" + _curEnhanceMetaData.piece;
            //     float rate = (float)(_specCharacter.Piece ) / (float)_curEnhanceMetaData.piece;
            //     if (rate > 1)
            //         rate = 1;
            //     pieceSlider.fillAmount = rate;
            // }



            // dataManager.AddKnightPieceByGacha(chID,amount, false, EventLocation.GACHA_CHRACTER,null);
            Run.After(System.TimeSpan.FromSeconds(aniTime).Seconds, () =>
            {
                if (this != null)
                {
                    TouchObject?.SetActive(true);
                }
            });
        }

        private bool isPlayCharacterBGM = false;
        public void SetChracater(CharacterInfo specCharacter, Action actoin = null, bool isTutorial = false)
        {
            EndAction = actoin;
            TouchObject.SetActive(false);
            isPlayCharacterBGM = false;
            for (int i = 0; i < TimeLineObject.Length; i++)
            {
                TimeLineObject[i].SetActive(false);
            }

            _specCharacter = specCharacter;
            // if (_specCharacter.IsShowFX == false)
            // {
            //     // if (dataManager.UserData.isFirstGacha == false)
            //     // {
            //     //     _specCharacter.IsShowFX = true;
            //     // }
            //
            //     switch (_specCharacter.grade)
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
            //     switch (_specCharacter.grade)
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
            switch (_specCharacter.grade_type)
            {
                case GradeType.SSR:
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
                case GradeType.SR:
                    timeLineIdx = 5;
                    aniTime = 1.2f;
                    break;
                case GradeType.R:
                    timeLineIdx = 6;
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


            sigmaMatSpriteLoader.SetSprite(SpriteNameParser.GetCharacterStigmaSprite(_specCharacter.prefab_id)).Forget();
            sigmaGlowSpriteLoader.SetSprite(SpriteNameParser.GetCharacterStigmaSprite(_specCharacter.prefab_id)).Forget();

            var quoteData = SpecDataManager.Instance.GetCharacterQuotesDataByPrefabID(_specCharacter.prefab_id);
            if (quoteData != null)
            {
                OpenText.text = LanguageManager.Instance.GetDefaultText(quoteData.dialog_token);
            }

            //play
            TimeLineObject[timeLineIdx].SetActive(true);
            playObj = TimeLineObject[timeLineIdx].GetComponent<PlayableDirector>();
            playObj.Play();
            NameText[timeLineIdx].text = LanguageManager.Instance.GetDefaultText(_specCharacter.name_token);
            // CVText[timeLineIdx].text = Localization.GetLocalizedString($"HEROES_CV_{characterID}");
            DescText[timeLineIdx].text = LanguageManager.Instance.GetDefaultText(_specCharacter.desc_token);

            SynergyImageSpriteLoader[timeLineIdx].SetSprite(SpriteNameParser.GetSpriteName(_specCharacter.character_element_type)).Forget();
            // SynergyBGImage[timeLineIdx].sprite = ImageManager.Instance.GetSprite(Defines.ICON_ATLAS_NAME,
            //     $"BG_{dataManager.GetCharacterSynergy(characterID)}");
            ElementText[timeLineIdx].text = LanguageManager.Instance.GetElementTest(_specCharacter.character_element_type);
            ClassImageSpriteLoader[timeLineIdx].SetSprite(SpriteNameParser.GetSpriteName(_specCharacter.character_stella_type)).Forget();
            StellaText[timeLineIdx].text = LanguageManager.Instance.GetStellaText(_specCharacter.character_stella_type);

            if (lowObj != null)
                Destroy(lowObj);
            if (upperObj != null)
                Destroy(upperObj);
            if (_staticHandle.IsValid())
            {
                Addressables.ReleaseInstance(_staticHandle);
                _staticHandle = default;
            }
            staticObj = null;
            if (idleObj != null)
                Destroy(idleObj);

            var targetSpriteName = SpriteNameParser.GetCharacterIllustSprite(_specCharacter.prefab_id);

            //lowObj = AddressablesUtil.Instantiate($"{characterID}_Static", LowBodyStaticObject.transform);
            //upperObj = AddressablesUtil.Instantiate($"{characterID}_Static", UpperBodyStaticObject.transform);
            lowBodySpriteLoader.SetSprite(targetSpriteName).Forget();
            //LowBodyStaticObject.GetComponent<RectTransform>().sizeDelta = new Vector2(targetSprite.rect.width, targetSprite.rect.height);
            upperBodySpriteLoader.SetSprite(targetSpriteName).Forget();
            //UpperBodyStaticObject.GetComponent<RectTransform>().sizeDelta = new Vector2(targetSprite.rect.width, targetSprite.rect.height);

            _staticHandle = Addressables.InstantiateAsync($"{characterID}_Static", CharaterStaticObjects[timeLineIdx].transform);
            staticObj = _staticHandle.WaitForCompletion();

            CharaterIdleObjects[timeLineIdx].gameObject.SetActive(true);
            charaterIdleSpriteLoader[timeLineIdx].SetSprite(targetSpriteName).Forget();
            //CharaterIdleObjects[timeLineIdx].GetComponent<RectTransform>().sizeDelta = new Vector2(targetSprite.rect.width, targetSprite.rect.height);
            charaterStaticSpriteLoader[timeLineIdx].SetSprite(SpriteNameParser.GetCharacterIllustSprite(_specCharacter.prefab_id)).Forget();

            // var offsetScript = CharaterIdleObjects[timeLineIdx].GetComponentInChildren<NormalSkillCharacterOffset>();
            // idleObj = AddressablesUtil.Instantiate($"{characterID}_LD", offsetScript.transform);
            // idleObj.transform.localPosition = offsetScript.GetOffset(characterID);



            // dataManager.AddKnightPieceByGacha(characterID,20, true, EventLocation.GACHA_CHRACTER,null);
            Run.After(System.TimeSpan.FromSeconds(aniTime).Seconds, () =>
            {
                if (this != null)
                {
                    TouchObject?.SetActive(true);
                }
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

            SoundManager.Instance.StopSFX(SoundFX.snd_sfx_gacha_result_ambient_001);

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

            CharacterInfo _specCharacter = SpecDataManager.Instance.CharacterInfo.Get(chID);

            switch (_specCharacter.grade_type)
            {
                case GradeType.SSR:
                    timeLineIdx = 0;
                    aniTime = 6f;
                    break;
                case GradeType.SR:
                    timeLineIdx = 1;
                    aniTime = 3.2f;
                    break;
                case GradeType.R:
                    timeLineIdx = 2;
                    aniTime = 3.2f;
                    break;
            }
            /*
            if (_specCharacter.Have == false)
            {
                switch (_specCharacter.MetaData.grade)
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
                switch (_specCharacter.MetaData.grade)
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


            sigmaMatSpriteLoader.SetSprite(SpriteNameParser.GetCharacterStigmaSprite(_specCharacter.prefab_id)).Forget();
            sigmaGlowSpriteLoader.SetSprite(SpriteNameParser.GetCharacterStigmaSprite(_specCharacter.prefab_id)).Forget();

            var quoteData = SpecDataManager.Instance.GetCharacterQuotesDataByPrefabID(_specCharacter.prefab_id);
            OpenText.text = LanguageManager.Instance.GetDefaultText(quoteData.dialog_token);
            //play
            TimeLineObject[timeLineIdx].SetActive(true);
            playObj = TimeLineObject[timeLineIdx].GetComponent<PlayableDirector>();
            playObj.Play();

            NameText[timeLineIdx].text = LanguageManager.Instance.GetDefaultText(_specCharacter.name_token);
            // CVText[timeLineIdx].text = Localization.GetLocalizedString($"HEROES_CV_{characterID}");
            DescText[timeLineIdx].text = LanguageManager.Instance.GetDefaultText(_specCharacter.desc_token);

            if (lowObj != null)
                Destroy(lowObj);
            if (upperObj != null)
                Destroy(upperObj);
            if (_staticHandle.IsValid())
            {
                Addressables.ReleaseInstance(_staticHandle);
                _staticHandle = default;
            }
            staticObj = null;
            if (idleObj != null)
                Destroy(idleObj);

            var targetSpriteName = SpriteNameParser.GetCharacterIllustSprite(_specCharacter.prefab_id);

            lowBodySpriteLoader.SetSprite(targetSpriteName).Forget();
            //LowBodyStaticObject.GetComponent<RectTransform>().sizeDelta = new Vector2(targetSprite.rect.width, targetSprite.rect.height);
            upperBodySpriteLoader.SetSprite(targetSpriteName).Forget();
            //UpperBodyStaticObject.GetComponent<RectTransform>().sizeDelta = new Vector2(targetSprite.rect.width, targetSprite.rect.height);

            CharaterIdleObjects[timeLineIdx].gameObject.SetActive(true);
            charaterIdleSpriteLoader[timeLineIdx].SetSprite(targetSpriteName).Forget();
            //CharaterIdleObjects[timeLineIdx].GetComponent<RectTransform>().sizeDelta = new Vector2(targetSprite.rect.width, targetSprite.rect.height);

            charaterStaticSpriteLoader[timeLineIdx].SetSprite(SpriteNameParser.GetCharacterIllustSprite(_specCharacter.prefab_id)).Forget();

            // var offsetScript = CharaterIdleObjects[timeLineIdx].GetComponentInChildren<NormalSkillCharacterOffset>();
            // idleObj = AddressablesUtil.Instantiate($"{characterID}_LD", offsetScript.transform);
            // idleObj.transform.localPosition = offsetScript.GetOffset(characterID);

            Run.After(System.TimeSpan.FromSeconds(aniTime).Seconds, () =>
            {
                TouchObject.SetActive(true);
            });
        }

        private void OnDestroy()
        {
            if (_staticHandle.IsValid())
                Addressables.ReleaseInstance(_staticHandle);
        }
    }

}
