using System;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using TMPro;
using UnityEngine;

public class InGameBuffDebuff : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _baseSprite;
    [SerializeField] private SpriteRenderer _elapsedCheckSprite;
    [SerializeField] private SpriteMask _elapsedCheckMask;
    [SerializeField] private TextMeshPro _buffSubText;

    private int codeID;
    private BuffStackData _buffStackData;
    
    public bool IsWorking { get; private set; }

    public void Set((int, BuffStackData) buffData)
    {
        codeID = buffData.Item1;
        _buffStackData = buffData.Item2;
        var sprite = ImageManager.Instance.GetBuffDebuffSprite(codeID);

        if (sprite == null)
        {
            Debug.LogError($"BuffDebuff Sprite is null. CodeId : {codeID}");
            return;
        }

        if (_buffStackData.isShowValue)
        {
            _buffSubText?.gameObject.SetActive(true);
            _buffSubText.text = $"{(int)_buffStackData.value}";
        }
        else
        {
            _buffSubText?.gameObject.SetActive(false);
        }

        Debug.Log($"BuffDebuff is On. CodeId : {codeID}");
        
        IsWorking = true;
        _baseSprite.sprite = _elapsedCheckSprite.sprite = sprite;
        _elapsedCheckMask.alphaCutoff = 1.0f;
    }

    public bool RefreshCoolTime()
    {
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
