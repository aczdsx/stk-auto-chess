using System;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CharacterInfo = CookApps.AutoBattler.CharacterInfo;

public class InGameCharacterItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const float LONG_PRESS_DURATION = 0.5f;

    public CharacterStatData StatData => _statData;
    public bool IsFocusSlot => _focusObj.activeSelf;
    [SerializeField] private Transform _characterIconRoot;
    [SerializeField] private SpriteLoader _SynergyImageSpriteLoader;
    [SerializeField] private SpriteLoader _SynergyClassImageSpriteLoader;
    [SerializeField] private GameObject _body;
    [SerializeField] private GameObject _focusObj;
    [SerializeField] private SpriteLoader _focusImageSpriteLoader;
    [SerializeField] private Animation _dropFxAnimation;
    [SerializeField] private TextMeshProUGUI _focusText;
    [SerializeField] private ParticleSystem _guideFx;
    [SerializeField] private SimpleImageColorSwapper  _positionColorSwapper;
    [SerializeField] private TextMeshProUGUI _characterPositionTypeText;
    [SerializeField] private TextMeshProUGUI _attrText;

    private Action<CharacterStatData> _onSelected;
    private CharacterStatData _statData;
    private GameObject _loadedCharacterIcon;

    private InGameBottomUI _parentUI;

    // 롱탭 기능 관련
    private bool _isShowLongPressFunc = false;
    private bool _isPressing = false;
    private float _pressTime;

    // 드래그 기능 관련
    private bool _isOverBoard = false;  // 보드 영역에 진입했는지
    private bool _isCharacterSpawned = false;  // 보드에 캐릭터가 생성되었는지
    private bool _scrollRectDragStarted = false;  // ScrollRect 드래그가 시작되었는지
    private ScrollRect _cachedScrollRect;  // 횡스크롤용 ScrollRect 캐싱

    public void SetData(InGameBottomUI parent, CharacterStatData characterStat, Action<CharacterStatData> onSelected)
    {
        _parentUI = parent;
        _statData = characterStat;
        bool isExsist = _statData != null;

        _body.SetActive(isExsist);
        if (_body.activeSelf)
        {
            LoadCharacterIcon(_statData.Spec.prefab_id).Forget();
            _SynergyImageSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(_statData.Spec.character_element_type)).Forget();
            _SynergyClassImageSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(_statData.Spec.character_stella_type)).Forget();
            _characterPositionTypeText.text = _statData.Spec.character_position_type.ToString();
            _attrText.text = $"{_statData.GetAttrValueCP().ToString("n0")}";
            if (_statData.Spec.atk_type == AtkType.AD)
                _positionColorSwapper.Swap(SimpleSwapType.AD);
            else
                _positionColorSwapper.Swap(SimpleSwapType.AP);
        }

        if (_guideFx)
            _guideFx.gameObject.SetActive(false);

        _onSelected = onSelected;
    }

    public void OnClickItem()
    {
        // [상건] 기존 배치는 우선 주석으로 합니다.
        // if (_statData != null && !_isShowLongPressFunc)
        // {
        //     if (_guideFx != null)
        //     {
        //         if (_guideFx.gameObject.activeSelf && _statData.CharacterID == 130301)
        //         {
        //             // var specSynergyDataList = SpecDataManager.Instance.GetSpecSynergyList(SynergyType.WATER);
        //             // if (specSynergyDataList != null && specSynergyDataList.Count > 0)
        //             // {
        //             //     var filteredSynergyDataList = specSynergyDataList.Where(l => l.grade != 0).ToList();
        //             //     SceneUILayerManager.Instance.PushUILayerAsync<SynergyTooltipInGamePopup>((filteredSynergyDataList, 2, specSynergyDataList[1], specSynergyDataList[2])).Forget();
        //             // }
        //         }
        //     }

        //     _onSelected.Invoke(_statData);
        // }

        // _isShowLongPressFunc = false;
    }

    public void SetFocusCharacter(CharacterInfo spec)
    {
        bool isActiveFocus = spec != null;
        if (isActiveFocus)
        {
            var userCharacter = ServerDataManager.Instance.Character.GetCharacter(spec.id);
            _focusImageSpriteLoader.SetSprite(SpriteNameParser.GetCharacterInGamePortraitSprite(spec.prefab_id)).Forget();
            _focusText.text = userCharacter?.Level.ToString("n0") ?? "0";
        }
        else
        {
            _focusText.text = "0";
        }

        _focusObj.SetActive(isActiveFocus);
    }

    public void PlayDropFx()
    {
        _dropFxAnimation.Play();
    }

    // 롱탭 동작 시 실행할 함수
    public void OnLongPress()
    {
        Debug.Log("######## Long Pressed ########");

        InGameMain.GetInGameMain().ShowSKillTooltip(_statData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPressing = true;
        _pressTime = Time.time;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressing = false;

        InGameMain.GetInGameMain().CloseSkillTooltip();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_statData == null) return;

        _isOverBoard = false;
        _isCharacterSpawned = false;
        _scrollRectDragStarted = false;
        _isPressing = false;  // 드래그 시작하면 롱프레스 취소

        // ScrollRect 캐싱 (스크롤용)
        _cachedScrollRect = GetComponentInParent<ScrollRect>();

        // 일단 ScrollRect 드래그로 시작
        if (_cachedScrollRect != null)
        {
            ExecuteEvents.Execute(_cachedScrollRect.gameObject, eventData, ExecuteEvents.beginDragHandler);
            _scrollRectDragStarted = true;
        }

        // 튜토리얼: UI 캐릭터 배치 드래그 시작 알림
        if (TutorialActionCharacterPlacementUI.IsActive)
        {
            TutorialActionCharacterPlacementUI.OnDragStart(gameObject);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_statData == null) return;

        // 이미 보드 영역에 진입한 경우 → 캐릭터 배치 모드
        if (_isOverBoard)
        {
            HandleBoardDrag(eventData.position);
            return;
        }

        // 보드 영역(Slot 또는 Playground) 진입 체크
        bool isOverBoard = InGameTouchManager.Instance.IsPointOverBoard(eventData.position, out InGameTileView tileView);

        if (isOverBoard)
        {
            // 보드 영역 진입 → 배치 모드로 전환
            _isOverBoard = true;

            // ScrollRect 드래그 중단
            if (_cachedScrollRect != null && _scrollRectDragStarted)
            {
                ExecuteEvents.Execute(_cachedScrollRect.gameObject, eventData, ExecuteEvents.endDragHandler);
                _cachedScrollRect.StopMovement();
                _scrollRectDragStarted = false;
            }

            // 캐릭터 배치 처리 시작
            HandleBoardDrag(eventData.position);
        }
        else
        {
            // 아직 보드 영역이 아님 → ScrollRect 스크롤 계속
            if (_cachedScrollRect != null && _scrollRectDragStarted)
            {
                ExecuteEvents.Execute(_cachedScrollRect.gameObject, eventData, ExecuteEvents.dragHandler);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_statData == null) return;

        if (_isOverBoard && _isCharacterSpawned)
        {
            // 보드에서 드래그 종료
            InGameTouchManager.Instance.EndDragFromUI(eventData.position);
        }
        else if (_isOverBoard && !_isCharacterSpawned)
        {
            // 보드 영역에 진입했지만 캐릭터 생성 못함 - 튜토리얼 드래그 취소 알림
            if (TutorialActionCharacterPlacementUI.IsActive)
            {
                TutorialActionCharacterPlacementUI.OnDragCancel();
            }
        }
        else if (_scrollRectDragStarted && _cachedScrollRect != null)
        {
            // 스크롤 모드 - ScrollRect에 EndDrag 이벤트 전달
            ExecuteEvents.Execute(_cachedScrollRect.gameObject, eventData, ExecuteEvents.endDragHandler);
        }
        else
        {
            // 보드 영역에 도달하지 못하고 종료 - 튜토리얼 드래그 취소 알림
            if (TutorialActionCharacterPlacementUI.IsActive)
            {
                TutorialActionCharacterPlacementUI.OnDragCancel();
            }
        }

        ResetDragState();
        _cachedScrollRect = null;
    }

    private void HandleBoardDrag(Vector3 screenPosition)
    {
        if (_isCharacterSpawned)
        {
            // 이미 캐릭터가 생성되었으면 위치 업데이트
            InGameTouchManager.Instance.UpdateDragPosition(screenPosition);
        }
        else
        {
            // 보드 영역 체크 (Slot 또는 Playground)
            bool isOverBoard = InGameTouchManager.Instance.IsPointOverBoard(screenPosition, out InGameTileView tileView);

            if (isOverBoard)
            {
                if (tileView != null && tileView.AllianceType == AllianceType.Player)
                {
                    // Player 타일 위 - 해당 타일에 캐릭터 생성
                    SpawnCharacterOnBoard(tileView, screenPosition).Forget();
                }
                else
                {
                    // Playground 위 - 빈 타일을 찾아서 캐릭터 생성
                    SpawnCharacterOnPlayground(screenPosition).Forget();
                }
            }
        }
    }

    private async UniTaskVoid SpawnCharacterOnPlayground(Vector3 screenPosition)
    {
        if (_isCharacterSpawned) return;

        // 빈 타일 찾기
        var emptyTile = InGameObjectManager.Instance.InGameGrid.GetRecommandedTile(_statData.Spec);
        if (emptyTile == null) return;

        var tileView = emptyTile.View as InGameTileView;
        if (tileView == null) return;

        // await 전에 플래그 먼저 설정 (중복 호출 방지)
        _isCharacterSpawned = true;
        Debug.Log($"[InGameCharacterItem] SpawnCharacterOnPlayground 시작 - CharacterId: {_statData?.CharacterId}");

        // 배치 확정 시 호출될 콜백 (UI 리스트에서 제거)
        Action<CharacterStatData> onPlacementConfirmed = (stat) =>
        {
            _parentUI?.RemoveCharacterFromList(stat);
        };

        bool success = await InGameTouchManager.Instance.StartDragFromUI(_statData, tileView, onPlacementConfirmed);
        if (success)
        {
            Debug.Log($"[InGameCharacterItem] Playground에서 고스트 캐릭터 생성 성공");

            // 생성 후 바로 현재 드래그 위치로 이동
            InGameTouchManager.Instance.UpdateDragPosition(screenPosition);
        }
        else
        {
            Debug.Log($"[InGameCharacterItem] Playground에서 고스트 캐릭터 생성 실패 - 플래그 리셋");
            _isCharacterSpawned = false;

            if (TutorialActionCharacterPlacementUI.IsActive)
            {
                TutorialActionCharacterPlacementUI.OnDragCancel();
            }
        }
    }

    private async UniTaskVoid SpawnCharacterOnBoard(InGameTileView tileView, Vector3 screenPosition)
    {
        if (_isCharacterSpawned) return;

        // await 전에 플래그 먼저 설정 (중복 호출 방지)
        _isCharacterSpawned = true;
        Debug.Log($"[InGameCharacterItem] SpawnCharacterOnBoard 시작 - CharacterId: {_statData?.CharacterId}");

        // 배치 확정 시 호출될 콜백 (UI 리스트에서 제거)
        Action<CharacterStatData> onPlacementConfirmed = (stat) =>
        {
            _parentUI?.RemoveCharacterFromList(stat);
        };

        bool success = await InGameTouchManager.Instance.StartDragFromUI(_statData, tileView, onPlacementConfirmed);
        if (success)
        {
            Debug.Log($"[InGameCharacterItem] 고스트 캐릭터 생성 성공");
        }
        else
        {
            Debug.Log($"[InGameCharacterItem] 고스트 캐릭터 생성 실패 - 플래그 리셋");
            // 실패 시 플래그 리셋
            _isCharacterSpawned = false;

            // 튜토리얼: 드래그 취소 알림
            if (TutorialActionCharacterPlacementUI.IsActive)
            {
                TutorialActionCharacterPlacementUI.OnDragCancel();
            }
        }
    }

    private void ResetDragState()
    {
        _isOverBoard = false;
        _isCharacterSpawned = false;
        _scrollRectDragStarted = false;
    }
    public int GetDisplayLv()
    {
        return _statData.Level;
    }
    public void SetAlert()
    {
        if (_guideFx)
        {
            _guideFx.gameObject.SetActive(true);
            _guideFx.Play();
        }
    }
    

    void Update()
    {
        if (_isPressing && (Time.time - _pressTime) >= LONG_PRESS_DURATION)
        {
            _isShowLongPressFunc = true;
            _isPressing = false;
            OnLongPress();
        }
    }

    private async UniTaskVoid LoadCharacterIcon(int prefabId)
    {
        ReleaseCharacterIcon();
        ClearCharacterIconRoot();

        string address = $"SD/{prefabId}/UI_{prefabId}.prefab";
        _loadedCharacterIcon = await Addressables.InstantiateAsync(address, _characterIconRoot);
    }

    private void ClearCharacterIconRoot()
    {
        for (int i = _characterIconRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(_characterIconRoot.GetChild(i).gameObject);
        }
    }

    private void ReleaseCharacterIcon()
    {
        if (_loadedCharacterIcon != null)
        {
            Addressables.ReleaseInstance(_loadedCharacterIcon);
            _loadedCharacterIcon = null;
        }
    }

    private void OnDisable()
    {
        ReleaseCharacterIcon();
    }
}