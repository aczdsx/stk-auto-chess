using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class InGameTopUI : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private CAButton _pauseButton;

    [Space]
    [SerializeField] private TextMeshProUGUI _timeText;

    [Space]
    [SerializeField] private TextMeshProUGUI _playerAttrText;
    [SerializeField] private TextMeshProUGUI _enemyAttrText;

    [Space]
    [SerializeField] private TextMeshProUGUI _playerHpRate;
    [SerializeField] private TextMeshProUGUI _enemyHpRate;
    [SerializeField] private Slider _enemySlider;
    [SerializeField] private Slider _playerSlider;
    [SerializeField] private Slider _enemyDelayedSlider;
    [SerializeField] private Slider _playerDelayedSlider;
    [SerializeField] private InGameRatioTween _playerSynergyRationTween;
    [SerializeField] private InGameRatioTween _enemySynergyRationTween;

    [Space]
    [SerializeField] private List<InGameSynergyUI> _playerSynergyUIList;
    [SerializeField] private List<InGameSynergyUI> _enemySynergyUIList;

    [Space]
    [SerializeField] private List<InGameSynergyUI> _combatPlayerSynergyUIList;
    [SerializeField] private List<InGameSynergyUI> _combatEnemySynergyUIList;
    [SerializeField] private TextMeshProUGUI _combatPlayerAttr;
    [SerializeField] private TextMeshProUGUI _combatEnemyAttr;
    [SerializeField] private TextMeshProUGUI _myName;
    [SerializeField] private TextMeshProUGUI _stageName;


    private const float AnimationDuration = 0.5f; // 애니메이션 지속 시간
    private float beforePlayerHpRate = 1.0f;
    private float beforeEnemyHpRate = 1.0f;

    private Type _failType;

    private void Awake()
    {
        _pauseButton.onClick.AddListener(OnClickPauseButton);
    }

    private void OnDestroy()
    {
        _pauseButton.onClick.RemoveListener(OnClickPauseButton);
    }

    public void UpdateTimeUI(float time)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(time);
        _timeText.text = timeSpan.ToString(@"mm\:ss");
    }

    public void UpdateSynergyUI(AllianceType type, bool isCombat)
    {
        List<InGameSynergyUI> _synergyUIList = type == AllianceType.Player ? _playerSynergyUIList : _enemySynergyUIList;
        if (isCombat)
        {
            _synergyUIList = type == AllianceType.Player ? _combatPlayerSynergyUIList : _combatEnemySynergyUIList;
        }
        int uiIndex = 0;
        int uiListCount = _synergyUIList.Count;

        void TrySetSynergyUI(Action setSynergyAction)
        {
            if (uiIndex < uiListCount)
            {
                setSynergyAction();
                _synergyUIList[uiIndex].gameObject.SetActive(true);
                uiIndex++;
            }
        }

        foreach (InGameSynergyUI synergyUI in _synergyUIList)
        {
            synergyUI.gameObject.SetActive(false);
        }

        bool isDescending = type == AllianceType.Enemy;

        var characterPositionCounts = Enum.GetValues(typeof(CharacterPositionType))
            .Cast<CharacterPositionType>()
            .Where(characterPosition => characterPosition != CharacterPositionType.NONE)
            .Select(characterPosition => new
            {
                Type = (object)characterPosition,
                Count = InGameObjectManager.Instance.GetCharacterSynergyCount(type, characterPosition),
                IsCharacterPosition = true
            });

        var elementTypeCounts = Enum.GetValues(typeof(ElementType))
            .Cast<ElementType>()
            .Where(elementType => elementType != ElementType.NONE)
            .Select(elementType => new
            {
                Type = (object)elementType,
                Count = InGameObjectManager.Instance.GetCharacterSynergyCount(type, elementType),
                IsCharacterPosition = false
            });

        var synergyCounts = characterPositionCounts
            .Concat(elementTypeCounts)
            .Where(x => x.Count > 0);

        synergyCounts = isDescending
            ? synergyCounts.OrderByDescending(x => x.Count)
            : synergyCounts.OrderBy(x => x.Count);

        var synergyCountList = synergyCounts.ToList();

        foreach (var synergyCount in synergyCountList)
        {
            if (synergyCount.IsCharacterPosition)
            {
                var list = SpecDataManager.Instance.GetSpecSynergyList((CharacterPositionType)synergyCount.Type);
                var data = list.Find(l => l.min_count <= synergyCount.Count && l.max_count >= synergyCount.Count);

                if (data.grade > 0)
                {
                    TrySetSynergyUI(() =>
                        _synergyUIList[uiIndex].SetPositionSynergy((CharacterPositionType)synergyCount.Type, synergyCount.Count, data.grade)
                    );
                }
            }
            else
            {
                var list = SpecDataManager.Instance.GetSpecSynergyList((ElementType) synergyCount.Type);
                var data = list.Find(l => l.min_count <= synergyCount.Count && l.max_count >= synergyCount.Count);

                if (data.grade > 0)
                {
                    TrySetSynergyUI(() =>
                        _synergyUIList[uiIndex]
                            .SetSynergy((ElementType) synergyCount.Type, synergyCount.Count, data.grade)
                    );
                }

                if (!isCombat)
                {
                    if (data.grade > 0)
                        InGameObjectManager.Instance.SpawnSynergyFx(type, (ElementType) synergyCount.Type);
                }
            }
        }


    }

    public void UpdateAttrUI(AllianceType type)
    {
        string attrText = InGameObjectManager.Instance.GetAttrText(type);
        if (type == AllianceType.Player)
        {
            _combatPlayerAttr.text = attrText;
        }
        else
        {
            _combatEnemyAttr.text = attrText;
        }
    }

    private CancellationTokenSource playerAnimationCts;
    private CancellationTokenSource enemyAnimationCts;

    public void UpdateTopHpUI(AllianceType type)
    {
        float rate = InGameObjectManager.Instance.GetHpRate(type);
        if (type == AllianceType.Player)
        {
            if (!Mathf.Approximately(beforePlayerHpRate, rate))
                _playerSynergyRationTween.DamageFXTween();

            _playerHpRate.text = rate.ToString("P0");
            _playerSlider.value = rate + 0.01f;
        
            playerAnimationCts?.Cancel();
            playerAnimationCts = new CancellationTokenSource();
        
            AnimateHpBar(_playerDelayedSlider, _playerDelayedSlider.value, rate, playerAnimationCts.Token);

            beforePlayerHpRate = rate;
        }
        else
        {
            if (!Mathf.Approximately(beforeEnemyHpRate, rate))
                _enemySynergyRationTween.DamageFXTween();

            _enemyHpRate.text = rate.ToString("P0");
            _enemySlider.value = rate + 0.01f;
        
            enemyAnimationCts?.Cancel();
            enemyAnimationCts = new CancellationTokenSource();
        
            AnimateHpBar(_enemyDelayedSlider, _enemyDelayedSlider.value, rate, enemyAnimationCts.Token);

            beforeEnemyHpRate = rate;
        }
    }

    public void SetStageName(string stageName)
    {
        _stageName.text = stageName;
    }
    
    public void SetMyName(string stageName)
    {
        _myName.text = stageName;
    }

    private async UniTask AnimateHpBar(Slider slider, float startRatio, float targetRatio, CancellationToken cancellationToken)
    {
        float elapsed = 0f;

        while (elapsed < AnimationDuration)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            slider.value = Mathf.Lerp(startRatio, targetRatio, elapsed / AnimationDuration);

            await UniTask.Yield(cancellationToken);
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            slider.value = targetRatio; 
        }
    }

    private void OnClickPauseButton()
    {
        SceneUILayerManager.Instance.PushUILayerAsync<InGameExitPopup>(_failType).Forget();
    }

    public void InitTopUI(Type type)
    {
        _failType = type;
    }
}
