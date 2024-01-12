//
// using Coffee.UIEffects;
// using UnityEngine;
// using UnityEngine.UI;
//
//
// [RequireComponent(typeof(UIEffect))]
// public class SimpleUIEffectSwapper : MonoBehaviour, ISimpleSwapper
// {
//     [SerializeField] private UIEffect effect;
//     [SerializeField] private SimpleSwapType currentType;
//
//     private void Awake()
//     {
//         if (effect == null)
//             effect = GetComponent<UIEffect>();
//
//         effect.effectMode = (EffectMode)(int)currentType;
//     }
//
//     public void Swap(SimpleSwapType swapType)
//     {
//         if (currentType == swapType)
//             return;
//
//         if (swapType is < SimpleSwapType.Normal or > SimpleSwapType.Disabled)
//             return;
//
//         currentType = swapType;
//         effect.effectMode = (EffectMode)(int)currentType;
//     }
// }
