using System;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using LitMotion;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

[UseEffectCodeIds(CodeId)]
public partial class EffectCodeCrowdControlKnockback : EffectCodeCharacterBase
{
    public const int CodeId = (int) EffectCodeNameType.CC_KNOCKBACK;
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
    private Ease _ease = Ease.OutExpo;

    private event Action<InGameTile> OnKnockbackEnd = null;
    public void SetOnKnockbackEndHandler(Action<InGameTile> onKnockbackEnd)
    {
        OnKnockbackEnd = onKnockbackEnd;
    }

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        var attacker = source as CharacterController;
        updatePendingTime = 0f;

        duration = codeInfo.GetCodeStatToFloat(0);
        height = codeInfo.GetCodeStatToFloat(1);
        tileID = codeInfo.GetCodeStatToInt(2);
        if (codeInfo.HasCodeStat(3))
        {
            _ease = (Ease)codeInfo.GetCodeStatToInt(3);
        }

        startY = owner.ViewPosition3D.y;

        owner.AddCrowdControl(CrowdControlType.KnockBack);
        elapsedTime = 0;

        // if (owner.SpecCharacter.is_knock_back)
        if (true)
        {
            _inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);

            float distance = Vector3.Distance(_inGameTile.View.Position, owner.Position3D);

            duration += distance * 0.1f;

            var halfDuration = duration * 0.5f;
            upFactor = (startY - height) / (halfDuration * halfDuration);
            downFactor = -height / (halfDuration * halfDuration);

            owner.ChangeOccupiedTile(_inGameTile);
            LMotion.Create(
                owner.Position3D,
                _inGameTile.View.Position,
                duration)
                .WithEase(_ease)
                .WithOnComplete(() =>
                {
                    if (owner != null)
                    {
                        owner.AddNextState<CharacterStateIdle>();
                        owner.Position3D = _inGameTile.View.Position;
                        OnKnockbackEnd?.Invoke(_inGameTile);
                    }
                })
                .Bind(value =>
                {
                    if (owner != null)
                        owner.Position3D = value;
                });
        }
        else
        {
            RemoveFromContainer();
        }
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
        elapsedTime = 0;
    }

    public override void OnUpdate(float dt)
    {
        // if (!owner.SpecCharacter.is_knock_back)
        //     return;

        elapsedTime += dt;
        if (elapsedTime > duration)
        {
            var pos = owner.ViewPosition3D;
            pos.y = 0;
            owner.ViewPosition3D = pos;


            owner.Position3D = _inGameTile.View.Position;
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
