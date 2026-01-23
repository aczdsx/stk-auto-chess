using System.Linq;
using System.Threading.Tasks;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 라플라스마녀 스페셜2
/// 대상: 맨허튼거리 2 5x5
// 효과: 범위 내에 {0}%만큼 피해를 입힌다.
/// </summary>
[UseEffectCodeIds(280109102)]
public partial class EffectCodeSkill280109102 : EffectCodeCharacterBase
{
    private bool _isReadyToActivate;
    private SkillActive _specSkill;
    private float _damageRate;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 2;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = 0;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = 0;
    }

    public override void OnUpdate(float dt)
    {
        if (!IsSkillActivated)
        {
            return;
        }

        // target check
        if (false)
        {
            owner.AddNextState<CharacterStateIdle>();
            CoolTimeElapsedTime = CoolTimeDurationTime;
        }
    }

    public override void OnCooltime(float dt)
    {
        if (_isReadyToActivate || IsSkillActivated)
            return;
        CoolTimeElapsedTime += dt;
        if (CoolTimeElapsedTime >= CoolTimeDurationTime)
        {
            _isReadyToActivate = true;
        }
    }

    public override bool IsReadyToActivate()
    {
        return _isReadyToActivate;
    }

    public override void Activate()
    {
        base.Activate();

        _isReadyToActivate = false;
        IsSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;
            
        var targetCharacterList = InGameObjectManager.Instance.GetCharacterListSortedByDistanceDescending(owner, false);
        if (targetCharacterList.Count == 0)
            return;

        CharacterController targetCharacter = null;

        foreach (var player in targetCharacterList)
        {
            if (player.IsAlive && player.CurrentTile != null && player.CharacterId == CookApps.AutoBattler.Prologue.PrologueID.프롤로그아트레시아ID)
            {
                targetCharacter = player;
                break;
            }
        }

        if (targetCharacter == null)
            return;

        DelayedDamageTime(targetCharacter).Forget();
    }

    private async UniTask DelayedDamageTime(CharacterController artresiaCharacterControler)
    {
        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByManhattanDistanceInRange(artresiaCharacterControler.CurrentTile, 5);

        foreach (var tile in inGameTiles)
            InGameVfxManager.Instance.AddInGameTileFx(SynergyType.LIGHTNING, tile);
        
        var offsetedTile = artresiaCharacterControler.CurrentTile.Int2Index + new int2(0, -1);
        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], InGameObjectManager.Instance.InGameGrid.GetTile(offsetedTile).View.CachedTr.position);
        
        await UniTask.Delay(1250);

        foreach (var tile in inGameTiles)
        {
            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                var damageValue = owner.SpecCharacter.atk_type is AtkType.AD ? owner.AD : owner.AP;
                if(tile.OccupiedCharacter.CharacterId == CookApps.AutoBattler.Prologue.PrologueID.프롤로그아트레시아ID)
                {
                    double artdamage = 0;
                    if(tile.OccupiedCharacter.CurrentHp > 0)
                    {
                        artdamage = tile.OccupiedCharacter.CurrentHp - 100;
                    }
                    CharacterController.DamageInfo damageInfo = CharacterController.DamageInfo.Create(
                        damageAmount: artdamage,
                        source: 0,
                        attackerType: AttackerType.CHARCTER,
                        isAD: owner.SpecCharacter.atk_type is AtkType.AD ? true : false,
                        isCritical: false,
                        isDoubleCritical: false
                    );
                    tile.OccupiedCharacter.GetDamaged(damageInfo, owner);
                }
                else
                {
                    var damage = owner.CalculateDamageAmount(9999, 0, tile.OccupiedCharacter, codeId, true);
                    tile.OccupiedCharacter.GetDamaged(damage, owner);
                }
            }
        }

        IsSkillActivated = false;
    } 

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }

    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }
}//280109001
