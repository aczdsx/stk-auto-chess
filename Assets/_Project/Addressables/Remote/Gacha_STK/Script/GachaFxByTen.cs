using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using Random = UnityEngine.Random;

namespace CookApps.AutoBattler
{
    public class GachaFxByTen : GameObjectSingleton<GachaFxByTen>
    {
        [SerializeField] private GameObject[] SSR10Objects;
        [SerializeField] private GameObject[] Normal10Objects;
        [SerializeField] private GameObject[] SSR10_1Objects;
        [SerializeField] private GameObject[] Normal10_1Objects;
        [SerializeField] private GameObject[] SSR1Objects;
        [SerializeField] private GameObject[] Normal1Objects;

        [SerializeField] private List<GachaItem> Gach10Items;
        [SerializeField] private List<GachaItem> Gach1Items;

        private List<RewardItem> _datas = null;
        private bool isClick = false;
        private bool isSkip = false;
        [SerializeField] private GameObject SkipObject;
        [SerializeField] private GameObject CloseObject;
        [SerializeField] private GameObject ReGachaObject;
        [SerializeField] private GameObject ConfirmObject;
        [SerializeField] private GameObject BlockerObject;
        [SerializeField] private GameObject FirstGachaConfirmPopup;
        [SerializeField] private GameObject ReGachaConfirmPopup;
        [SerializeField] private GameObject TitleObject;
        [SerializeField] private GameObject[] SkipParticleObjects;

        [SerializeField] private TextMeshProUGUI ConfirmButtonText;
        // [SerializeField] private PlayableAsset[] TimeLine10;
        // [SerializeField] private PlayableAsset[] TimeLine1;

        [SerializeField] private PlayableDirector[] pds;

        [SerializeField] private GameObject[] SSR10StarObjects;
        [SerializeField] private GameObject[] NormalStar10Objects;
        [SerializeField] private GameObject[] SSR1StarObjects;
        [SerializeField] private GameObject[] NormalStar1Objects;


        [SerializeField] private GameObject[] NormaGachaFX;
        [SerializeField] private GameObject[] SSRGachaFX;

