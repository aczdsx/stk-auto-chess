using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class GachaEffect : SingletonMonoBehaviour<GachaEffect>
    {
        [Header("임시 코드 Count")] [SerializeField] private List<int> TestIDCount;
        [Header("임시 코드 ID")] [SerializeField] private List<int> TestCountString;



        [Header("메인")]
        // Start is called before the first frame update
        //back


        [SerializeField] private List<GachaItem> GachItems;


        private bool isClick = false;

        private string gachaAtlasString = "Atlas_Gacha";
        private List<SpecCharacter> resultGacha;
        public void InitGacha(List<SpecCharacter> result)
        {
            isClick = true;
            cnt = 0;
            resultGacha = result;
            foreach (var obj in GachItems)
            {
                obj.gameObject.SetActive(false);
            }

            for (int i = 0; i < result.Count; i++)
            {
                SpecCharacter idxCharcater = SpecDataManager.Instance.GetCharacterData( result[i].character_id);
                // GachItems[i].InitItem(idxCharcater, result[i].piece, i);
            }

            //SoundManager.Instance.StopBGM();
            //SoundManager.Instance.PlaySFX(SFXIndex.gacha_start_001);

            Run.After(System.TimeSpan.FromSeconds(5).Seconds, () =>
            {
                foreach (var obj in GachItems)
                {
                    obj.gameObject.SetActive(true);
                }
            });
        }


        public void ChangeImageCard(int idx)
        {
            GachItems[idx].ChangeItem();
            if (idx == 0)
            {
                //SoundManager.Instance.PlaySFX(SFXIndex.gacha_result_ambient_001);
            }
            if (idx == 9)
            {
                Run.After(System.TimeSpan.FromSeconds(1f).Seconds, () =>
                {
                    ShowGetFX();
                });
            }
        }

        private int cnt = 0;
        private void ShowGetFX()
        {
            if (cnt > resultGacha.Count - 1)
            {
                foreach (var obj in GachItems)
                {
                        // obj.ShowItem();
                    ChangeEffect(obj);
                }
                isClick = false;
                return;
            }
            if (resultGacha[cnt].need_piece == 20)
            {
                Addressables.InstantiateAsync("GetNewCharacter").WaitForCompletion().GetComponent<GetNewCharacter>().SetChracater(resultGacha[cnt], ShowGetFX);
            }
            else
            {
                Addressables.InstantiateAsync("GetNewCharacter").WaitForCompletion().GetComponent<GetNewCharacter>().SetPiece(resultGacha[cnt],resultGacha[cnt].need_piece, ShowGetFX);
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

        public void OnClickBack()
        {
            if(isClick == true)
                return;
            // if (DialogueEventManager.Instance.CheckCondition(DialogueEventCondition.GACHA_RESULT))
            // {
            //     PopupManager.Instance.Create<PopupSurvey>().Open(false);
            //     PopupManager.Instance.DeleteAllPopup();
            //     AppEventManager.Instance.SendInitial_Launch_Funnel(120210);
            // }
            //
            //
            // SoundManager.Instance.StopSFX(SFXIndex.gacha_result_ambient_001);
            // LobbyScene.Instance.ShowLobby();
            // LobbyUIManager.Instance.PlayCharacterBGM();

            Destroy(this.gameObject);
        }

    }
}

