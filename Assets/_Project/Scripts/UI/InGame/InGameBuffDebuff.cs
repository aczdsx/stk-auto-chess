using System;
using CookApps.AutoBattler;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InGameBuffDebuff : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _baseSprite;
    [SerializeField] private SpriteRenderer _elapsedCheckSprite;

    public void SetData(CharacterStatData characterStat, Action<CharacterStatData> onSelected)
    {
    }
}
