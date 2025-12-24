using CookApps.AutoBattler;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class InGameKillLogItem : MonoBehaviour
{
    private RectTransform _rectTransform;
    [SerializeField] private TextMeshProUGUI _killCharacterNameText;
    [SerializeField] private TextMeshProUGUI _deathCharacterNameText;

    [SerializeField] private Image _killCharacterImage;
    [SerializeField] private Image _deathCharacterImage;
    [SerializeField] private SpriteLoader _killCharacterSpriteLoader;
    [SerializeField] private SpriteLoader _deathCharacterSpriteLoader;

    [SerializeField] private Animator _animator;

    public System.Action<InGameKillLogItem> OnDespawn;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
    }

    public RectTransform RectTransform => _rectTransform;
    public float Height => _rectTransform != null ? _rectTransform.rect.height : 0f;

    public void SetData(CharacterController kill, CharacterController death, bool isPlayerKill)
    {
        if (isPlayerKill)
            _animator.SetTrigger("SetKill");
        else
            _animator.SetTrigger("SetDead");
        _killCharacterNameText.text = LanguageManager.Instance.GetLanguageText(kill.SpecCharacter.name_token);
        _deathCharacterNameText.text = LanguageManager.Instance.GetLanguageText(death.SpecCharacter.name_token);

        _killCharacterSpriteLoader.SetSprite(SpriteNameParser.GetCharacterSmallItemSprite(kill.SpecCharacter.prefab_id)).Forget();
        _deathCharacterSpriteLoader.SetSprite(SpriteNameParser.GetCharacterSmallItemSprite(death.SpecCharacter.prefab_id)).Forget();
    }

    public void SetData(CookApps.AutoBattler.KillSource source, CharacterController death, bool isPlayerKill)
    {
        if (isPlayerKill)
            _animator.SetTrigger("SetKill");
        else
            _animator.SetTrigger("SetDead");

        ExtractSourceData(source);
        _deathCharacterNameText.text = LanguageManager.Instance.GetLanguageText(death.SpecCharacter.name_token);
        _deathCharacterSpriteLoader.SetSprite(SpriteNameParser.GetCharacterSmallItemSprite(death.SpecCharacter.prefab_id)).Forget();
    }

    private void ExtractSourceData(CookApps.AutoBattler.KillSource source)
    {
        switch (source.Type)
        {
            case AttackerType.CHARCTER:
                if (source.Character != null)
                {
                    _killCharacterNameText.text = LanguageManager.Instance.GetLanguageText(source.Character.SpecCharacter.name_token);
                    _killCharacterSpriteLoader.SetSprite(SpriteNameParser.GetCharacterSmallItemSprite(source.Character.SpecCharacter.prefab_id)).Forget();
                }
                else
                {
                    _killCharacterNameText.text = LanguageManager.Instance.GetLanguageText(source.Id.ToString());
                    _killCharacterSpriteLoader.UnloadSprite();
                }
                break;
            case AttackerType.COMMANDER_SKILL:
                {
                    var data = SpecDataManager.Instance.GetCommanderSkillDataList((int)source.Id);
                    _killCharacterNameText.text = data != null ? LanguageManager.Instance.GetLanguageText(data[0].name_token) : source.Id.ToString();
                    _killCharacterSpriteLoader.UnloadSprite();
                }
                break;
            case AttackerType.CHAPTER_RULE:
                {
                    var data = SpecDataManager.Instance.GetChapterRuleData((int)source.Id);
                    _killCharacterNameText.text = data != null ? LanguageManager.Instance.GetLanguageText(data.name_token) : source.Id.ToString();
                    _killCharacterSpriteLoader.UnloadSprite();
                }
                break;
        }
    }

    public void Desytroy()
    {
        // 애니메이션 이벤트로 호출됨. 상위 스택에 소멸을 알린 다음 파괴한다.
        try { OnDespawn?.Invoke(this); }
        catch { }
        Destroy(this.gameObject);
    }
}
