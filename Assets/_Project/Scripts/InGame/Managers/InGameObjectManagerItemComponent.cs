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
        abstract void OnItemTargetObjectRelease(CharacterController targetCharacter, InGameObjectManagerItemComponent.ItemState itemState);
    }
    public class InGameObjectManagerItemComponent
    {
        //Key: SpecPrefabID, Value: CharacterController List
        public enum ItemState
        {
            ITEM_NONE = 0,
            ITEM_DRAG_DROP = 1,
            ITEM_APPLIED = 2,
        }

        public class InGameObjectItemInfo
        {
            public ItemState itemState;
            public IEffectCodeSource source;
            public CharacterController targetObj;
            public Action<CharacterController, IEffectCodeSource> OnItemApplyDragAndDrop;// apply될 시 Callback
            public Func<CharacterController, bool> OnItemCanApplyDragAndDrop;// apply 가능한지 조건문 Callback
            public Func<CharacterController, bool> OnItemCheckCharacterAffected;// 캐릭터가 사라질때에 영향을 받는지 조건 체크 Callback
            public Action<CharacterController, ItemState> OnItemTargetObjectRelease;// 조건 성립 시 처리할 액션 Callback

            public static InGameObjectItemInfo Create(
                CharacterController character,
                Action<CharacterController, IEffectCodeSource> OnItemApplyDragAndDrop,
                IEffectCodeSource source,
                Func<CharacterController, bool> OnItemCanApplyDragAndDrop,
                Func<CharacterController, bool> OnItemCheckCharacterAffected = null,
                Action<CharacterController, ItemState> OnItemTargetObjectRelease = null)
            {
                return new InGameObjectItemInfo
                {
                    itemState = ItemState.ITEM_DRAG_DROP,
                    targetObj = character,
                    OnItemApplyDragAndDrop = OnItemApplyDragAndDrop,
                    source = source,    
                    OnItemCanApplyDragAndDrop = OnItemCanApplyDragAndDrop,
                    OnItemCheckCharacterAffected = OnItemCheckCharacterAffected,
                    OnItemTargetObjectRelease = OnItemTargetObjectRelease
                };
            }
        }

        //key :SpecCharacter.prefab_id, value : ItemInfo list
        private Dictionary<int, List<InGameObjectItemInfo>> _itemDic = new Dictionary<int, List<InGameObjectItemInfo>>();
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
            List<InGameObjectItemInfo> RemoveItemList = null;
            foreach (var item in _itemDic)
            {
                var itemList = item.Value;

                foreach (var itemInfo in itemList)
                {
                    if (itemInfo.OnItemCheckCharacterAffected?.Invoke(character) ?? false)
                    {
                        if (RemoveItemList == null)
                            RemoveItemList = new List<InGameObjectItemInfo>();
                        RemoveItemList.Add(itemInfo);
                    }
                }
            }
            
            foreach (var item in RemoveItemList)
            {
                item.OnItemTargetObjectRelease?.Invoke(character, item.itemState);
            }
        }

        public void RegisterItem(InGameObjectItemInfo itemInfo)
        {
            if (itemInfo.targetObj.SpecCharacter.character_type != CharacterType.ITEM)
                return;

            if (!_itemDic.TryGetValue(itemInfo.targetObj.SpecCharacter.prefab_id, out var itemList))
            {
                _itemDic[itemInfo.targetObj.SpecCharacter.prefab_id] = itemList = new List<InGameObjectItemInfo>();
            }
            itemList.Add(itemInfo);
        }

        public bool IsDragAndDropItem(CharacterController character)
        {
            if (character.SpecCharacter.character_type != CharacterType.ITEM
            || !_itemDic.TryGetValue(character.SpecCharacter.prefab_id, out var itemList))
                return false;

            foreach (var item in itemList)
            {
                if (item.itemState == ItemState.ITEM_DRAG_DROP && item.targetObj == character)
                    return true;
            }
            return false;
        }
        public bool IsRegisteredItem(int prefab_id)
        {
            return _itemDic.ContainsKey(prefab_id);
        }

        public bool ApplyItem(CharacterController itemObj, CharacterController targetObj)
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
            if (itemInfo.OnItemCanApplyDragAndDrop != null && !itemInfo.OnItemCanApplyDragAndDrop.Invoke(targetObj))
                return false;

            itemInfo.OnItemApplyDragAndDrop?.Invoke(targetObj, itemInfo.source);
            itemInfo.itemState = ItemState.ITEM_APPLIED;
            itemInfo.targetObj = targetObj;

            InGameObjectManager.Instance.RemoveCharacterFromField(itemObj);
            return true;
        }


        //해당 이펙트 코드가 remove되어야할 시점은
        //1. 아이템 착용된 캐릭터가 다시 들어갈때.
        //2. 해당 시너지가 제거될때(시너지 카운트가 2보다 떨어질때)

        public void TryRemoveItemFromTarget(int prefab_id)
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
                    if (itemList[i].targetObj.GetCharacterView() != null)
                    {
                        itemList[i].targetObj.GetEffectCodeContainer().RemoveEffectCodesAssociatedWithSource(itemList[i].source);
                    }
                    itemList.RemoveAt(i);
                    break;
                }
            }
            if (itemList.Count == 0)
            {
                _itemDic.Remove(prefab_id);
            }
        }
    }
}
