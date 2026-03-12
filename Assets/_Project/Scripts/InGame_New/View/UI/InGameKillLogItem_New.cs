using CookApps.AutoBattler;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    public class InGameKillLogItem_New : MonoBehaviour
    {
        [SerializeField] private TMP_Text _killerNameText;
        [SerializeField] private TMP_Text _victimNameText;
        [SerializeField] private SpriteLoader _killerSpriteLoader;
        [SerializeField] private SpriteLoader _victimSpriteLoader;
        [SerializeField] private Animator _animator;

        public System.Action<InGameKillLogItem_New> OnDespawn;
        public RectTransform RectTransform { get; private set; }
        public float Height => RectTransform != null ? RectTransform.rect.height : 0f;

        private void Awake() => RectTransform = transform as RectTransform;

        public void SetData(int killerChampSpecId, int victimChampSpecId, bool isPlayerKill)
        {
            _animator.SetTrigger(isPlayerKill ? "SetKill" : "SetDead");

            var killerSpec = SpecDataManager.Instance.GetSpecCharacter(killerChampSpecId);
            var victimSpec = SpecDataManager.Instance.GetSpecCharacter(victimChampSpecId);

            _killerNameText.text = LanguageManager.Instance.GetDefaultText(killerSpec?.name_token ?? "");
            _victimNameText.text = LanguageManager.Instance.GetDefaultText(victimSpec?.name_token ?? "");

            if (killerSpec != null)
                _killerSpriteLoader.SetSprite(
                    SpriteNameParser.GetCharacterSmallItemSprite(killerSpec.prefab_id)).Forget();
            if (victimSpec != null)
                _victimSpriteLoader.SetSprite(
                    SpriteNameParser.GetCharacterSmallItemSprite(victimSpec.prefab_id)).Forget();
        }

        // 애니메이션 이벤트에서 호출 (일정 시간 후 자동 소멸)
        public void Despawn()
        {
            OnDespawn?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
