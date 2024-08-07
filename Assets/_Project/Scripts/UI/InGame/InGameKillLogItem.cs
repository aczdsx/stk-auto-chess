using CookApps.AutoBattler;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class InGameKillLogItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _killCharacterNameText;
    [SerializeField] private TextMeshProUGUI _deathCharacterNameText;
    
    [SerializeField] private Image _killCharacterImage;
    [SerializeField] private Image _deathCharacterImage;

    [SerializeField] private Animator _animator;

    public void SetData(CharacterController kill, CharacterController death, bool isPlayerKill)
    {
        if(isPlayerKill)
            _animator.SetTrigger("SetKill");
        else
            _animator.SetTrigger("SetDead");
        _killCharacterNameText.text = LanguageManager.Instance.GetLanguageText(kill.SpecCharacter.name_token);
        _deathCharacterNameText.text = LanguageManager.Instance.GetLanguageText(death.SpecCharacter.name_token);
        
        _killCharacterImage.sprite = ImageManager.Instance.GetCharacterSmallItemSprite(kill.SpecCharacter.prefab_id);
        _deathCharacterImage.sprite = ImageManager.Instance.GetCharacterSmallItemSprite(death.SpecCharacter.prefab_id);
    }

    public void Desytroy()
    {
        Destroy(this.gameObject);
    }
}
