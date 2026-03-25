using CookApps.BattleSystem;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoBattler
{
    public class InGameBuffDebuff : CachedMonoBehaviour
    {
        [SerializeField] private SpriteRenderer _baseSprite;
        [SerializeField] private SpriteLoader _baseSpriteLoader;
        [SerializeField] private SpriteRenderer _elapsedCheckSprite;
        [SerializeField] private SpriteLoader _elapsedCheckSpriteLoader;
        [SerializeField] private SpriteMask _elapsedCheckMask;
        [SerializeField] private TextMeshPro _buffSubText;

        private int codeID;
        private BuffStackData _buffStackData;
        private int _lastStackCount = -1;

        public bool IsWorking { get; private set; }

        public void Set((int, BuffStackData) buffData)
        {
            codeID = buffData.Item1;
            _buffStackData = buffData.Item2;

            if (_buffStackData.isShowValue)
            {
                int valueInt = (int)_buffStackData.value;
                _buffSubText.gameObject.SetActive(true);
                if (_lastStackCount != valueInt)
                {
                    _lastStackCount = valueInt;
                    _buffSubText.text = valueInt.ToString();
                }
            }
            else
            {
                _lastStackCount = -1;
                _buffSubText.gameObject.SetActive(false);
            }


            IsWorking = true;
            var sprite = SpriteNameParser.GetBuffDebuffSprite(codeID);
            _baseSpriteLoader.SetSprite(sprite).Forget();
            _elapsedCheckSpriteLoader.SetSprite(sprite).Forget();
            _elapsedCheckMask.alphaCutoff = 1.0f;
        }

        public void Set(string spriteName, float duration, float elapsedTime, int stackCount = 1)
        {
            gameObject.SetActive(true);
            IsWorking = true;
            _buffStackData ??= new BuffStackData();
            _buffStackData.duration = duration;
            _buffStackData.elapsedTime = elapsedTime;

            _baseSpriteLoader.SetSprite(spriteName).Forget();
            _elapsedCheckSpriteLoader.SetSprite(spriteName).Forget();
            _elapsedCheckMask.alphaCutoff = 1.0f;

            if (stackCount > 1)
            {
                _buffSubText.gameObject.SetActive(true);
                if (_lastStackCount != stackCount)
                {
                    _lastStackCount = stackCount;
                    _buffSubText.text = stackCount.ToString();
                }
            }
            else
            {
                _lastStackCount = -1;
                _buffSubText.gameObject.SetActive(false);
            }
        }

        public bool RefreshCoolTime()
        {
            if (_buffStackData == null)
            {
                IsWorking = false;
                return true;
            }

            // 0 ~ 1의 비율 (시간이 지남에 따라 증가)
            float coolTimeRatio = 1.0f - (_buffStackData.elapsedTime / _buffStackData.duration);

            _elapsedCheckMask.alphaCutoff = coolTimeRatio;

            if (coolTimeRatio >= 1)
            {
                IsWorking = false;
                return true;
            }

            return false;
        }
    }


    public class InGameBuffDebuffPool : Singleton<InGameBuffDebuffPool>
    {
        private UnityPool<InGameBuffDebuff> _inGameBuffDebuffPool;
        private GameObject _instance;
        private bool _loadedFromSO;

        public void Initialize(GameObject instance)
        {
            _instance = instance;
            _loadedFromSO = false;
            _inGameBuffDebuffPool = new UnityPool<InGameBuffDebuff>();
            _inGameBuffDebuffPool.Initialize(_instance);
        }

        public void Initialize()
        {
            var config = SoDataProvider.Instance.Get<CookApps.AutoChess.View.BuffIconConfigSO>();
            if (config?.BuffIconPrefab == null)
            {
                Debug.LogError("[InGameBuffDebuffPool] BuffIconConfigSO or BuffIconPrefab is null");
                return;
            }

            var handle = config.BuffIconPrefab.LoadAssetAsync<GameObject>();
            handle.Completed += op =>
            {
                _instance = op.Result;
                _loadedFromSO = true;
                _inGameBuffDebuffPool = new UnityPool<InGameBuffDebuff>();
                _inGameBuffDebuffPool.Initialize(_instance);
            };
        }

        public void Clear()
        {
            _inGameBuffDebuffPool?.ClearPool();
            _inGameBuffDebuffPool = null;
            if (_loadedFromSO && _instance != null)
            {
                UnityEngine.AddressableAssets.Addressables.Release(_instance);
                _instance = null;
                _loadedFromSO = false;
            }
        }

        public InGameBuffDebuff Get()
        {
            return _inGameBuffDebuffPool.Get(null);
        }

        public void Return(InGameBuffDebuff inGameBuffDebuff)
        {
            _inGameBuffDebuffPool?.Return(inGameBuffDebuff);
        }
    }
}