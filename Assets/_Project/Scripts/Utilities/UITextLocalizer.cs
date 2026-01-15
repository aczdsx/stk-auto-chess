using CookApps.AutoBattler;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

/// <summary>
/// UI 텍스트 로컬라이징 컴포넌트
/// Unity Localization의 LocalizedString을 사용하여 언어 변경 시 자동 갱신
/// </summary>
public class UITextLocalizer : MonoBehaviour
{
    [SerializeField] private string _languageToken;
    [SerializeField] private LocalizedString _localizedString;

    private Text _uiText;
    private TextMeshProUGUI _uiTextMeshUGUI;

    protected void Awake()
    {
        _uiText = GetComponent<Text>();
        _uiTextMeshUGUI = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(_languageToken))
        {
            Debug.LogError($"*** Language Token is empty --> {gameObject.name} ***");
            return;
        }

        _languageToken = _languageToken.Trim();

        if (_localizedString != null)
        {
            _localizedString.StringChanged += OnStringChanged;
        }

        // 초기 텍스트 설정
        RefreshText();
    }

    private void OnDisable()
    {
        if (_localizedString != null)
        {
            _localizedString.StringChanged -= OnStringChanged;
        }
    }

    private void OnStringChanged(string value)
    {
        RefreshText();
    }

    private void RefreshText()
    {
        // 동기적으로 텍스트 가져오기
        string text = _localizedString.GetLocalizedString();
        SetText(text);
    }

    private void SetText(string text)
    {
        if (_uiText != null)
        {
            _uiText.text = text;
        }
        else if (_uiTextMeshUGUI != null)
        {
            _uiTextMeshUGUI.text = text;
        }
    }
}
