using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommanderSkillUI : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Slider _coolTimeSlider;

    public void UpdateCommanderSkillCoolTime(float rate)
    {
        _coolTimeSlider.value = rate;
    }

    public void SetIconColor(float fadeAlpha)
    {
        if (_iconImage != null)
        {
            Color color = _iconImage.color;
            color.a = fadeAlpha;
            _iconImage.color = color;
        }
    }
}
