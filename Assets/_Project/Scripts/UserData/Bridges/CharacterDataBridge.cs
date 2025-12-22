using System;
using System.Collections.Generic;
using R3;
using Tech.Hive.V1;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 캐릭터 데이터 브릿지
    /// ServerDataManager와 UI 사이의 중간 레이어
    /// UI가 직접 데이터 모델을 접근하지 않고 브릿지를 통해 접근
    /// </summary>
    public class CharacterDataBridge : DataBridgeBase<CharacterModel>
    {
        // R3 이벤트
        public readonly Subject<Unit> OnCharactersChanged = new();
        public readonly Subject<CharacterData> OnCharacterUpdated = new();

        public CharacterDataBridge()
            : base(ServerDataManager.Instance.Character, CharacterModel.CATEGORY_KEY)
        {
        }

        /// <summary>
        /// 모델 이벤트 구독
        /// </summary>
        protected override void SubscribeModelEvents()
        {
            Model.OnCharacterAdded.Subscribe(this, (character, self) => self.OnCharactersChanged.OnNext(Unit.Default)).AddTo(ref disposableBag);
            Model.OnCharacterUpdated.Subscribe(this, (character, self) => self.OnCharacterUpdated.OnNext(character)).AddTo(ref disposableBag);
            Model.OnCharacterRemoved.Subscribe(this, (_, self) => self.OnCharactersChanged.OnNext(Unit.Default)).AddTo(ref disposableBag);
        }

        /// <summary>
        /// 모델 변경 감지 (전체 갱신)
        /// </summary>
        protected override void OnModelChanged()
        {
            OnCharactersChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 모든 캐릭터 가져오기
        /// </summary>
        public void GetAllCharacters(List<CharacterData> output)
        {
            Model?.GetAllCharacters(output);
        }

        /// <summary>
        /// 특정 캐릭터 가져오기
        /// </summary>
        public CharacterData GetCharacter(string instanceId)
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
        public void GetFilteredCharacters(List<CharacterData> output, Func<CharacterData, bool> filter)
        {
            Model?.GetCharactersByCondition(output, filter);
        }

        /// <summary>
        /// 레벨 범위로 필터링
        /// </summary>
        public void GetCharactersByLevelRange(List<CharacterData> output, uint minLevel, uint maxLevel)
        {
            if (Model == null || output == null) return;

            output.Clear();
            using var _ = ListPool<CharacterData>.Get(out var allCharacters);
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
        public void GetCharactersByClass(List<CharacterData> output, ClassType classType)
        {
            if (Model == null || output == null) return;

            output.Clear();
            using var _ = ListPool<CharacterData>.Get(out var allCharacters);
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
        public void GetCharactersByRarity(List<CharacterData> output, Rarity rarity)
        {
            if (Model == null || output == null) return;

            output.Clear();
            using var _ = ListPool<CharacterData>.Get(out var allCharacters);
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
