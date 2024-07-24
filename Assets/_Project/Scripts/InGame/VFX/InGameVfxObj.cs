using System;
using CookApps.AutoBattler;
using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.BattleSystem
{
    public class InGameVfxObj : InGameVfx
    {
        [SerializeField] private Transform _bodyTransform;
        [SerializeField] private Material _material;
        
        [SerializeField] private Texture2D _frontTexture;
        [SerializeField] private Texture2D _backTexture;

        [SerializeField] private GameObject _lightFx;
        
        public void SetTexture(bool isFront, bool isRight, bool isFx)
        {
            Vector3 scale = _bodyTransform.localScale;
            scale.x = isRight ? 1 : -1;
            _bodyTransform.localScale = scale;
            _lightFx.SetActive(isFx);
        }
    }
}
