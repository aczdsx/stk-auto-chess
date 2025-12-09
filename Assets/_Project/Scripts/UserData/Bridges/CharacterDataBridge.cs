using System;
using System.Collections.Generic;
using CookApps.AutoBattler.Data;
using Tech.Hive.V1;

namespace CookApps.AutoBattler.UI
{
    /// <summary>
    /// 캐릭터 데이터 브릿지
    /// ServerDataManager와 UI 사이의 중간 레이어
    /// UI가 직접 데이터 모델을 접근하지 않고 브릿지를 통해 접근
    /// </summary>
    public class CharacterDataBridge
    {
        private CharacterModel _model;
        private ServerDataManager _dataManager;
        private DataEventBus _eventBus;

        // UI 갱신 이벤트
        public event Action OnCharactersChanged;
        public event Action<Tech.Hive.V1.CharacterInfo> OnCharacterUpdated;

        public CharacterDataBridge()
        {
            _dataManager = ServerDataManager.Instance;
            _eventBus = DataEventBus.Instance;

            // 데이터 모델 가져오기
            _model = _dataManager.GetData<CharacterModel>(CharacterModel.CATEGORY_KEY);
            if (_model == null)
            {
                _model = new CharacterModel();
                _dataManager.RegisterFactory(CharacterModel.CATEGORY_KEY, () => new CharacterModel());
                _dataManager.SetData(CharacterModel.CATEGORY_KEY, _model);
            }

            // 이벤트 구독
            SubscribeEvents();
        }

        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeEvents()
        {
            // 데이터 변경 감지
            _eventBus.Subscribe(CharacterModel.CATEGORY_KEY, OnDataChanged);

            // 모델 이벤트 구독
            _model.OnCharacterAdded += OnCharacterAdded;
            _model.OnCharacterUpdated += OnCharacterUpdatedInternal;
            _model.OnCharacterRemoved += OnCharacterRemoved;
        }

        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        public void Dispose()
        {
            _eventBus.Unsubscribe(CharacterModel.CATEGORY_KEY, OnDataChanged);

            if (_model != null)
            {
                _model.OnCharacterAdded -= OnCharacterAdded;
                _model.OnCharacterUpdated -= OnCharacterUpdatedInternal;
                _model.OnCharacterRemoved -= OnCharacterRemoved;
            }
        }

        /// <summary>
        /// 데이터 변경 콜백
        /// </summary>
        private void OnDataChanged(DataChangeEvent changeEvent)
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
            _model?.GetAllCharacters(output);
        }

        /// <summary>
        /// 특정 캐릭터 가져오기
        /// </summary>
        public Tech.Hive.V1.CharacterInfo GetCharacter(string instanceId)
        {
            return _model?.GetCharacter(instanceId);
        }

        /// <summary>
        /// 캐릭터 개수
        /// </summary>
        public int CharacterCount => _model?.CharacterCount ?? 0;

        /// <summary>
        /// 캐릭터 존재 여부
        /// </summary>
        public bool HasCharacter(string instanceId)
        {
            return _model?.HasCharacter(instanceId) ?? false;
        }

        /// <summary>
        /// 조건에 맞는 캐릭터 필터링
        /// </summary>
        public void GetFilteredCharacters(List<Tech.Hive.V1.CharacterInfo> output, Func<Tech.Hive.V1.CharacterInfo, bool> filter)
        {
            _model?.GetCharactersByCondition(output, filter);
        }

        /// <summary>
        /// 레벨 범위로 필터링
        /// </summary>
        public void GetCharactersByLevelRange(List<Tech.Hive.V1.CharacterInfo> output, uint minLevel, uint maxLevel)
        {
            if (_model == null || output == null) return;

            output.Clear();
            var allCharacters = new List<Tech.Hive.V1.CharacterInfo>();
            _model.GetAllCharacters(allCharacters);

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
            if (_model == null || output == null) return;

            output.Clear();
            var allCharacters = new List<Tech.Hive.V1.CharacterInfo>();
            _model.GetAllCharacters(allCharacters);

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
            if (_model == null || output == null) return;

            output.Clear();
            var allCharacters = new List<Tech.Hive.V1.CharacterInfo>();
            _model.GetAllCharacters(allCharacters);

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
