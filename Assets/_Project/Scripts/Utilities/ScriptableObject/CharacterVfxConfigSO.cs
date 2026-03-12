using UnityEngine;

namespace CookApps.AutoBattler
{
    [CreateAssetMenu(fileName = "CharacterVfxConfig", menuName = "AutoChess/Character Vfx Config")]
    public class CharacterVfxConfigSO : ScriptableObject
    {
        [Header("Source")]
        [SerializeField] private GameObject _sourcePrefab;

        [Header("VFX")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private SkillViewData[] _skillEffectPrefabs;

        public GameObject ProjectilePrefab => _projectilePrefab;
        public SkillViewData[] SkillEffectPrefabs => _skillEffectPrefabs;
    }
}
