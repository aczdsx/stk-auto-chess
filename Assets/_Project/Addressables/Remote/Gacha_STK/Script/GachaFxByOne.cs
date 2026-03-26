using System.Collections.Generic;
using ClockStone;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoBattler
{
    public class GachaFxByOne : SingletonMonoBehaviour<GachaFxByOne>
    {
         [SerializeField] private GameObject[] SSRObjects;
        [SerializeField] private GameObject[] NormalObjects;
        [SerializeField] private List<GachaItem> GachItems;
        private List<RewardItem> _datas = null;
        [SerializeField] private PlayableDirector pd;
        private bool isClick = false;
        private bool isSkip = false;
        [SerializeField] private GameObject SkipObject;
        [SerializeField] private GameObject CloseObject;
        [SerializeField] private GameObject BlockerObject;
        [SerializeField] private GameObject[] SkipParticleObjects;

        public void SetItem(List<RewardItem> datas)
        {
            if (datas == null)
            {
                Destroy(this);
                return;
            }
            isClick = true;
            isSkip = false;
            SkipObject.SetActive(true);
            CloseObject.SetActive(false);
            BlockerObject.SetActive(true);
            _datas = datas;
            for (int i = 0; i < SSRObjects.Length; i++)
            {
                SSRObjects[i].SetActive(false);
            }
            for (int i = 0; i < NormalObjects.Length; i++)
            {
                NormalObjects[i].SetActive(false);
            }

            bool isIncludeSSR = false;

            // SSR 캐릭터 포함 여부 확인
            foreach (var data in datas)
            {
                if (data.Id.GetCharacterId(out int charId))
                {
                    var charInfo = SpecDataManager.Instance.GetCharacterData(charId);
                    if (charInfo != null && charInfo.grade_type == GradeType.SSR && data.Id.IsCharacter())
                    {
                        isIncludeSSR = true;
                        break;
                    }
                }
            }

            if (isIncludeSSR == true)
            {
                for (int i = 0; i < SSRObjects.Length; i++)
                {
                    SSRObjects[i].SetActive(true);
                }
            }
            else
            {
                for (int i = 0; i < NormalObjects.Length; i++)
                {
                    NormalObjects[i].SetActive(true);
                }
            }

            for (int i = 0; i < datas.Count; i++)
            {
                GachItems[i].InitItem(datas[i]);
            }

            //SoundManager.Instance.StopBGM();
            //SoundManager.Instance.PlaySFX(SFXIndex.gacha_start_001);

            Run.After(System.TimeSpan.FromSeconds(5).Seconds, () =>
            {
                if (isSkip == false)
                {
                    foreach (var obj in GachItems)
                    {
                        obj.gameObject.SetActive(true);
                    }
                }
            });
        }

        public void ChangeImageCard(int idx)
        {
            GachItems[idx].ChangeItem();
            if (idx == 0)
            {
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_result_ambient_001);
            }
            if (idx == 0)
            {
                Run.After(System.TimeSpan.FromSeconds(1f).Seconds, () =>
                {
                    SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_gacha_open_cha001);
                    // foreach (var obj in GachItems)
                    // {
                    //     // obj.ShowItem();
                    //     ChangeEffect(obj);
                    // }
                    ShowGetFX();
                });
            }
        }
        private GameObject fx = null;
        private AsyncOperationHandle<GameObject> _fxHandle;
        private int cnt = 0;
        private void ShowGetFX()
        {
            if (isSkip == true)
            {
                return;
            }

            ReleaseFx();
            if (cnt > _datas.Count - 1)
            {
                foreach (var obj in GachItems)
                {
                    ChangeEffect(obj);
                }
                isClick = false;
                SkipObject.SetActive(false);
                CloseObject.SetActive(true);
                return;
            }
            BlockerObject.SetActive(false);

            if (!_datas[cnt].Id.GetCharacterId(out int characterId))
            {
                cnt++;
                ShowGetFX();
                return;
            }

            CharacterInfo idxCharcater = SpecDataManager.Instance.GetCharacterData(characterId);
            _fxHandle = Addressables.InstantiateAsync("GetNewCharacter");
            fx = _fxHandle.WaitForCompletion();
            if (_datas[cnt].Id.IsCharacter())
            {
                fx.GetComponent<GetNewCharacter>().SetChracater(idxCharcater, ShowGetFX);
            }
            else
            {
                fx.GetComponent<GetNewCharacter>().SetPiece(idxCharcater, _datas[cnt].Count, ShowGetFX);
            }

            cnt++;
        }

        private void ChangeEffect(GachaItem gachaItem)
        {

            //gachaItem.GetComponent<CanvasGroup>().DOFade(0, 0.2f);
            gachaItem.GetComponent<CanvasGroup>().alpha = 0;
            Run.After(System.TimeSpan.FromSeconds(0.2f).Seconds, () =>
            {
                gachaItem.ShowItem();
                //gachaItem.GetComponent<CanvasGroup>().DOFade(1, 0.2f);
                gachaItem.GetComponent<CanvasGroup>().alpha = 1;
            });
        }
        public void OnClickSkip()
        {
            pd.time = pd.duration;
            isClick = false;
            SkipObject.SetActive(false);
            CloseObject.SetActive(true);
            BlockerObject.SetActive(true);
            //SoundManager.Instance.StopSFX(SFXIndex.gacha_start_001);
            ReleaseFx();

            for(int i = 0; i < SkipParticleObjects.Length; i++)
                SkipParticleObjects[i].SetActive(false);
            foreach (var obj in GachItems)
            {
                obj.gameObject.SetActive(true);
                obj.ChangeItem();
                obj.ShowItem();
                ChangeEffect(obj);
            }
        }
        private void ReleaseFx()
        {
            if (_fxHandle.IsValid())
            {
                Addressables.ReleaseInstance(_fxHandle);
                _fxHandle = default;
            }
            fx = null;
        }

        public void OnClickBack()
        {
            if(isClick == true)
                return;
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

            var gachaPopup = SceneUILayerManager.Instance.GetUILayer<GachaPopup>();
            if (gachaPopup != null)
            {
                gachaPopup.SetCanvasTargetDisplay(0);
            }

            SoundManager.Instance.StopSFX(SoundFX.snd_sfx_gacha_result_ambient_001);
            SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_command01);
            SoundManager.Instance.IsPlayingGacha = false;

            Destroy(this.gameObject);
        }

        private void OnDestroy()
        {
            ReleaseFx();
        }
    }

}
