using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class SceneRegress : MonoBehaviour
{

    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage rawImage;
    // Start is called before the first frame update


    private int cnt = 0;
    private bool isSkip = false;
    private bool isOver = false;
    IEnumerator Start()
    {
        isSkip = false;
        isOver = false;

        cnt = 0;
        // AppEventManager.Instance.SendInitial_Launch_Funnel(10300);
        rawImage.color = new Color(0, 0, 0, 1);
        yield return new WaitForSeconds(2);
        videoPlayer.loopPointReached += CheckOver;
        videoPlayer.Play();
        rawImage.color = new Color(1, 1, 1, 0);
        rawImage.DOFade(1, 3).SetEase(Ease.Linear);
    }


    void CheckOver(UnityEngine.Video.VideoPlayer vp)
    {
        print("Video Is Over");
        isOver = true;
        if (!isSkip)
        {
            videoPlayer.Pause();
            // popupManager.Create<PopupSkipMessage>().Open(CloseAction);
        }
    }

    public void OnClickSkip()
    {
        if (isOver)
            return;

        videoPlayer.Pause();
        isSkip = true;
        // popupManager.Create<PopupMessage>().Open(Localization.GetLocalizedString("POPUP_COMMON_TITLE"),
        //     Localization.GetLocalizedString("MSG_MOVIE_SKIP_POPUP"), Localization.GetLocalizedString("INTRO_MOVIE_SKIP_BTN_OK"),
        //     CallbackConfirm,
        //     Localization.GetLocalizedString("INTRO_MOVIE_SKIP_BTN_CANCEL")
        //     , () =>
        //     {
        //         if (isOver)
        //         {
        //             popupManager.Close();   
        //             popupManager.Create<PopupSkipMessage>().Open(CloseAction);
        //         }
        //         else
        //         {
        //             isSkip = false;
        //             videoPlayer.Play();
        //             popupManager.Close();    
        //         }

        //     }, null, false);
    }

    private void CloseAction(bool isSkip)
    {
        if (isSkip)
        {
            // rawImage.DOFade(0, 0.1f).SetEase(Ease.Linear).OnComplete(() =>
            // {
            //     List<RewardData> datas = new List<RewardData>();
            //     for (int i = 0; i < SpecDataManager.Instance.TutorialSkipRewardList.Count; i++)
            //     {
            //         if (SpecDataManager.Instance.TutorialSkipRewardList[i].category_key == "tutorial_skip_get_reward")
            //         {
            //             RewardData data = new RewardData(SpecDataManager.Instance.TutorialSkipRewardList[i].reward_type,
            //                 SpecDataManager.Instance.TutorialSkipRewardList[i].reward_key,
            //                 SpecDataManager.Instance.TutorialSkipRewardList[i].reward_count);
            //             datas.Add(data);
            //         }
            //         else if (SpecDataManager.Instance.TutorialSkipRewardList[i].category_key == "tutorial_skip_outpost_base_fastreward_free_count")
            //         {
            //             DataManager.Instance.UserTimeData.accWallC = 1;
            //         }
            //         else if (SpecDataManager.Instance.TutorialSkipRewardList[i].category_key == "tutorial_skip_change_character_lv")
            //         {
            //             CharacterData cha = DataManager.Instance.Characters.Find(x =>
            //                 x.ID == SpecDataManager.Instance.TutorialSkipRewardList[i].target_id);
            //             if (cha != null)
            //             {
            //                 cha.Level = SpecDataManager.Instance.TutorialSkipRewardList[i].value;
            //             }
            //         }
            //     }
            //     RewardManager.Instance.SaveRewards(datas, EventLocation.MAIL_BOX, "");
            //     DataManager.Instance.UserData.LastStageID = 5;
            //     DataManager.Instance.UserData.BestStageID = 5;
            //     DataManager.Instance.UserData.NickName = NetworkManager.Instance.UID.ToString();
            //     DataManager.Instance.UserData.CurrentDialogueEventGroupNo = 12;

            //     var buildingData = DataManager.Instance.Buildings.Find(x => x.GetBuildingID == BuildingID.CASTLE_WALL);

            //     if (buildingData == null)
            //     {
            //         BuildingMetaData newBuildingMetaData =
            //             SpecDataManager.Instance.BuildingList.Find(x => x.building_id == BuildingID.CASTLE_WALL);
            //         buildingData = new BuildingData(newBuildingMetaData);
            //         DataManager.Instance.AddBuilding(buildingData);
            //     }

            //     DataManager.Instance.SaveData();

            //     SoundManager.Instance.StopBGM();
            //     AppEventManager.Instance.SendProgress(2);
            //     AppEventManager.Instance.SendTutorialSkip(true);
            //     QuestManager.Instance.ReportQuest(QuestID.GEOFRONT_SWEEP);
            //     GameSceneManager.MoveScene(Scene.Lobby);
            // });
        }
        else
        {
            // rawImage.DOFade(0, 0.1f).SetEase(Ease.Linear).OnComplete(() =>
            // {
            //     DataManager.TestDialogueScriptName = "0-1";
            //     GameSceneManager.MoveScene(Scene.Dialogue, true, 500);
            //     AppEventManager.Instance.SendTutorialSkip(false);
            // });
        }
    }
    private void CallbackConfirm()
    {
        // popupManager.DeleteAllPopup();
        // popupManager.Create<PopupSkipMessage>().Open(CloseAction);
    }
}
