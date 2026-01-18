using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 캐릭터 드래그 시 이펙트 처리
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSupernovaDragEffect : EffectCodeCharacterBase
{
    public const int CodeId = (int)EffectCodeNameType.BATTLE_ITEM_SUPERNOVA_DRAGGING_EFFECT;

    private List<InGameVfx> _dragVfxList = new();
    private Dictionary<CharacterController, InGameVfx> _dragVfxByCharacter = new();
    private bool _isVfxCreated = false; // VFX 생성 여부 플래그

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
    }

    public override void OnCharacterDragging()
    {
        base.OnCharacterDragging();

        // 이미 VFX가 생성되었으면 중복 호출 방지
        if (_isVfxCreated)
        {
            // VFX가 파괴되었는지 확인하고 복구
            ValidateAndRestoreVfx();
            return;
        }

        // 슈퍼노바 배틀 아이템인지 확인
        if (owner != null && owner.SpecCharacter.character_type == CharacterType.BATTLEITEM)
        {
            int battleItemId = owner.SpecCharacter.prefab_id;
            if (battleItemId >= (int)EffectCodeNameType.BATTLE_ITEM_SUPERNOVA && 
                battleItemId < (int)EffectCodeNameType.BATTLE_ITEM_SUPERNOVA + 3)
            {
                // 슈퍼노바 배틀 아이템 드래그 시 모든 슈퍼노바 캐릭터들 위에 VFX 생성
                CreateSupernovaDragVfx();
                _isVfxCreated = true;
            }
        }
    }

    /// <summary>
    /// VFX가 파괴되었는지 확인하고 복구
    /// </summary>
    private void ValidateAndRestoreVfx()
    {
        // 딕셔너리에서 파괴된 VFX 제거
        var keysToRemove = new List<CharacterController>();
        foreach (var kvp in _dragVfxByCharacter)
        {
            if (kvp.Value == null || kvp.Value.CachedGo == null)
            {
                keysToRemove.Add(kvp.Key);
                _dragVfxList.Remove(kvp.Value);
            }
        }

        foreach (var key in keysToRemove)
        {
            _dragVfxByCharacter.Remove(key);
        }

        // 모든 VFX가 파괴되었으면 플래그 리셋
        if (_dragVfxList.Count == 0)
        {
            _isVfxCreated = false;
        }
    }

    /// <summary>
    /// 슈퍼노바 배틀 아이템 드래그 시 모든 슈퍼노바 캐릭터들 위에 VFX 생성
    /// </summary>
    private void CreateSupernovaDragVfx()
    {
        var characterList = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player);
        foreach (var character in characterList)
        {
            if (character != null && character.SpecCharacter.character_stella_type == SynergyType.SUPERNOVA)
            {
                CreateVfxOnCharacter(character);
            }
        }
    }

    /// <summary>
    /// 캐릭터 위에 드래그 VFX 생성
    /// </summary>
    private void CreateVfxOnCharacter(CharacterController targetCharacter)
    {
        if (targetCharacter == null)
            return;

        // 이미 해당 캐릭터에 VFX가 있으면 생성하지 않음
        if (_dragVfxByCharacter.ContainsKey(targetCharacter))
        {
            var existingVfx = _dragVfxByCharacter[targetCharacter];
            if (existingVfx != null && existingVfx.CachedGo != null)
            {
                return; // 이미 VFX가 존재함
            }
            else
            {
                // VFX가 파괴되었으면 딕셔너리에서 제거
                _dragVfxByCharacter.Remove(targetCharacter);
                _dragVfxList.Remove(existingVfx);
            }
        }

        // 슈퍼노바 캐릭터 위에 VFX 생성
        var vfx = InGameVfxManager.Instance.AddInGameVfx(
            InGameVfxNameType.fx_common_cast_supernova_02,
            targetCharacter.SkillRootTransformFollowable);

        if (vfx != null)
        {
            _dragVfxList.Add(vfx);
            _dragVfxByCharacter[targetCharacter] = vfx;
        }
    }

    /// <summary>
    /// 드래그 종료 시 생성된 VFX 제거 (외부에서 호출 가능)
    /// </summary>
    public void RemoveDragVfx()
    {
        foreach (var vfx in _dragVfxList)
        {
            if (vfx != null)
            {
                if (vfx.CachedGo != null)
                {
                    InGameVfxManager.Instance.RemoveInGameVfx(vfx);
                }
                else
                {
                    vfx.Remove();
                }
            }
        }
        _dragVfxList.Clear();
        _dragVfxByCharacter.Clear();
        _isVfxCreated = false; // 플래그 리셋
    }

    public override void OnCharacterDraggingEnd()
    {
        base.OnCharacterDraggingEnd();
        
        // 드래그 종료 시 모든 VFX 제거
        RemoveDragVfx();
    }

    public override void OnPreRemoved()
    {
        // EffectCode 제거 시 모든 VFX 제거
        RemoveDragVfx();
        base.OnPreRemoved();
    }
}
