using System;
using System.Collections.Generic;
using System.Threading;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class InGameTopUI : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private CAButton _pauseButton;
    [SerializeField] private CAButton _skipButton;

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
    [SerializeField] private List<SynergyInGameElement> _playerSynergyUIList;
    [SerializeField] private List<SynergyInGameElement> _enemySynergyUIList;

    [Space]
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

    [Space]
    [SerializeField] private RectTransform _elementCounterUI;
    [SerializeField] private GameObject _elementCounterUIParent;


    private static readonly Dictionary<SynergyType, float> elementCounterUIZRotation = new Dictionary<SynergyType, float>
    {
        { SynergyType.WIND, 0f },
        { SynergyType.LIGHTNING, -72f },
        { SynergyType.EARTH, -144f },
        { SynergyType.WATER, -216f },
        { SynergyType.FIRE, -288f },
    };




    private const float AnimationDuration = 0.5f; // 애니메이션 지속 시간
    private float beforePlayerHpRate = 1.0f;
    private float beforeEnemyHpRate = 1.0f;

    private Type _failType;
    private struct SynergyCountData
    {
        public SynergyType Type;
        public int Count;
        public SynergyCountData(SynergyType type, int count)
        {
            Type = type;
            Count = count;
        }
    }

    private void Awake()
    {
        _pauseButton?.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickPauseButton()).AddTo(this);
        _skipButton?.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickSkipButton()).AddTo(this);
    }

    public void UpdateTimeUI(float time)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(time);
        _timeText.text = timeSpan.ToString(@"mm\:ss");
    }

    public void UpdateSynergyUI(AllianceType type, bool isCombat)
    {
        if (type != AllianceType.Player)
            return;
        List<SynergyInGameElement> _synergyUIList = type == AllianceType.Player ? _playerSynergyUIList : _enemySynergyUIList;
        if (isCombat)
        {
            // _synergyUIList = type == AllianceType.Player ? _combatPlayerSynergyUIList : _combatEnemySynergyUIList;
        }
        int uiIndex = 0;
        int uiListCount = _synergyUIList.Count;

        void TrySetSynergyUI(Action setSynergyAction)//이 함수는 여기서 밖에 콜을 안하기때문에 이렇게 작성
        {
            if (uiIndex < uiListCount && _synergyUIList[uiIndex] != null)
            {
                setSynergyAction();
                _synergyUIList[uiIndex].gameObject.SetActive(true);
                uiIndex++;
            }
        }

        foreach (SynergyInGameElement synergyUI in _synergyUIList)
        {
            if (synergyUI == null) continue;
            synergyUI.gameObject.SetActive(false);
        }


        List<SynergyCountData> synergyCountDataList = new List<SynergyCountData>();
        InGameObjectManager inGameObjectManagerInstance = InGameObjectManager.Instance;

        int synergyTypeCount = Enum.GetValues(typeof(SynergyType)).Length;
        //  아래 루프는 SynergyType enum의 배치가
        //  element부터 enum값이 편성되어있기때문에 기존 플로우를 따라 Position부터 넣어야함.
        for (int i = synergyTypeCount - 1; i > 0; i--)
        {
            SynergyType synergyType = (SynergyType)i;

            var count = inGameObjectManagerInstance.GetCharacterSynergyCount(type, synergyType);
            if (count > 0)
            {
                synergyCountDataList.Add(new SynergyCountData(
                    synergyType,
                    count
                    ));
            }
        }

        synergyCountDataList.Sort((a, b) => b.Count.CompareTo(a.Count));// 내림차순 수행.
        if (synergyCountDataList.Count > 9)
        {
            //Take(9)
            synergyCountDataList.RemoveRange(9, synergyCountDataList.Count - 9);
        }

        // count가 높은 순서대로 정렬 (내림차순 유지)

        var specDataManagerInstance = SpecDataManager.Instance;
        foreach (var synergyCountData in synergyCountDataList)
        {
            var canSynergy = specDataManagerInstance.TryGetSynergyDataByCount(synergyCountData.Type, synergyCountData.Count,
            out var outSynergyData, out var outSynergyList);

            if (outSynergyList != null && outSynergyList.Count > 0)
            {
                if (outSynergyData != null)
                {//여기오면 일단 1단계는 달성했다는거임.
                    var nextData = outSynergyList.Find(l => l.grade == outSynergyData.grade + 1) ?? outSynergyData;
                    TrySetSynergyUI(() =>
                        _synergyUIList[uiIndex].SetSynergy(synergyCountData.Type, synergyCountData.Count, outSynergyData, nextData, isActive: true)
                    );
                }
                else if (outSynergyData == null && synergyCountData.Count > 0)
                {//여긴 1단계도 안됏다는거임.
                    if (type == AllianceType.Player)
                    {
                        var nextData = outSynergyList[0];
                        TrySetSynergyUI(() =>
                            _synergyUIList[uiIndex].SetSynergy(synergyCountData.Type, synergyCountData.Count, nextData,
                            nextData, isActive: false)
                        );

                        continue;
                    }
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
        if (_combatPlayerAttr == null || _combatEnemyAttr == null)
            return;

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
    public void SetElementCounterUI(SynergyType synergyType)
    {
        if (!elementCounterUIZRotation.TryGetValue(synergyType, out var rotation))
        {
            _elementCounterUIParent.SetActive(false);
            return;
        }
        _elementCounterUIParent.SetActive(true);
        _elementCounterUI.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    public void AddKillLog(in CookApps.AutoBattler.KillSource source, CharacterController death, bool isPlayerKill)
    {
        if (_killLogRootTransform == null || _killLogItemPrefab == null)
            return;

        var item = Instantiate(_killLogItemPrefab, _killLogRootTransform);
        item.transform.position = _killLogRootTransform.position;
        item.RectTransform.SetAsFirstSibling();
        item.SetData(source, death, isPlayerKill);
        item.OnDespawn += HandleKillLogDespawn;

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

        const float slideDuration = 0.2f;
        float elapsed = 0f;

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
        // 테스트 모드일 경우 게임 종료
        if (InGameResourceHolder.InGameType == InGameType.TEST)
        {
            Debug.LogColor("[Test] 테스트 재시작 - 씬 재로드");

            // 인게임 정리
            InGameManager.Instance.EndInGame();

            // 씬 재로드
            SceneTransition.Create<SceneTransition_FadeInOut>();
            SceneTransition.FadeInAsync().Forget();


            var inGameMainParams = new InGameMainParams(
                InGameType.TEST,
                new InGameMainStateTest(),
                InGameManager.Instance.TestConfig.StageChapterId
            );

            SceneLoading.GoToNextScene("InGame_New", inGameMainParams);
            return;
        }

        // 챕터 1 (튜토리얼 스테이지) 관련 처리
        if (InGameManager.Instance.SpecStage != null
        && InGameManager.Instance.SpecStage.chapter_id == 1
        && !ServerDataManager.Instance.Battle.IsStageCleared((uint)InGameManager.Instance.SpecStage.stage_id))
        {
            ToastManager.Instance.ShowToastByTokenKey("TUTORIAL_PLAYING_ALERT");
            return;
        }

        SceneUILayerManager.Instance.PushUILayerAsync<InGameExitPopup>(_failType).Forget();
    }

    private void OnClickSkipButton()
    {
        InGameMain.GetInGameMain().Skip();
    }

    public void SetSkipButtonVisible(bool visible)
    {
        if (_skipButton != null)
            _skipButton.gameObject.SetActive(visible);
    }

    public void InitTopUI(Type type)
    {
        _failType = type;
    }
    public void InitCombatTopUI()
    {
        for (int i = 0; i < _playerSynergyUIList.Count; ++i)
        {
            _playerSynergyUIList[i].SetCountTextVisible(false);
        }
    }
}
