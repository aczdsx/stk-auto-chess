using System;
using System.Collections;
using CookApps.AutoBattler;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITextLocalizer : MonoBehaviour
{
    [SerializeField] private string _languageToken;
    
    private Text _uiText;
    private TextMeshProUGUI _uiTextMeshUGUI;
    
    protected void Awake()
    {
        _uiText = GetComponent<Text>();
        _uiTextMeshUGUI = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(_languageToken))
        {
            Debug.LogError($"*** Language Token is empty --> {gameObject.name} ***");
            return;
        }
        
        _languageToken = _languageToken.Trim();
            
        if (_uiText != null)
        {
            _uiText.text = LanguageManager.Instance.GetLanguageText(_languageToken);
        }
        else if (_uiTextMeshUGUI != null)
        {
            _uiTextMeshUGUI.text = LanguageManager.Instance.GetLanguageText(_languageToken);
        }
    }
}
