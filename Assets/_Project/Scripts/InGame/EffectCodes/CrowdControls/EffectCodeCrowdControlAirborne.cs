using CookApps.Obfuscator;
using CookApps.TeamBattle.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.TeamBattle.BattleSystem.CharacterController;

public class EffectCodeCrowdControlAirborne : EffectCodeCharacterBase
{
    public override bool IsRemoveWithSource => false;
    public override EffectCodeType Type => EffectCodeType.CrowdControl;

    // const data
    private ObfuscatorFloat duration;
    private ObfuscatorFloat height;
    private ObfuscatorFloat startY;
    private Vector2 knockBackSpeed;

    // runtime data
    private ObfuscatorFloat elapsedTime;
    private bool isKnockBacking;
    private bool isGoingUp;
    private ObfuscatorFloat upFactor;
    private ObfuscatorFloat downFactor;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        var attacker = source as CharacterController;
        updatePendingTime = 0f;

        duration = codeInfo.GetCodeStatToFloat(0);
        height = codeInfo.GetCodeStatToFloat(1);
        startY = owner.ViewPosition3D.y;

        var halfDuration = duration * 0.5f;
        upFactor = (startY - height) / (halfDuration * halfDuration);
        downFactor = -height / (halfDuration * halfDuration);

        if (codeInfo.HasCodeStat(2))
        {
            var sign = 1;
            if (attacker != null)
            {
                sign = attacker.Position.x < owner.Position.x ? 1 : -1;
            }
            knockBackSpeed = new Vector2(sign * codeInfo.GetCodeStatToFloat(2), codeInfo.GetCodeStatToFloat(3));
            owner.AddCrowdControl(CrowdControlType.KnockBack);
            isKnockBacking = true;
        }
        else
        {
            knockBackSpeed = Vector2.zero;
            owner.AddCrowdControl(CrowdControlType.Airborne);
            isKnockBacking = false;
        }

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

        startY = owner.ViewPosition3D.y;

        var halfDuration = duration * 0.5f;
        upFactor = (startY - height) / (halfDuration * halfDuration);
        downFactor = -height / (halfDuration * halfDuration);

        if (codeInfo.HasCodeStat(2))
        {
            var sign = 1;
            if (attacker != null)
            {
                sign = attacker.Position.x < owner.Position.x ? 1 : -1;
            }
            knockBackSpeed = new Vector2(sign * codeInfo.GetCodeStatToFloat(2), codeInfo.GetCodeStatToFloat(3));
            owner.AddCrowdControl(CrowdControlType.KnockBack);
            isKnockBacking = true;
        }
        else
        {
            knockBackSpeed = Vector2.zero;
            owner.AddCrowdControl(CrowdControlType.Airborne);
            isKnockBacking = false;
        }

        elapsedTime = 0;
    }

    public override void OnUpdate(float dt)
    {
        elapsedTime += dt;
        if (elapsedTime > duration)
        {
            RemoveFromContainer();
            owner.RemoveCrowdControl(CrowdControlType.KnockBack);
            var pos = owner.ViewPosition3D;
            pos.y = 0;
            owner.ViewPosition3D = pos;
            return;
        }

        // 넉백 위치 계산
        if (isKnockBacking)
        {
            var pos = owner.Position;
            pos += knockBackSpeed * dt;
            owner.Position = pos;

            // 저항 계산을 해서 자연스럽게 밀리도록
            knockBackSpeed -= knockBackSpeed.normalized * (-0.5f * knockBackSpeed.sqrMagnitude * 0.1f * dt);
        }

        // 에어본 위치 계산
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
}
