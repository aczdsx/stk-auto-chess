using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 캐릭터 데이터 브릿지
    /// ServerDataManager와 UI 사이의 중간 레이어
    /// UI가 직접 데이터 모델을 접근하지 않고 브릿지를 통해 접근
    /// </summary>
    public class CharacterDataBridge : DataBridgeBase<CharacterModel>
    {
        // UI 갱신 이벤트
        public event Action OnCharactersChanged;
        public event Action<Tech.Hive.V1.CharacterInfo> OnCharacterUpdated;

        public CharacterDataBridge() : base(CharacterModel.CATEGORY_KEY)
        {
        }

        /// <summary>
        /// 모델 이벤트 구독
        /// </summary>
        protected override void SubscribeModelEvents()
        {
            Model.OnCharacterAdded += OnCharacterAdded;
            Model.OnCharacterUpdated += OnCharacterUpdatedInternal;
            Model.OnCharacterRemoved += OnCharacterRemoved;
        }

        /// <summary>
        /// 모델 이벤트 구독 해제
        /// </summary>
        protected override void UnsubscribeModelEvents()
        {
            Model.OnCharacterAdded -= OnCharacterAdded;
            Model.OnCharacterUpdated -= OnCharacterUpdatedInternal;
            Model.OnCharacterRemoved -= OnCharacterRemoved;
        }

        /// <summary>
        /// 데이터 변경 콜백
        /// </summary>
        protected override void OnDataChanged(DataChangeEvent changeEvent)
        {
            OnCharactersChanged?.Invoke();
        }

        private void OnCharacterAdded(Tech.Hive.V1.CharacterInfo character)
        {
            OnCharactersChanged?.Invoke();
        }

        private void OnCharacterUpdatedInternal(Tech.Hive.V1.CharacterInfo character)
        {
            OnCharacterUpdated?.Invoke(character);
        }

        private void OnCharacterRemoved(string instanceId)
        {
            OnCharactersChanged?.Invoke();
        }

        /// <summary>
        /// 모든 캐릭터 가져오기
        /// </summary>
        public void GetAllCharacters(List<Tech.Hive.V1.CharacterInfo> output)
        {
            Model?.GetAllCharacters(output);
        }

        /// <summary>
        /// 특정 캐릭터 가져오기
        /// </summary>
        public Tech.Hive.V1.CharacterInfo GetCharacter(string instanceId)
        {
            return Model?.GetCharacter(instanceId);
        }

        /// <summary>
        /// 캐릭터 개수
        /// </summary>
        public int CharacterCount => Model?.CharacterCount ?? 0;

        /// <summary>
        /// 캐릭터 존재 여부
        /// </summary>
        public bool HasCharacter(string instanceId)
        {
            return Model?.HasCharacter(instanceId) ?? false;
        }

        /// <summary>
        /// 조건에 맞는 캐릭터 필터링
        /// </summary>
        public void GetFilteredCharacters(List<Tech.Hive.V1.CharacterInfo> output, Func<Tech.Hive.V1.CharacterInfo, bool> filter)
        {
            Model?.GetCharactersByCondition(output, filter);
        }

        /// <summary>
        /// 레벨 범위로 필터링
        /// </summary>
        public void GetCharactersByLevelRange(List<Tech.Hive.V1.CharacterInfo> output, uint minLevel, uint maxLevel)
        {
            if (Model == null || output == null) return;

            output.Clear();
            var allCharacters = new List<Tech.Hive.V1.CharacterInfo>();
            Model.GetAllCharacters(allCharacters);

            // for문 사용 (Linq 지양)
            for (int i = 0; i < allCharacters.Count; i++)
            {
                var character = allCharacters[i];
                if (character.Level >= minLevel && character.Level <= maxLevel)
                {
                    output.Add(character);
                }
            }
        }

        /// <summary>
        /// 클래스 타입으로 필터링
        /// </summary>
        public void GetCharactersByClass(List<Tech.Hive.V1.CharacterInfo> output, ClassType classType)
        {
            if (Model == null || output == null) return;

            output.Clear();
            var allCharacters = new List<Tech.Hive.V1.CharacterInfo>();
            Model.GetAllCharacters(allCharacters);

            for (int i = 0; i < allCharacters.Count; i++)
            {
                var character = allCharacters[i];
                if (character.ClassType == classType)
                {
                    output.Add(character);
                }
            }
        }

        /// <summary>
        /// 레어리티로 필터링
        /// </summary>
        public void GetCharactersByRarity(List<Tech.Hive.V1.CharacterInfo> output, Rarity rarity)
        {
            if (Model == null || output == null) return;

            output.Clear();
            var allCharacters = new List<Tech.Hive.V1.CharacterInfo>();
            Model.GetAllCharacters(allCharacters);

            for (int i = 0; i < allCharacters.Count; i++)
            {
                var character = allCharacters[i];
                if (character.Rarity == rarity)
                {
                    output.Add(character);
                }
            }
        }
    }
}
