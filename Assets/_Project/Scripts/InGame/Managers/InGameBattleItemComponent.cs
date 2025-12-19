using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Converters;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace CookApps.BattleSystem
{
    public interface IEffectCodeInGameObjectItemInfo
    {
        abstract void OnItemApplyDragAndDrop(CharacterController targetCharacter, IEffectCodeSource source);
        abstract bool OnItemCanApplyDragAndDrop(CharacterController targetCharacter);
        abstract bool OnItemCheckCharacterAffected(CharacterController targetCharacter);
        abstract void OnItemTargetObjectRelease(CharacterController targetCharacter, InGameBattleItemComponent.ItemState itemState);
        /// <summary>
        /// 전투 시작 전까지 아이템이 부여되지 않았을 때 호출됩니다.
        /// </summary>
        /// <param name="source">이펙트 코드 소스</param>
        abstract void OnItemNotAppliedBeforeCombat(CharacterController targetItemController, IEffectCodeSource source);
    }

    public class InGameBattleItemComponent
    {
        public enum ItemState
        {
            ITEM_NONE = 0,
            ITEM_DRAG_DROP = 1,
            ITEM_APPLIED = 2,
        }

        /// <summary>
        /// 배틀 아이템 정보를 담는 클래스
        /// </summary>
        public class InGameBattleItemInfo
        {
            public ItemState itemState;
            public IEffectCodeSource source;
            public CharacterController targetObj;
            private IEffectCodeInGameObjectItemInfo _itemInfoHandler;

            // 콜백들을 래핑하여 사용
            public Action<CharacterController, IEffectCodeSource> OnItemApplyDragAndDrop =>
                (target, src) => _itemInfoHandler?.OnItemApplyDragAndDrop(target, src);
            public Func<CharacterController, bool> OnItemCanApplyDragAndDrop =>
                target => _itemInfoHandler?.OnItemCanApplyDragAndDrop(target) ?? false;
            public Func<CharacterController, bool> OnItemCheckCharacterAffected =>
                target => _itemInfoHandler?.OnItemCheckCharacterAffected(target) ?? false;
            public Action<CharacterController, ItemState> OnItemTargetObjectRelease =>
                (target, state) => _itemInfoHandler?.OnItemTargetObjectRelease(target, state);
            public Action<CharacterController, IEffectCodeSource> OnItemNotAppliedBeforeCombat =>
                (target, src) => _itemInfoHandler?.OnItemNotAppliedBeforeCombat(target, src);

            private InGameBattleItemInfo()
            {
            }

            /// <summary>
            /// IEffectCodeInGameObjectItemInfo 인터페이스를 구현한 객체로부터 아이템 정보를 생성합니다.
            /// </summary>
            public static InGameBattleItemInfo Create(
                CharacterController character,
                IEffectCodeSource source,
                IEffectCodeInGameObjectItemInfo itemInfoHandler)
            {
                return new InGameBattleItemInfo
                {
                    itemState = ItemState.ITEM_DRAG_DROP,
                    targetObj = character,
                    source = source,
                    _itemInfoHandler = itemInfoHandler
                };
            }

            /// <summary>
            /// 콜백들을 래핑하는 내부 클래스
            /// </summary>
            private class CallbackWrapper : IEffectCodeInGameObjectItemInfo
            {
                private readonly Action<CharacterController, IEffectCodeSource> _onItemApplyDragAndDrop;
                private readonly Func<CharacterController, bool> _onItemCanApplyDragAndDrop;
                private readonly Func<CharacterController, bool> _onItemCheckCharacterAffected;
                private readonly Action<CharacterController, ItemState> _onItemTargetObjectRelease;
                private readonly Action<CharacterController, IEffectCodeSource> _onItemNotAppliedBeforeCombat;

                public CallbackWrapper(
                    Action<CharacterController, IEffectCodeSource> onItemApplyDragAndDrop,
                    Func<CharacterController, bool> onItemCanApplyDragAndDrop,
                    Func<CharacterController, bool> onItemCheckCharacterAffected,
                    Action<CharacterController, ItemState> onItemTargetObjectRelease,
                    Action<CharacterController, IEffectCodeSource> onItemNotAppliedBeforeCombat)
                {
                    _onItemApplyDragAndDrop = onItemApplyDragAndDrop;
                    _onItemCanApplyDragAndDrop = onItemCanApplyDragAndDrop;
                    _onItemCheckCharacterAffected = onItemCheckCharacterAffected;
                    _onItemTargetObjectRelease = onItemTargetObjectRelease;
                    _onItemNotAppliedBeforeCombat = onItemNotAppliedBeforeCombat;
                }

                public void OnItemApplyDragAndDrop(CharacterController targetCharacter, IEffectCodeSource source)
                {
                    _onItemApplyDragAndDrop?.Invoke(targetCharacter, source);
                }

                public bool OnItemCanApplyDragAndDrop(CharacterController targetCharacter)
                {
                    return _onItemCanApplyDragAndDrop?.Invoke(targetCharacter) ?? false;
                }

                public bool OnItemCheckCharacterAffected(CharacterController targetCharacter)
                {
                    return _onItemCheckCharacterAffected?.Invoke(targetCharacter) ?? false;
                }

                public void OnItemTargetObjectRelease(CharacterController targetCharacter, ItemState itemState)
                {
                    _onItemTargetObjectRelease?.Invoke(targetCharacter, itemState);
                }

                public void OnItemNotAppliedBeforeCombat(CharacterController targetItemController, IEffectCodeSource source)
                {
                    _onItemNotAppliedBeforeCombat?.Invoke(targetItemController, source);
                }
            }
        }

        //key :SpecCharacter.prefab_id, value : ItemInfo list
        private Dictionary<int, List<InGameBattleItemInfo>> _itemDic = new Dictionary<int, List<InGameBattleItemInfo>>();
        public void Initialize()
        {
            _itemDic.Clear();
        }

        public void Clear()
        {
            foreach (var item in _itemDic)
            {
                item.Value.Clear();
            }
            _itemDic.Clear();
        }

        public void CheckAffectedByItemController(CharacterController character)
        {
            List<InGameBattleItemInfo> RemoveItemList = null;
            foreach (var item in _itemDic)
            {
                var itemList = item.Value;

                foreach (var itemInfo in itemList)
                {
                    if (itemInfo.OnItemCheckCharacterAffected?.Invoke(character) ?? false)
                    {
                        if (RemoveItemList == null)
                            RemoveItemList = new List<InGameBattleItemInfo>();
                        RemoveItemList.Add(itemInfo);
                    }
                }
            }

            if (RemoveItemList==  null)
                return;

            foreach (var item in RemoveItemList)
            {
                item.OnItemTargetObjectRelease?.Invoke(character, item.itemState);
            }
        }

        public void RegisterBattleItem(InGameBattleItemInfo itemInfo)
        {
            if (itemInfo.targetObj.SpecCharacter.character_type != CharacterType.BATTLEITEM)
                return;

            if (!_itemDic.TryGetValue(itemInfo.targetObj.SpecCharacter.prefab_id, out var itemList))
            {
                _itemDic[itemInfo.targetObj.SpecCharacter.prefab_id] = itemList = new List<InGameBattleItemInfo>();
            }
            itemList.Add(itemInfo);
        }

        public bool IsDragAndDropBattleItem(CharacterController character)
        {
            if (character.SpecCharacter.character_type != CharacterType.BATTLEITEM
            || !_itemDic.TryGetValue(character.SpecCharacter.prefab_id, out var itemList))
                return false;

            foreach (var item in itemList)
            {
                if (item.itemState == ItemState.ITEM_DRAG_DROP && item.targetObj == character)
                    return true;
            }
            return false;
        }
        public bool IsRegisteredBattleItem(int prefab_id)
        {
            return _itemDic.ContainsKey(prefab_id);
        }

        public bool ApplyBattleItem(CharacterController itemObj, CharacterController targetObj)
        {
            if (!_itemDic.TryGetValue(itemObj.SpecCharacter.prefab_id, out var itemList))
                return false;

            int indexToMove = -1;
            for (int i = 0; i < itemList.Count; i++)
            {
                if (itemList[i].itemState == ItemState.ITEM_DRAG_DROP && itemList[i].targetObj == itemObj)
                {
                    indexToMove = i;
                    break;
                }
            }

            if (indexToMove == -1)
                return false;

            var itemInfo = itemList[indexToMove];
            if (!itemInfo.OnItemCanApplyDragAndDrop(targetObj))
                return false;

            itemInfo.OnItemApplyDragAndDrop(targetObj, itemInfo.source);
            itemInfo.itemState = ItemState.ITEM_APPLIED;
            itemInfo.targetObj = targetObj;

            InGameObjectManager.Instance.RemoveCharacterFromField(itemObj);
            return true;
        }

        public void TryRemoveBattleItemFromTarget(int prefab_id)
        {
            if (!_itemDic.TryGetValue(prefab_id, out var itemList))
                return;

            for (int i = 0; i < itemList.Count; i++)
            {
                if (itemList[i].itemState == ItemState.ITEM_DRAG_DROP)
                {
                    InGameObjectManager.Instance.RemoveCharacterFromField(itemList[i].targetObj);
                    itemList.RemoveAt(i);
                    break;
                }
                else if (itemList[i].itemState == ItemState.ITEM_APPLIED)
                {
                    // if (itemList[i].targetObj.GetCharacterView() != null)
                    // {
                    //     itemList[i].targetObj.GetEffectCodeContainer().RemoveEffectCodesAssociatedWithSource(itemList[i].source);
                    // }
                    itemList.RemoveAt(i);
                    break;
                }
            }
            if (itemList.Count == 0)
            {
                _itemDic.Remove(prefab_id);
            }
        }

        /// <summary>
        /// 전투 시작 시 아이템이 적용되지 않은 상태인 아이템들의 콜백을 호출합니다.
        /// </summary>
        public void CheckAndHandleNotAppliedItemsBeforeCombat()
        {
            foreach (var kvp in _itemDic)
            {
                var itemList = kvp.Value;
                foreach (var itemInfo in itemList)
                {
                    // 아이템이 아직 적용되지 않은 상태(ITEM_DRAG_DROP)인 경우
                    if (itemInfo.itemState == ItemState.ITEM_DRAG_DROP)
                    {
                        itemInfo.OnItemNotAppliedBeforeCombat?.Invoke(itemInfo.targetObj, itemInfo.source);
                    }
                }
            }
        }
    }
}
