using System.Collections.Generic;
using CookApps.TeamBattle.UI;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Cover, "Prefabs/UI/Ready/ReadyMain.prefab")]
    public class ReadyMain : UILayer
    {
        [SerializeField] private TMP_Text stageNameText;
        [SerializeField] private Transform[] playerPositionParentTrs;
        [SerializeField] private Transform[] enemyPositionParentTrs;
        [SerializeField] private CharacterSlot characterSlotOrigin;
        [SerializeField] private TableView tableView;
        [SerializeField] private CAButton startButton;

        private ObjectPool<CharacterSlot> characterSlotPool;
        private int chapter;
        private int stageIndex;
        private List<int> ownCharacterIds = new ();

        private List<CharacterSlot> playerCharacterSlots = new ();
        private List<CharacterSlot> enemyCharacterSlots = new ();

        protected override void Awake()
        {
            base.Awake();
            tableView.OnGetTotalCellItemCount += OnGetTotalTableViewCellItemCount;
            tableView.OnGetCellItemSize += OnGetTableViewCellItemSize;
            tableView.OnReleaseCellItem += OnReleaseTableViewCellItem;
            tableView.OnGetCellItem += OnGetTableViewCellItem;

            characterSlotPool = new ObjectPool<CharacterSlot>(
                () =>
                {
                    GameObject go = Instantiate(characterSlotOrigin.CachedGo, tableView.content);
                    var slot = go.GetComponent<CharacterSlot>();
                    return slot;
                },
                slot =>
                {
                    slot.CachedGo.SetActive(true);
                    slot.OnClickSlot += OnClickCharacterSlot;
                },
                slot =>
                {
                    slot.CachedGo.SetActive(false);
                    slot.OnClickSlot -= OnClickCharacterSlot;
                },
                slot => Destroy(slot.CachedGo),
                false
            );

            characterSlotOrigin.CachedGo.SetActive(false);
            startButton.onClick.AddListener(OnClickStartButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            tableView.OnGetTotalCellItemCount -= OnGetTotalTableViewCellItemCount;
            tableView.OnGetCellItemSize -= OnGetTableViewCellItemSize;
            tableView.OnReleaseCellItem -= OnReleaseTableViewCellItem;
            tableView.OnGetCellItem -= OnGetTableViewCellItem;
            characterSlotPool.Dispose();
            startButton.onClick.RemoveListener(OnClickStartButton);
        }

        private GameObject OnGetTableViewCellItem(int idx)
        {
            CharacterSlot slot = characterSlotPool.Get();
            slot.CachedTr.SetParent(tableView.content, false);
            slot.CachedTr.localRotation = Quaternion.identity;
            slot.SetCharacterData(false, ownCharacterIds[idx]);
            return slot.CachedGo;
        }

        private void OnReleaseTableViewCellItem(int idx, GameObject obj)
        {
            characterSlotPool.Release(obj.GetComponent<CharacterSlot>());
        }

        private Vector2 OnGetTableViewCellItemSize(int idx)
        {
            return characterSlotOrigin.CachedRectTr.rect.size;
        }

        private int OnGetTotalTableViewCellItemCount()
        {
            return ownCharacterIds.Count;
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);
            (chapter, stageIndex) = ((int, int)) param;
            stageNameText.text = ZString.Format("{0}-{1}", chapter, stageIndex + 1);
            ownCharacterIds.Clear();
            ownCharacterIds.AddRange(UserDataManager.Instance.GetAllCharacterIds());
            tableView.RefreshAll();
            RefreshDeck();
            RefreshEnemyDeck();
        }

        protected override void OnPreExit()
        {
            base.OnPreExit();
            UserDataManager.Instance.SaveUserDeck();
        }

        private void RefreshDeck()
        {
            foreach (CharacterSlot slot in playerCharacterSlots)
            {
                characterSlotPool.Release(slot);
            }

            playerCharacterSlots.Clear();

            foreach (int id in UserDataManager.Instance.GetFront())
            {
                CharacterSlot slot = characterSlotPool.Get();
                slot.CachedTr.SetParent(playerPositionParentTrs[0], false);
                slot.CachedTr.localRotation = Quaternion.Euler(-30, 0, 0);
                slot.SetCharacterData(true, id);
                slot.CachedTr.SetAsLastSibling();
                playerCharacterSlots.Add(slot);
            }

            foreach (int id in UserDataManager.Instance.GetMid())
            {
                CharacterSlot slot = characterSlotPool.Get();
                slot.CachedTr.SetParent(playerPositionParentTrs[1], false);
                slot.CachedTr.localRotation = Quaternion.Euler(-30, 0, 0);
                slot.SetCharacterData(true, id);
                slot.CachedTr.SetAsLastSibling();
                playerCharacterSlots.Add(slot);
            }

            foreach (int id in UserDataManager.Instance.GetBack())
            {
                CharacterSlot slot = characterSlotPool.Get();
                slot.CachedTr.SetParent(playerPositionParentTrs[2], false);
                slot.CachedTr.localRotation = Quaternion.Euler(-30, 0, 0);
                slot.SetCharacterData(true, id);
                slot.CachedTr.SetAsLastSibling();
                playerCharacterSlots.Add(slot);
            }
        }

        private void RefreshEnemyDeck()
        {
            foreach (CharacterSlot slot in enemyCharacterSlots)
            {
                characterSlotPool.Release(slot);
            }

            enemyCharacterSlots.Clear();

            // SpecStage specStage = SpecDataManager.Instance.GetSpecStage(chapter, stageIndex);
            // foreach ((int id, int level) in specStage.GetFront())
            // {
            //     CharacterSlot slot = characterSlotPool.Get();
            //     slot.CachedTr.SetParent(enemyPositionParentTrs[0], false);
            //     slot.CachedTr.localRotation = Quaternion.Euler(-30, 0, 0);
            //     slot.SetEnemyCharacterData(id, level);
            //     slot.CachedTr.SetAsLastSibling();
            //     enemyCharacterSlots.Add(slot);
            // }
            //
            // foreach ((int id, int level) in specStage.GetMid())
            // {
            //     CharacterSlot slot = characterSlotPool.Get();
            //     slot.CachedTr.SetParent(enemyPositionParentTrs[1], false);
            //     slot.CachedTr.localRotation = Quaternion.Euler(-30, 0, 0);
            //     slot.SetEnemyCharacterData(id, level);
            //     slot.CachedTr.SetAsLastSibling();
            //     enemyCharacterSlots.Add(slot);
            // }
            //
            // foreach ((int id, int level) in specStage.GetBack())
            // {
            //     CharacterSlot slot = characterSlotPool.Get();
            //     slot.CachedTr.SetParent(enemyPositionParentTrs[2], false);
            //     slot.CachedTr.localRotation = Quaternion.Euler(-30, 0, 0);
            //     slot.SetEnemyCharacterData(id, level);
            //     slot.CachedTr.SetAsLastSibling();
            //     enemyCharacterSlots.Add(slot);
            // }
        }

        private void OnClickCharacterSlot(CharacterSlot slot)
        {
            TestSpecCharacter specCharacter = SpecDataManager.Instance.TestSpecCharacter.Get(slot.CharacterId);
            if (specCharacter.seq <= 0)
            {
                return;
            }

            if (UserDataManager.Instance.IsDeployed(specCharacter.id))
            {
                UserDataManager.Instance.RemoveCharacterInTeam(slot.CharacterId);
                RefreshDeck();
                tableView.RefreshAll();
            }
            else
            {
                UserDataManager.Instance.AddCharacterInTeam(slot.CharacterId);
                RefreshDeck();
                tableView.RefreshAll();
            }
        }

        private void OnClickStartButton()
        {
            SceneLoading.GoToNextScene("InGame", (chapter, stageIndex, DifficultyType.NORMAL)).Forget();
        }
    }
}
