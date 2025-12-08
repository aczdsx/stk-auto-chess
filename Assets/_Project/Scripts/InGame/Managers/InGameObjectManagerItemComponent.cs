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
    public class InGameObjectManagerItemComponent
    {
        //Key: SpecPrefabID, Value: CharacterController List
        private enum ItemState
        {
            ITEM_NONE = 0,
            ITEM_DRAG_DROP = 1,
            ITEM_APPLIED = 2,
        }

        private struct ItemInfo
        {
            public ItemState itemState;
            public IEffectCodeSource source;
            public CharacterController targetObj;
            public Action<CharacterController, IEffectCodeSource> onApply;
            public Func<CharacterController, bool> onCanApply;
        }
        
        //key :SpecCharacter.prefab_id, value : ItemInfo list
        private Dictionary<int, List<ItemInfo>> _itemDic = new Dictionary<int, List<ItemInfo>>();
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
            List<SynergyType> needsToRemove = null;
            
            foreach (var item in _itemDic)
            {
                var itemList = item.Value;

                foreach (var itemInfo in itemList)
                {
                    if (itemInfo.itemState == ItemState.ITEM_APPLIED && itemInfo.targetObj == character)
                    {
                        if(needsToRemove == null)
                        {
                            needsToRemove = new List<SynergyType>();
                        }
                        needsToRemove.Add(itemInfo.targetObj.SpecCharacter.element_type);
                        needsToRemove.Add(itemInfo.targetObj.SpecCharacter.asterism_type);
                    }
                }
            }

            if(needsToRemove == null)
                return;

            // 반복문 밖에서 시너지 제거
            foreach (var targetSynergyType in needsToRemove)
            {
                InGameManager.Instance.RemoveSynergyTeamOnce(AllianceType.Player, targetSynergyType);
            }
        }

        public void RegisterItemGameObjectDragAndDrop(CharacterController character, Action<CharacterController, IEffectCodeSource> onApply, IEffectCodeSource source,
        Func<CharacterController, bool> onCanApply)
        {
            if (character.SpecCharacter.character_type != CharacterType.ITEM)
                return;
            if (!_itemDic.TryGetValue(character.SpecCharacter.prefab_id, out var itemList))
            {
                _itemDic[character.SpecCharacter.prefab_id] = itemList = new List<ItemInfo>();
            }
            itemList.Add(new ItemInfo
            {
                itemState = ItemState.ITEM_DRAG_DROP,
                targetObj = character,
                onApply = onApply,
                source = source,
                onCanApply = onCanApply
            });
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
            if (itemInfo.onCanApply != null && !itemInfo.onCanApply.Invoke(targetObj))
                return false;

            itemInfo.onApply?.Invoke(targetObj, itemInfo.source);
            itemInfo.itemState = ItemState.ITEM_APPLIED;
            itemInfo.targetObj = targetObj;
            itemList[indexToMove] = itemInfo; // 구조체이므로 수정된 값을 다시 할당해야 함
            
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
            if(itemList.Count == 0)
            {
                _itemDic.Remove(prefab_id);
            }
        }
    }
}
