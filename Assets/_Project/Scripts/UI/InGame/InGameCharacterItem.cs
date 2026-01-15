using System;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CharacterInfo = CookApps.AutoBattler.CharacterInfo;

public class InGameCharacterItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const float LONG_PRESS_DURATION = 0.5f;

    public CharacterStatData StatData => _statData;
    public bool IsFocusSlot => _focusObj.activeSelf;
    [SerializeField] private Transform _characterIconRoot;
    [SerializeField] private SpriteLoader _SynergyImageSpriteLoader;
    [SerializeField] private SpriteLoader _SynergyClassImageSpriteLoader;
    [SerializeField] private TextMeshProUGUI _lvText;
    [SerializeField] private GameObject _body;
    [SerializeField] private GameObject _emptySlotObj;
    [SerializeField] private GameObject _focusObj;
    [SerializeField] private SpriteLoader _focusImageSpriteLoader;
    [SerializeField] private Animation _dropFxAnimation;
    [SerializeField] private TextMeshProUGUI _focusText;
    [SerializeField] private ParticleSystem _guideFx;
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
    private const float DRAG_THRESHOLD = 10f;  // 방향 판정 임계값
    private bool _isDraggingVertical = false;  // 종 드래그 모드인지
    private bool _isDragDirectionDecided = false;  // 드래그 방향이 결정되었는지
    private bool _isCharacterSpawned = false;  // 보드에 캐릭터가 생성되었는지
    private Vector2 _dragStartPosition;

    public void SetData(InGameBottomUI parent, CharacterStatData characterStat, Action<CharacterStatData> onSelected)
    {
        _parentUI = parent;
        _statData = characterStat;
        bool isExsist = _statData != null;

        _body.SetActive(isExsist);
        _emptySlotObj.SetActive(!isExsist);
        if (_body.activeSelf)
        {
            LoadCharacterIcon(_statData.Spec.prefab_id).Forget();
            _SynergyImageSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(_statData.Spec.character_element_type)).Forget();
            _SynergyClassImageSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(_statData.Spec.character_stella_type)).Forget();
            _characterPositionTypeText.text = _statData.Spec.character_position_type.ToString();
            _lvText.text = $"{_statData.Level}";
            _attrText.text = $"{_statData.GetAttrValueCP().ToString("n0")}";
        }
        else
        {
            _lvText.text = $"0";
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
            _lvText.text = userCharacter?.Level.ToString("n0") ?? "0";
        }
        else
        {
            _focusText.text = "0";
            _lvText.text = "0";
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

        _dragStartPosition = eventData.position;
        _isDraggingVertical = false;
        _isDragDirectionDecided = false;
        _isCharacterSpawned = false;
        _isPressing = false;  // 드래그 시작하면 롱프레스 취소

        // 튜토리얼: UI 캐릭터 배치 드래그 시작 알림
        if (TutorialActionCharacterPlacementUI.IsActive)
        {
            TutorialActionCharacterPlacementUI.OnDragStart(gameObject);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_statData == null) return;

        // 드래그 방향이 아직 결정되지 않았으면 판정
        if (!_isDragDirectionDecided)
        {
            Vector2 delta = eventData.position - _dragStartPosition;

            if (delta.magnitude > DRAG_THRESHOLD)
            {
                // 종(세로) 방향이 더 크면 캐릭터 배치 모드
                _isDraggingVertical = Mathf.Abs(delta.y) > Mathf.Abs(delta.x);
                _isDragDirectionDecided = true;

                if (_isDraggingVertical)
                {
                    // ScrollRect 드래그 중단
                    var scrollRect = GetComponentInParent<ScrollRect>();
                    if (scrollRect != null)
                    {
                        scrollRect.StopMovement();
                        scrollRect.enabled = false;
                    }
                }
            }
        }

        // 종 드래그 모드일 때만 보드 배치 처리
        if (_isDraggingVertical)
        {
            HandleVerticalDrag(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_statData == null) return;

        // ScrollRect 다시 활성화
        var scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.enabled = true;
        }

        if (_isDraggingVertical && _isCharacterSpawned)
        {
            // 보드에서 드래그 종료
            InGameTouchManager.Instance.EndDragFromUI(eventData.position);
        }
        else if (_isDraggingVertical && !_isCharacterSpawned)
        {
            // 보드에 도달하지 못하고 드래그 종료 - 튜토리얼 드래그 취소 알림
            if (TutorialActionCharacterPlacementUI.IsActive)
            {
                TutorialActionCharacterPlacementUI.OnDragCancel();
            }
        }

        ResetDragState();
    }

    private void HandleVerticalDrag(Vector3 screenPosition)
    {
        if (_isCharacterSpawned)
        {
            // 이미 캐릭터가 생성되었으면 위치 업데이트
            InGameTouchManager.Instance.UpdateDragPosition(screenPosition);
        }
        else
        {
            // 보드 타일 위에 있는지 체크
            var tileView = InGameTouchManager.Instance.GetTileViewFromScreenPosition(screenPosition);
            if (tileView != null && tileView.AllianceType == AllianceType.Player)
            {
                // 보드에 도달 - 캐릭터 생성
                SpawnCharacterOnBoard(tileView, screenPosition).Forget();
            }
        }
    }

    private async UniTaskVoid SpawnCharacterOnBoard(InGameTileView tileView, Vector3 screenPosition)
    {
        if (_isCharacterSpawned) return;

        // await 전에 플래그 먼저 설정 (중복 호출 방지)
        _isCharacterSpawned = true;
        Debug.Log($"[InGameCharacterItem] SpawnCharacterOnBoard 시작 - CharacterId: {_statData?.CharacterId}");

        bool success = await InGameTouchManager.Instance.StartDragFromUI(_statData, tileView);
        if (success)
        {
            Debug.Log($"[InGameCharacterItem] 캐릭터 생성 성공");
            // 리스트에서 캐릭터 제거
            _parentUI?.RemoveCharacterFromList(_statData);

            // 튜토리얼: 생성된 캐릭터를 마스크 타겟으로 설정
            if (TutorialActionCharacterPlacementUI.IsActive)
            {
                var tile = InGameObjectManager.Instance.GetInGameTile(tileView.ID);
                if (tile?.OccupiedCharacter?.GetCharacterView() != null)
                {
                    TutorialActionCharacterPlacementUI.UpdateMaskTarget(tile.OccupiedCharacter.GetCharacterView().gameObject);
                }
            }
        }
        else
        {
            Debug.Log($"[InGameCharacterItem] 캐릭터 생성 실패 - 플래그 리셋");
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
        _isDraggingVertical = false;
        _isDragDirectionDecided = false;
        _isCharacterSpawned = false;
    }

    public int GetDisplayLv()
    {
        int level;
        if (int.TryParse(_lvText.text, out level))
        {
            return level;
        }
        else
        {
            return 0;
        }
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