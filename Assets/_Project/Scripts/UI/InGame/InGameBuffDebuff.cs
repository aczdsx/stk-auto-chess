using CookApps.BattleSystem;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

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

        public bool IsWorking { get; private set; }

        public void Set((int, BuffStackData) buffData)
        {
            codeID = buffData.Item1;
            _buffStackData = buffData.Item2;

            if (_buffStackData.isShowValue)
            {
                _buffSubText.gameObject.SetActive(true);
                _buffSubText.text = $"{(int)_buffStackData.value}";
            }
            else
            {
                _buffSubText.gameObject.SetActive(false);
            }


            IsWorking = true;
            var sprite = SpriteNameParser.GetBuffDebuffSprite(codeID);
            _baseSpriteLoader.SetSprite(sprite).Forget();
            _elapsedCheckSpriteLoader.SetSprite(sprite).Forget();
            _elapsedCheckMask.alphaCutoff = 1.0f;
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

        public void Initialize(GameObject instance)
        {
            // TODO: load hp bar prefab from addressable
            _instance = instance;
            _inGameBuffDebuffPool = new UnityPool<InGameBuffDebuff>();
            _inGameBuffDebuffPool.Initialize(_instance);
        }

        public void Clear()
        {
            _inGameBuffDebuffPool.ClearPool();
            _inGameBuffDebuffPool = null;
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