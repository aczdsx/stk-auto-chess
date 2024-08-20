using CookApps.Obfuscator;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using PrimeTween;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

[UseEffectCodeIds(CodeId)]
public class EffectCodeCrowdControlKnockback : EffectCodeCharacterBase
{
    public const int CodeId = (int) EffectCodeNameType.KNOCKBACK;
    public override bool IsRemoveWithSource => false;
    public override EffectCodeType Type => EffectCodeType.CrowdControl;

    // const data
    private ObfuscatorFloat duration;
    private ObfuscatorFloat height;
    private ObfuscatorFloat startY;
    private ObfuscatorInt tileID;

    // runtime data
    private ObfuscatorFloat elapsedTime;
    private bool isGoingUp;
    private ObfuscatorFloat upFactor;
    private ObfuscatorFloat downFactor;

    private InGameTile _inGameTile;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        var attacker = source as CharacterController;
        updatePendingTime = 0f;

        duration = codeInfo.GetCodeStatToFloat(0);
        height = codeInfo.GetCodeStatToFloat(1);
        tileID = codeInfo.GetCodeStatToInt(2);

        startY = owner.ViewPosition3D.y;
        _inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);

        float distance = Vector3.Distance(owner.Position3D, _inGameTile.View.Position);
        if (distance > 0)
        {
            distance = Mathf.Min(1.0f, distance);

            var halfDuration = duration * distance * 0.5f;
            upFactor = (startY - height) / (halfDuration * halfDuration);
            downFactor = -height / (halfDuration * halfDuration);

            owner.AddCrowdControl(CrowdControlType.KnockBack);
            elapsedTime = 0;

            if (owner.SpecCharacter.is_knock_back)
            {
                owner.ChangeOccupiedTile(_inGameTile);
                Tween.Custom(
                    owner.Position3D,
                    _inGameTile.View.Position,
                    duration * distance,
                    (Vector3 value) =>
                    {
                        if (owner != null)
                            owner.Position3D = value;
                    }, ease: Ease.InCirc).OnComplete(this, target =>
                    {
                        if (owner != null)
                            owner.AddNextState<CharacterStateIdle>();
                    }
                );
            }
        }
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        var attacker = source as CharacterController;
        updatePendingTime = 0f;

        duration = codeInfo.GetCodeStatToFloat(0);
        height = codeInfo.GetCodeStatToFloat(1);
        tileID = codeInfo.GetCodeStatToInt(2);

        startY = owner.ViewPosition3D.y;
        _inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);

        float distance = Vector3.Distance(owner.Position3D, _inGameTile.View.Position);
        if (distance > 0)
        {
            distance = Mathf.Min(1.0f, distance);

            var halfDuration = duration * distance * 0.5f;
            upFactor = (startY - height) / (halfDuration * halfDuration);
            downFactor = -height / (halfDuration * halfDuration);

            owner.AddCrowdControl(CrowdControlType.KnockBack);
            elapsedTime = 0;

            if (owner.SpecCharacter.is_knock_back)
            {
                owner.ChangeOccupiedTile(_inGameTile);
                Tween.Custom(
                    owner.Position3D,
                    _inGameTile.View.Position,
                    duration * distance,
                    (Vector3 value) =>
                    {
                        if (owner != null)
                            owner.Position3D = value;
                    }, ease: Ease.InCirc).OnComplete(this, target =>
                    {
                        if (owner != null)
                            owner.AddNextState<CharacterStateIdle>();
                    }
                );
            }
        }
    }

    public override void OnUpdate(float dt)
    {
        elapsedTime += dt;
        if (elapsedTime > duration)
        {
            var pos = owner.ViewPosition3D;
            pos.y = 0;
            owner.ViewPosition3D = pos;
            RemoveFromContainer();
            return;
        }

        var x = elapsedTime - duration * 0.5f;
        if (x < 0)
        {
            isGoingUp = true;
            var pos = owner.ViewPosition3D;
            pos.y = upFactor * x * x + height;
            owner.ViewPosition3D = pos;
        }
        else
        {
            isGoingUp = false;
            var pos = owner.ViewPosition3D;
            pos.y = downFactor * x * x + height;
            owner.ViewPosition3D = pos;
        }
    }

    public override void OnPreRemoved()
    {
        owner.RemoveCrowdControl(CrowdControlType.KnockBack);
        base.OnPreRemoved();
    }
}
