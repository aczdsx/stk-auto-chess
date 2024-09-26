using CookApps.Obfuscator;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using PrimeTween;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

[UseEffectCodeIds(CodeId)]
public partial class EffectCodeCrowdControlAirborne : EffectCodeCharacterBase
{
    public const int CodeId = (int) EffectCodeNameType.AIRBORNE;
    public override bool IsRemoveWithSource => false;
    public override EffectCodeType Type => EffectCodeType.CrowdControl;

    // const data
    private ObfuscatorFloat duration;
    private ObfuscatorFloat height;
    private ObfuscatorInt tileID;

    // runtime data
    private ObfuscatorFloat elapsedTime;
    private bool isGoingUp;
    private ObfuscatorFloat upFactor;
    private ObfuscatorFloat downFactor;

    private InGameTile _inGameTile;

    private const float _moveTime = 0.2f;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        var attacker = source as CharacterController;
        updatePendingTime = 0f;

        duration = codeInfo.GetCodeStatToFloat(0);
        height = codeInfo.GetCodeStatToFloat(1);
        tileID = codeInfo.GetCodeStatToInt(2);

        owner.AddCrowdControl(CrowdControlType.Airborne);
        elapsedTime = 0;
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        this.source = source;
        var attacker = source as CharacterController;
        duration = codeInfo.GetCodeStatToFloat(0);

        if (isGoingUp)
        {
            float h = codeInfo.GetCodeStatToFloat(1);
            // 기존 높이가 더 크면 기존높이에서 약간 더 높이 띄움
            if (h < height)
            {
                height = height * 1.25f;
            }
            // 이번에 띄우는 높이가 많이 차이 안나면 약간 더 높이 띄움
            else if (h < height * 1.1f)
            {
                height = h * 1.1f;
            }
            else
            {
                height = h;
            }
        }
        else
        {
            height = codeInfo.GetCodeStatToFloat(1);
            // 내려가고 있을 때 현재 캐릭터 위치가 띄울 높이보다 높으면 그 위치에서 약간 더 높이 띄움
            if (owner.ViewPosition3D.y > height)
            {
                height = owner.ViewPosition3D.y * 1.25f;
            }
        }

        elapsedTime = 0;
    }

    public override void OnUpdate(float dt)
    {
        elapsedTime += dt;
        if (elapsedTime > duration + (_moveTime * 2))
        {
            var pos = owner.ViewPosition3D;
            pos.y = 0;
            owner.ViewPosition3D = pos;
            RemoveFromContainer();
            return;
        }

        if (elapsedTime <= _moveTime)
        {
            var pos = owner.ViewPosition3D;
            pos.y = height * (elapsedTime / _moveTime);
            owner.ViewPosition3D = pos;
        }
        else if (elapsedTime <= duration + _moveTime)
        {
            var pos = owner.ViewPosition3D;
            pos.y = height + Mathf.Sin(elapsedTime * Mathf.PI * 3) * 0.1f;
            owner.ViewPosition3D = pos;
        }
        else
        {
            var pos = owner.ViewPosition3D;
            pos.y = height * (1 - ((elapsedTime - duration - _moveTime) / _moveTime));
            owner.ViewPosition3D = pos;
        }
    }

    public override void OnPreRemoved()
    {
        owner.RemoveCrowdControl(CrowdControlType.Airborne);
        base.OnPreRemoved();
    }
}
