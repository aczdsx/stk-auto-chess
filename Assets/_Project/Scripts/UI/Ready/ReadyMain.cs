using System.Collections.Generic;
using CookApps.TeamBattle.UI;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.Pool;

namespace CookApps.SampleTeamBattle
{
    public class ReadyMain : UILayer
    {
        [SerializeField] private Transform[] playerPositionParentTrs;
        [SerializeField] private Transform[] enemyPositionParentTrs;
        [SerializeField] private CharacterSlot characterSlotOrigin;
        [SerializeField] private TableView tableView;

        private ObjectPool<CharacterSlot> characterSlotPool;
        private int chapter;
        private int stageIndex;

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
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            tableView.OnGetTotalCellItemCount -= OnGetTotalTableViewCellItemCount;
            tableView.OnGetCellItemSize -= OnGetTableViewCellItemSize;
            tableView.OnReleaseCellItem -= OnReleaseTableViewCellItem;
            tableView.OnGetCellItem -= OnGetTableViewCellItem;
            characterSlotPool.Dispose();
        }

        private RectTransform OnGetTableViewCellItem(int idx)
        {
            CharacterSlot slot = characterSlotPool.Get();
            slot.CachedTr.SetParent(tableView.content, false);
            slot.CachedTr.localRotation = Quaternion.identity;
            slot.SetCharacterData(false, idx);
            return slot.CachedRectTr;
        }

        private void OnReleaseTableViewCellItem(int idx, Transform obj)
        {
            characterSlotPool.Release(obj.GetComponent<CharacterSlot>());
        }

        private Vector2 OnGetTableViewCellItemSize(int idx)
        {
            return characterSlotOrigin.CachedRectTr.rect.size;
        }

        private int OnGetTotalTableViewCellItemCount()
        {
            return UserDataManager.UserCharacter.GetCharacterCount();
        }

        public override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.Bread, TopPanelType.CloseButton);
            (chapter, stageIndex) = ((int, int)) param;
            tableView.RefreshAll();
            RefreshDeck();
            RefreshEnemyDeck();
        }

        private void RefreshDeck()
        {
            foreach (CharacterSlot slot in playerCharacterSlots)
            {
                characterSlotPool.Release(slot);
            }

            foreach (int id in UserDataManager.UserDeck.GetFront())
            {
                CharacterSlot slot = characterSlotPool.Get();
                slot.CachedTr.SetParent(enemyPositionParentTrs[0], false);
                slot.CachedTr.localRotation = Quaternion.Euler(-30, 0, 0);
                slot.SetCharacterData(true, id);
                enemyCharacterSlots.Add(slot);
            }

            foreach (int id in UserDataManager.UserDeck.GetMid())
            {
                CharacterSlot slot = characterSlotPool.Get();
                slot.CachedTr.SetParent(enemyPositionParentTrs[1], false);
                slot.CachedTr.localRotation = Quaternion.Euler(-30, 0, 0);
                slot.SetCharacterData(true, id);
                enemyCharacterSlots.Add(slot);
            }

            foreach (int id in UserDataManager.UserDeck.GetBack())
            {
                CharacterSlot slot = characterSlotPool.Get();
                slot.CachedTr.SetParent(enemyPositionParentTrs[2], false);
                slot.CachedTr.localRotation = Quaternion.Euler(-30, 0, 0);
                slot.SetCharacterData(true, id);
                enemyCharacterSlots.Add(slot);
            }
        }

        private void RefreshEnemyDeck()
        {
            foreach (CharacterSlot slot in enemyCharacterSlots)
            {
                characterSlotPool.Release(slot);
            }

            SpecStage specStage = SpecDataManager.Instance.GetSpecStage(chapter, stageIndex);
            foreach (int id in specStage.GetFront())
            {
                CharacterSlot slot = characterSlotPool.Get();
                slot.CachedTr.SetParent(enemyPositionParentTrs[0], false);
                slot.CachedTr.localRotation = Quaternion.Euler(-30, 0, 0);
                slot.SetCharacterData(true, id);
                enemyCharacterSlots.Add(slot);
            }

            foreach (int id in specStage.GetMid())
            {
                CharacterSlot slot = characterSlotPool.Get();
                slot.CachedTr.SetParent(enemyPositionParentTrs[1], false);
                slot.CachedTr.localRotation = Quaternion.Euler(-30, 0, 0);
                slot.SetCharacterData(true, id);
                enemyCharacterSlots.Add(slot);
            }

            foreach (int id in specStage.GetBack())
            {
                CharacterSlot slot = characterSlotPool.Get();
                slot.CachedTr.SetParent(enemyPositionParentTrs[2], false);
                slot.CachedTr.localRotation = Quaternion.Euler(-30, 0, 0);
                slot.SetCharacterData(true, id);
                enemyCharacterSlots.Add(slot);
            }
        }

        private void OnClickCharacterSlot(CharacterSlot slot)
        {
            SpecCharacter specCharacter = SpecDataManager.Instance.SpecCharacter.Get(slot.CharacterId);
            if (specCharacter.seq <= 0)
            {
                return;
            }

            if (slot.IsInDeck)
            {
                UserDataManager.UserDeck.RemoveCharacterInTeam(slot.CharacterId);
                RefreshDeck();
            }
            else
            {
                UserDataManager.UserDeck.AddCharacterInTeam(slot.CharacterId);
                RefreshDeck();
            }
        }
    }
}
