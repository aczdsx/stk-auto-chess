using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [SerializeField] private Transform _killLogRootTransform;
    [SerializeField] private InGameKillLogItem _killLogItemPrefab;
    [SerializeField] private float _killLogItemGapY = 40f; // 킬로그 아이템 간 간격

    [Space]
    [SerializeField] private ScrollRect _playerSynergyScrollRect;
    [SerializeField] private ScrollRect _enemySynergyScrollRect;
    [SerializeField] private ScrollRect _combatPlayerSynergyScrollRect;
    [SerializeField] private ScrollRect _combatEnemySynergyScrollRect;

    [Space]
    [SerializeField] private ContentSizeFitter _playerSynergyContentFitter;
    [SerializeField] private ContentSizeFitter _combatPlayerSynergyContentFitter;

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

        synergyCounts = synergyCounts.OrderByDescending(x => x.Count).Take(9);
        if (!isDescending)
            synergyCounts = synergyCounts.OrderBy(x => x.Count);

        var synergyCountList = synergyCounts.ToList();

        foreach (var synergyCount in synergyCountList)
        {
            if (synergyCount.IsCharacterPosition)
            {
                var list = SpecDataManager.Instance.GetSpecSynergyList((CharacterPositionType)synergyCount.Type);
                var data = list.Find(l => l.min_count <= synergyCount.Count && l.max_count >= synergyCount.Count);
                var nextData = list.Find(l => l.grade == data.grade + 1) ?? data;

                // [TODO] 0 등급 임시 시너지 표현 제외 처리
                bool isActiveZeroGrade = (type == AllianceType.Player) ? data.grade >= 0 : data.grade > 0;
                if (isActiveZeroGrade)
                {
                    TrySetSynergyUI(() =>
                        _synergyUIList[uiIndex].SetPositionSynergy((CharacterPositionType)synergyCount.Type, synergyCount.Count, data, nextData, data.grade > 0)
                    );
                }
            }
            else
            {
                var list = SpecDataManager.Instance.GetSpecSynergyList((ElementType)synergyCount.Type);
                var data = list.Find(l => l.min_count <= synergyCount.Count && l.max_count >= synergyCount.Count);
                var nextData = list.Find(l => l.grade == data.grade + 1) ?? data;

                // [TODO] 0 등급 임시 시너지 표현 제외 처리
                bool isActiveZeroGrade = (type == AllianceType.Player) ? data.grade >= 0 : data.grade > 0;
                if (isActiveZeroGrade)
                {
                    TrySetSynergyUI(() =>
                        _synergyUIList[uiIndex]
                            .SetSynergy((ElementType)synergyCount.Type, synergyCount.Count, data, nextData)
                    );
                }

                if (!isCombat)
                {
                    if (isActiveZeroGrade)
                        InGameObjectManager.Instance.SpawnSynergyFx(type, (ElementType)synergyCount.Type);
                }
            }
        }

        if (isCombat)
        {
            if (type == AllianceType.Player)
            {
                EnsureRightAlign(_combatPlayerSynergyScrollRect, _combatPlayerSynergyContentFitter, true).Forget();
            }
            else
            {
                SnapLeft(_combatEnemySynergyScrollRect);
            }
        }
        else
        {
            if (type == AllianceType.Player)
            {
                EnsureRightAlign(_playerSynergyScrollRect, _playerSynergyContentFitter, true).Forget();
            }
            else
            {
                SnapLeft(_enemySynergyScrollRect);
            }
        }
    }

    private async UniTask EnsureRightAlign(ScrollRect sr, ContentSizeFitter fitter, bool snapRight)
    {
        if (sr == null) return;
        await UniTask.NextFrame();
        Canvas.ForceUpdateCanvases();

        var content = sr.content;
        var vp = sr.viewport != null ? sr.viewport : sr.transform as RectTransform;
        if (content == null || vp == null) return;

        bool isShort = content.rect.width <= vp.rect.width + 0.1f;

        if (fitter != null)
        {
            if (isShort)
            {
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, vp.rect.width);
            }
            else
            {
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        if (isShort)
        {
            var pos = content.anchoredPosition;
            pos.x = 0f; // Pivot X=1 기준 오른쪽 붙임
            content.anchoredPosition = pos;
        }
        else if (snapRight)
        {
            sr.horizontalNormalizedPosition = 1f;
        }

        sr.velocity = Vector2.zero;
    }

    private async void SnapLeft(ScrollRect sr)
    {
        if (sr == null) return;
        await UniTask.NextFrame();
        Canvas.ForceUpdateCanvases();
        sr.horizontalNormalizedPosition = 0f;
        sr.velocity = Vector2.zero;
    }

    public void UpdateAttrUI(AllianceType type, bool isCombat)
    {
        TextMeshProUGUI textMesh = type == AllianceType.Player ? _playerAttrText : _enemyAttrText;
        if (isCombat)
        {
            textMesh = type == AllianceType.Player ? _combatPlayerAttr : _combatEnemyAttr;
        }

        string attrText = InGameObjectManager.Instance.GetAttrText(type);
        if (type == AllianceType.Player)
        {
            textMesh.text = attrText;
        }
        else
        {
            textMesh.text = attrText;
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

    public void AddKillLog(CharacterController kill, CharacterController death, bool isPlayerKill)
    {
        if (_killLogRootTransform == null || _killLogItemPrefab == null)
            return;

        var item = Instantiate(_killLogItemPrefab, _killLogRootTransform);
        item.transform.position = _killLogRootTransform.position;
        item.RectTransform.SetAsFirstSibling();
        item.SetData(kill, death, isPlayerKill);
        item.OnDespawn += HandleKillLogDespawn;

        // 새 항목은 맨 아래(최신이 위로 쌓이게 하려면 Layout을 Top-Down으로 설정)로 추가됨
        _killLogItems.Insert(0, item);
        RelayoutKillLogs(animated: true);
    }

    private readonly List<InGameKillLogItem> _killLogItems = new List<InGameKillLogItem>();
    private CancellationTokenSource _killLogLayoutCts;

    private void HandleKillLogDespawn(InGameKillLogItem item)
    {
        item.OnDespawn -= HandleKillLogDespawn;
        _killLogItems.Remove(item);
        // 남은 항목을 위로 슬라이드
        RelayoutKillLogs(animated: true);
    }

    private async void RelayoutKillLogs(bool animated)
    {
        if (_killLogRootTransform == null)
            return;

        _killLogLayoutCts?.Cancel();
        _killLogLayoutCts = new CancellationTokenSource();
        var token = _killLogLayoutCts.Token;

        // 상단 기준으로 쌓이는 형태를 가정. 항목의 anchoredPosition.y를 음수로 증가시키며 정렬
        float currentY = 0f;
        var items = _killLogItems; // 최신 순서 유지

        // 레이아웃 즉시 모드: 애니메이션 없이 위치 지정
        if (!animated)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var rt = items[i].RectTransform;
                if (rt == null) continue;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -currentY);
                currentY += rt.rect.height + _killLogItemGapY;
            }
            return;
        }

        // 애니메이션 모드: 부드럽게 이동
        const float slideDuration = 0.2f;
        float elapsed = 0f;

        // 시작/목표 포지션 캡처
        var startPositions = new Vector2[items.Count];
        var targetPositions = new Vector2[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            var rt = items[i].RectTransform;
            if (rt == null) continue;
            startPositions[i] = rt.anchoredPosition;
            targetPositions[i] = new Vector2(rt.anchoredPosition.x, -currentY);
            currentY += rt.rect.height + _killLogItemGapY;
        }

        while (elapsed < slideDuration)
        {
            if (token.IsCancellationRequested)
                return;

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            // easeOutCubic
            t = 1f - Mathf.Pow(1f - t, 3f);

            for (int i = 0; i < items.Count; i++)
            {
                var rt = items[i].RectTransform;
                if (rt == null) continue;
                rt.anchoredPosition = Vector2.LerpUnclamped(startPositions[i], targetPositions[i], t);
            }

            await UniTask.Yield(token).SuppressCancellationThrow();
            if (token.IsCancellationRequested)
                return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            var rt = items[i].RectTransform;
            if (rt == null) continue;
            rt.anchoredPosition = targetPositions[i];
        }
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

            await UniTask.Yield(cancellationToken).SuppressCancellationThrow();
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            slider.value = targetRatio;
        }
    }

    private void OnClickPauseButton()
    {
        // 챕터 1 (튜토리얼 스테이지) 관련 처리
        if (InGameManager.Instance.SpecStage.chapter_id == 1 && !UserDataManager.Instance.IsClearStage(InGameManager.Instance.SpecStage.stage_id))
        {
            ToastManager.Instance.ShowToastByTokenKey("TUTORIAL_PLAYING_ALERT");
            return;
        }

        SceneUILayerManager.Instance.PushUILayerAsync<InGameExitPopup>(_failType).Forget();
    }

    public void InitTopUI(Type type)
    {
        _failType = type;
    }
}
