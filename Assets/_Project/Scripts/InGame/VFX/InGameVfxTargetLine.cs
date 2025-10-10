using CookApps.Obfuscator;
using UnityEngine;

namespace CookApps.BattleSystem
{
    public class InGameVfxTargetLine : InGameVfx
    {
        public TargetLineRenderer TargetLine => _targetLine;
        [SerializeField] private TargetLineRenderer _targetLine;

        public void SetActiveObject(bool isActive)
        {
            this.CachedGo.SetActive(isActive);
        }
    }
}