        [SerializeField] private GameObject[] ParticleObjects;
        private List<GachaItem> GachItems = null;
        private bool IsOne = false;
        //public Dictionary<int, int> TempDatas = null;
        private bool isClickRetry = false;
        public List<int> pieceTempNew = null;
        public void SetItem(List<RewardItem> datas, bool isOne = false)
        {
            if (datas == null)
            {
                Destroy(this);
                return;
            }
            isClickRetry = false;
            // if (TempDatas == null)
            // {
            //     TempDatas = new Dictionary<int, int>();
            // }
            // else
            // {
            //     TempDatas.Clear();
            // }
            if (pieceTempNew == null)
            {
                pieceTempNew = new List<int>();
            }
            else
            {
                pieceTempNew.Clear();
            }

            // for (int i = 0; i < datas.Count; i++)
            // {
            //     if (TempDatas.ContainsKey(datas[i].character_id))
            //         TempDatas[datas[i].character_id] += datas[i].need_piece;
            //     else
            //     {
            //         TempDatas.Add(datas[i].character_id, datas[i].need_piece);
            //     }
            // }

            FirstGachaConfirmPopup.SetActive(false);
            ReGachaConfirmPopup.SetActive(false);
            // if (dataManager.UserData.isFirstGacha)
            // {
            //     ConfirmButtonText.text = Localization.GetLocalizedString("TUTORIAL_GACHA_CONFIRM_BTN");
            // }
            // else
            // {
            //     ConfirmButtonText.text = Localization.GetLocalizedString("COMMON_CLOSE");
            // }
            IsOne = isOne;
            isSkip = false;
            isClick = true;
            isSkipGet = false;
            SkipObject.SetActive(true);
            CloseObject.SetActive(false);
            TitleObject.SetActive(false);
            ReGachaObject.SetActive(false);
            ConfirmObject.SetActive(false);
            BlockerObject.SetActive(true);
            _datas = datas;
            skipCnt = 0;
            cnt = 0;
            bool isHaveSSR = false;
            int ssrCount = 0;
            for (int i = 0; i < SSR10StarObjects.Length; i++)
            {
                SSR10StarObjects[i].SetActive(false);
                NormalStar10Objects[i].SetActive(false);
            }

            for (int i = 0; i < ParticleObjects.Length; i++)
            {
                ParticleObjects[i].SetActive(true);
            }

            for (int i = 0; i < datas.Count; i++)
            {
                if (datas[i].Id.GetCharacterId(out int characterId))
                {
                    var specData = SpecDataManager.Instance.GetCharacterData(characterId);

                    if (specData != null && specData.grade_type == GradeType.LEGENDARY && datas[i].Id.IsCharacter())
                    {
                        ssrCount++;
                        isHaveSSR = true;
                        if(isOne == false)
                        {
                            SSR10StarObjects[i].SetActive(true);
                            SSR10StarObjects[i+10].SetActive(true);
                            NormalStar10Objects[i].SetActive(false);
                            NormalStar10Objects[i+10].SetActive(false);
                        }
                    }
                    else
                    {
                        if(isOne == false)
                        {
                            SSR10StarObjects[i].SetActive(false);
                            SSR10StarObjects[i+10].SetActive(false);
                            NormalStar10Objects[i].SetActive(true);
                            NormalStar10Objects[i+10].SetActive(true);
                        }
                    }
                }
                else
                {
                    if(isOne == false)
                    {
                        SSR10StarObjects[i].SetActive(false);
                        SSR10StarObjects[i+10].SetActive(false);
                        NormalStar10Objects[i].SetActive(true);
                        NormalStar10Objects[i+10].SetActive(true);
                    }
                }
            }

            SetSkipList();
            if (ssrCount > 1)
            {
                int[] resultNum = generatorRandomNumber(ssrCount -1);
                for (int i = 0; i < SSR10Objects.Length; i++)
                {
                    if (resultNum.Contains(i))
                    {
                        SSR10Objects[i].SetActive(true);
                        SSR10_1Objects[i].SetActive(true);
                        Normal10Objects[i].SetActive(false);
                        Normal10_1Objects[i].SetActive(false);
                    }
                    else
                    {
                        SSR10Objects[i].SetActive(false);
                        SSR10_1Objects[i].SetActive(false);
                        Normal10Objects[i].SetActive(true);
                        Normal10_1Objects[i].SetActive(true);
                    }
                }
            }



            if (isOne)
            {
                SSR1StarObjects[0].SetActive(isHaveSSR);
                SSR1StarObjects[1].SetActive(isHaveSSR);
                NormalStar1Objects[0].SetActive(!isHaveSSR);
                NormalStar1Objects[1].SetActive(!isHaveSSR);

                for (int i = 0; i < SSR1Objects.Length; i++)
                {
                    SSR1Objects[i].SetActive(isHaveSSR);
                }
                for (int i = 0; i < Normal1Objects.Length; i++)
                {
                    Normal1Objects[i].SetActive(!isHaveSSR);
                }

                if (isHaveSSR)
                {
                    // 1개 획득할때 SSR 사운드
                    SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_start_003);
                }
                else
                {
                    // 1개 획득할때 노말 사운드
                    SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_start_004);
                }
            }
            else
            {

                if (isHaveSSR)
                {
                    // 10개 획득할때 SSR 사운드
                    SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_start_001);
                }
                else
                {
                    // 10개 획득할때 노말 사운드
                    SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_start_002);
                }
            }
            for (int i = 0; i < SSRGachaFX.Length; i++)
            {
                SSRGachaFX[i].SetActive(isHaveSSR);
            }
            for (int i = 0; i < NormaGachaFX.Length; i++)
            {
                NormaGachaFX[i].SetActive(!isHaveSSR);
            }



            for(int i = 0 ; i < pds.Length; i++)
                pds[i].gameObject.SetActive(false);

            if (isOne)
            {

                GachItems = Gach1Items;
                pds[2].gameObject.SetActive(true);
                pds[2].Play();
            }
            else
            {

                GachItems = Gach10Items;
                pds[0].gameObject.SetActive(true);
                pds[0].Play();
            }

            Run.After(System.TimeSpan.FromSeconds(9.5).Seconds, () =>
            {
                if (isSkip == false)
                {
                    if (isOne)
                    {
                        pds[2].time = pds[2].duration;
                        pds[2].gameObject.SetActive(false);
                        pds[3].gameObject.SetActive(true);

                        pds[3].Play();
                        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_starfall_002);
                    }
                    else
                    {
                        pds[0].time = pds[0].duration;
                        pds[0].gameObject.SetActive(false);
                        pds[1].gameObject.SetActive(true);

                        pds[1].Play();
                        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_starfall_001);
                    }
                }
            });

            for (int i = 0; i < datas.Count; i++)
            {
                GachItems[i].InitItem(datas[i]);
            }


            Run.After(System.TimeSpan.FromSeconds(5).Seconds, () =>
            {
                if (isSkip == false)
                    SkipObject.SetActive(false);
            });
        }

        private IDisposable obj1 = null;
        private IDisposable obj2 = null;
        private List<RewardItem> skipDatas = null;

        private void SetSkipList()
        {
            if(skipDatas == null)
                skipDatas = new List<RewardItem>();
            else
            {
                skipDatas.Clear();
            }
            for (int i = 0; i < _datas.Count; i++)
            {
                if (!_datas[i].Id.GetCharacterId(out int skipCharId))
                    continue;
                if(_datas[i].Id != skipCharId)
                    continue;
                CharacterInfo idxCharcater = SpecDataManager.Instance.GetCharacterData(skipCharId);
                if (idxCharcater != null && idxCharcater.grade_type == GradeType.LEGENDARY)
                {
                    skipDatas.Add(_datas[i]);
                }
                else
                {
                     // if (idxCharcater.IsShowFX == false)
                     // {
                     //     if (idxCharcater.Level == 0)
                     //     {
                     //         // if (dataManager.UserData.isFirstGacha)
                     //         // {
                     //         //     if (skipDatas.Contains(_datas[i]) == false)
                     //         //     {
                     //         //         skipDatas.Add(_datas[i]);
                     //         //         idxCharcater.IsShowFX = true;
                     //         //     }
                     //         // }
                     //     }
                     //     else
                     //     {
                     //         if (skipDatas.Contains(_datas[i]) == false)
                     //         {
                     //             skipDatas.Add(_datas[i]);
                     //             idxCharcater.IsShowFX = true;
                     //         }
                     //
                     //     }
                     // }
                }

            }
        }

        private bool CheckSkipCharacter()
        {
            bool result = false;


            if (skipDatas.Count > 0)
            {
                result = true;
                ShowSkipCharacterFX();
            }

            return result;
        }

        private int skipCnt = 0;
        private void ShowSkipCharacterFX()
        {
            if (fx != null)
            {
                Destroy(fx);
                fx = null;
            }

            if (skipDatas.Count <= skipCnt)
            {
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_open_cha001);
                foreach (var obj in GachItems)
                {

                    obj.gameObject.SetActive(true);
                    obj.ChangeItem();
                    obj.ShowItem();
                    ChangeEffect(obj);
                    CloseObject.SetActive(true);
                    // if (DataManager.Instance.UserData.isFirstGacha == true)
                    // {
                    //     ReGachaObject.SetActive(true);
                    //     ConfirmObject.SetActive(false);
                    // }
                    // else
                    // {
                    //     ConfirmObject.SetActive(true);
                    // }
                    ConfirmObject.SetActive(true);

                    BlockerObject.SetActive(true);
                }
                TitleObject.SetActive(true);
                return;
            }
            if (BlockerObject != null)
                BlockerObject.SetActive(false);
            fx = Addressables.InstantiateAsync("GetNewCharacter").WaitForCompletion();
            if (skipDatas[skipCnt].Id.GetCharacterId(out int skipCharacterId))
            {
                var idxCharacter = SpecDataManager.Instance.GetCharacterData(skipCharacterId);
                fx.GetComponent<GetNewCharacter>().SetChracater(idxCharacter, ShowSkipCharacterFX);
            }
            skipCnt++;
        }
        private int[] generatorRandomNumber(int count)
        {
            int[] intArray = new int[count];


            for(int loop = 0; loop < count; loop++)
            {
                // 랜덤 값 생성
                int randNumber = Random.Range(0, 9);

                // 랜덤 값이 배열에 존재하면 loop를 1 감소
                if (intArray.Contains(randNumber))
                {
                    loop--;
                }
                // 랜덤 값이 배열에 없으면 배열에 추가
                else
                {
                    intArray[loop] = randNumber;
                }
            }
            return intArray;
        }

        public void ChangeImageCard(int idx)
        {
            GachItems[idx].ChangeItem();
            if (idx == 0)
            {
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_result_ambient_001);
                SkipObject.SetActive(true);

            }

            int id = 0;
            if (IsOne == false)
                id = 9;

            if (idx == id)
            {
                SkipObject.SetActive(false);
                Run.After(System.TimeSpan.FromSeconds(1f).Seconds, () =>
                {
                    SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_open_cha001);
                    // foreach (var obj in GachItems)
                    // {
                    //     // obj.ShowItem();
                    //     ChangeEffect(obj);
                    // }
                    SkipObject.SetActive(true);
                    ShowGetFX();
                });
            }
        }

        private int cnt = 0;
        private GameObject fx = null;

        private void ShowGetFX()
        {
            if (fx != null)
            {
                Destroy(fx);
                fx = null;
            }

            if(isSkipGet == true)
                return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_open_cha001);
            
            if (cnt > _datas.Count - 1)
            {
                //SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_open_cha001);
                foreach (var obj in GachItems)
                {
                    // obj.ShowItem();

                    ChangeEffect(obj);
                }
                BlockerObject.SetActive(true);
                isClick = false;
                SkipObject.SetActive(false);
                CloseObject.SetActive(true);
                TitleObject.SetActive(true);
                // if (dataManager.UserData.isFirstGacha)
                // {
                //     ReGachaObject.SetActive(true);
                //     ConfirmObject.SetActive(false);
                // }
                // else
                // {
                //     ConfirmObject.SetActive(true);
                // }
                ConfirmObject.SetActive(true);

                return;
            }

            if (BlockerObject != null)
                BlockerObject.SetActive(false);

            if (!_datas[cnt].Id.GetCharacterId(out int fxCharacterId))
            {
                cnt++;
                ShowGetFX();
                return;
            }

            CharacterInfo idxCharcater = SpecDataManager.Instance.GetCharacterData(fxCharacterId);
            if (idxCharcater == null)
            {
                cnt++;
                ShowGetFX();
                return;
            }

            if (_datas[cnt].Id.IsCharacter())
            {
                if (idxCharcater.grade_type == GradeType.LEGENDARY)
                {
                    fx = Addressables.InstantiateAsync("GetNewCharacter").WaitForCompletion();
                    fx.GetComponent<GetNewCharacter>().SetChracater(idxCharcater, ShowGetFX);

                    if (skipDatas.Contains(_datas[cnt]))
                    {
                        skipDatas.Remove(_datas[cnt]);
                    }
                }
                else
                {
                    // if (dataManager.UserData.isFirstGacha)
                    // {
                    //     if (idxCharcater.Level == 0)
                    //     {
                    //         fx = Addressables.InstantiateAsync("GetNewCharacter").WaitForCompletion();
                    //         fx.GetComponent<GetNewCharacter>().SetChracater(_datas[cnt].id, ShowGetFX);
                    //         if (skipDatas.Contains(_datas[cnt]))
                    //         {
                    //             skipDatas.Remove(_datas[cnt]);
                    //         }
                    //     }
                    //     else
                    //     {
                    //         fx = Addressables.InstantiateAsync("GetNewCharacter").WaitForCompletion();
                    //         fx.GetComponent<GetNewCharacter>().SetChracater(_datas[cnt].id, ShowGetFX);
                    //     }
                    // }
                    // else
                    // {
                    //     if (idxCharcater.IsShowFX == false)
                    //     {
                    //         fx = Addressables.InstantiateAsync("GetNewCharacter").WaitForCompletion();
                    //         fx.GetComponent<GetNewCharacter>().SetChracater(_datas[cnt].id, ShowGetFX);
                    //         if (skipDatas.Contains(_datas[cnt]))
                    //         {
                    //             skipDatas.Remove(_datas[cnt]);
                    //         }
                    //     }
                    //     else
                    //     {
                    //         fx = Addressables.InstantiateAsync("GetNewCharacter").WaitForCompletion();
                    //         fx.GetComponent<GetNewCharacter>().SetChracater(_datas[cnt].id, ShowGetFX);
                    //     }
                    // }
                    // if (idxCharcater.IsShowFX == false)
                    // {
                    //     fx = Addressables.InstantiateAsync("GetNewCharacter").WaitForCompletion();
                    //     fx.GetComponent<GetNewCharacter>().SetChracater(_datas[cnt].id, ShowGetFX);
                    //     if (skipDatas.Contains(_datas[cnt]))
                    //     {
                    //         skipDatas.Remove(_datas[cnt]);
                    //     }
                    // }
                    // else
                    // {
                    //     fx = Addressables.InstantiateAsync("Gacha_VFX_Ver_Final_01").WaitForCompletion();
                    //     fx.GetComponent<GetNewCharacter>().SetChracater(_datas[cnt].id, ShowGetFX);
                    // }

                    fx = Addressables.InstantiateAsync("GetNewCharacter").WaitForCompletion();
                    fx.GetComponent<GetNewCharacter>().SetChracater(idxCharcater, ShowGetFX);

                }
            }
            else
            {
                fx = Addressables.InstantiateAsync("GetNewCharacter").WaitForCompletion();
                fx.GetComponent<GetNewCharacter>().SetPiece(idxCharcater, _datas[cnt].Count, ShowGetFX);
            }

            cnt++;
        }

        private void ChangeEffect(GachaItem gachaItem)
        {
            if (gachaItem == null)
                return;

            //gachaItem.GetComponent<CanvasGroup>().DOFade(0, 0.2f);
            gachaItem.GetComponent<CanvasGroup>().alpha = 0;
            Run.After(System.TimeSpan.FromSeconds(0.2f).Seconds, () =>
            {
                gachaItem.ShowItem();
                //gachaItem.GetComponent<CanvasGroup>().DOFade(1, 0.2f);
                gachaItem.GetComponent<CanvasGroup>().alpha = 1;
            });
        }

        private bool isSkipGet = false;
        public void OnClickSkip()
        {

            isClick = false;
            SkipObject.SetActive(false);
            isSkip = true;
            if (fx != null)
            {
                Destroy(fx);
                fx = null;
            }
            SoundManager.Instance.StopAllSound();
            if (IsOne)
            {

                if (pds[2].gameObject.activeSelf == true)
                {
                    pds[2].time = pds[2].duration;
                    pds[2].gameObject.SetActive(false);
                    pds[3].gameObject.SetActive(true);
                    pds[3].Play();
                    SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_starfall_002);
                }
                else
                {
                    for (int i = 0; i < SkipParticleObjects.Length; i++)
                    {
                        SkipParticleObjects[i].SetActive(false);
                    }
                    isSkipGet = true;
                    if (CheckSkipCharacter() == false)
                    {
                        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_open_cha001);
                        foreach (var obj in GachItems)
                        {

                            obj.gameObject.SetActive(true);
                            obj.ChangeItem();
                            obj.ShowItem();
                            ChangeEffect(obj);
                            CloseObject.SetActive(true);
                            // if (dataManager.UserData.isFirstGacha)
                            // {
                            //     ReGachaObject.SetActive(true);
                            //     ConfirmObject.SetActive(false);
                            // }
                            // else
                            // {
                            //     ConfirmObject.SetActive(true);
                            // }
                            ConfirmObject.SetActive(true);

                            BlockerObject.SetActive(true);
                        }
                        TitleObject.SetActive(true);
                    }
                }
            }
            else
            {

                if (pds[0].gameObject.activeSelf == true)
                {
                    pds[0].time = pds[0].duration;
                    pds[0].gameObject.SetActive(false);
                    pds[1].gameObject.SetActive(true);
                    pds[1].Play();
                    SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_starfall_001);
                }
                else
                {
                    for (int i = 0; i < SkipParticleObjects.Length; i++)
                    {
                        SkipParticleObjects[i].SetActive(false);
                    }
                    isSkipGet = true;
                    if (CheckSkipCharacter() == false)
                    {
                        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_open_cha001);
                        foreach (var obj in GachItems)
                        {

                            obj.gameObject.SetActive(true);
                            obj.ChangeItem();
                            obj.ShowItem();
                            ChangeEffect(obj);
                            CloseObject.SetActive(true);
                            // if (dataManager.UserData.isFirstGacha)
                            // {
                            //     ReGachaObject.SetActive(true);
                            //     ConfirmObject.SetActive(false);
                            // }
                            // else
                            // {
                            //     ConfirmObject.SetActive(true);
                            // }
                            ConfirmObject.SetActive(true);

                            BlockerObject.SetActive(true);
                        }
                        TitleObject.SetActive(true);
                    }

                }
            }




            // for (int i = 0; i < SkipParticleObjects.Length; i++)
            //     SkipParticleObjects[i].SetActive(false);

        }

        public void OnClickBack()
        {
            if (isClick == true)
                return;
            // if (dataManager.UserData.isFirstGacha)
            // {
            //
            //     FirstGachaConfirmPopup.SetActive(true);
            //
            // }
            // else
            // {
            //     CloseProcess();
            // }

            TutorialManager.Instance?.HandleTutorialAction(TutorialTriggerType.CLOSE_POP_COMPLETE, nameof(GachaFxByTen));

            CloseProcess();
        }

        public void OnCloseReGachaConfirmPopup()
        {
            ReGachaConfirmPopup.SetActive(false);
        }

        public void OnCloseConfirmPopup()
        {
            FirstGachaConfirmPopup.SetActive(false);
        }
        public void OnClickFirstGacha()
        {
            // dataManager.UserData.isFirstGacha = false;
            // AppEventManager.Instance.SendProgress(5);
            // popupManager.Create<PopupKnightTicket3Day>(param: new BasePopupParam(), data: null, isDelay: true, isCloseAll: true);
            // dataManager.UserData.CurrentDialogueEventGroupNo = 17;
            // LobbyUIManager.Instance.SetKnights3Day();
            // ChangeRewardDataAndSave(_datas);
            // dataManager.SaveData();
            // CloseProcess();
        }

        private void ChangeRewardDataAndSave(List<CharacterInfo> result)
        {
            // List<RewardInfo> rewardDatas = new List<RewardInfo>();
            // for (int i = 0; i < result.Count; i++)
            // {
            //     RewardInfo rd = new RewardInfo(RewardType.KNIGHT_PIECE, result[i].id, result[i].piece);
            //     rewardDatas.Add(rd);
            // }
            //RewardManager.Instance.SaveRewards(rewardDatas, EventLocation.GACHA_CHRACTER, string.Empty);
        }
        private void CloseProcess()
        {
            SkipObject.SetActive(false);
            CloseObject.SetActive(false);
            // if (GameSceneManager.CurrentScene == Scene.Lobby)
            // {
            //     SoundManager.Instance.StopSFX(SFXIndex.gacha_result_ambient_001);
            //     SoundManager.Instance.StopBGM(1f);
            //     SoundManager.Instance.PlayBGM(BGMIndex.cash_shop_001);
            // }
            // else if (GameSceneManager.CurrentScene == Scene.InGame)
            // {
            //     PopupManager.Instance.DeleteAllPopup();
            //     GameSceneManager.MoveScene(Scene.Lobby);
            // }
            //
            //

            var gachaPopup = SceneUILayerManager.Instance.GetUILayer<GachaPopup>();
            if (gachaPopup != null)
            {
                gachaPopup.SetCanvasTargetDisplay(0);
            }

            SoundManager.Instance.StopSFX(SoundFX.snd_sfx_gacha_result_ambient_001);
            SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_lobby);
            SoundManager.Instance.IsPlayingGacha = false;

            Destroy(this.gameObject);
        }

        public void OnClickRegachaByPopup()
        {
            if(isClickRetry == true)
                return;
            isClickRetry = true;
            // CloseProcess();
            // popupManager.GetCurrentPopup().GetComponent<PopupGacha>().ProcessReGacha();
            // NetworkManager.Instance.GachaForKnight(110000,10, result =>
            // {
            //
            //     // for (int i = 0; i < result.result.Count; i++)
            //     // {
            //     //     Debug.LogColor(String.Format("Id = {0}, Count = {1}",result.result[i].id, result.result[i].piece));
            //     // }
            //     //
            //     // Debug.LogColor("CellingCount " + result.UserData.User.CellingCount);
            //     // dataManager.UserData.CellingCount = result.UserData.User.CellingCount;
            //     //todo 연출이 나오기 전까지 임시로 저장
            //     // SoundManager.Instance.StopSFX(SFXIndex.gacha_result_ambient_001);
            //     // SoundManager.Instance.StopBGM(1f);
            //     // Destroy(this.gameObject);
            //     if (result.result.Count == 10)
            //     {
            //         ReGachaConfirmPopup.SetActive(false);
            //         obj2.Dispose();
            //         obj1.Dispose();
            //         SetItem(result.result);
            //
            //         // AddressablesUtil.Instantiate("Gacha_VFX_Ver_Final_01").GetComponent<GachaFxByTen>().SetItem(result.result);
            //     }
            // }, () =>
            // {
            //
            // });
        }
        public void OnClickReGacha()
        {
            ReGachaConfirmPopup.SetActive(true);

        }
    }
}
