using System.Collections;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleCallbackByOne : MonoBehaviour
    {
        [SerializeField] private int idx;
        private void OnEnable()
        {
            StartCoroutine(WaitPlay());
        }

        private IEnumerator WaitPlay()
        {
            var component = GetComponent<ParticleSystem>();
            var main = component.main;
            yield return new WaitForSeconds(main.startDelayMultiplier + 0.2f);
            OnPlay();
        }

        private void OnPlay()
        {
            Debug.Log($"ParticleSystem Play : {gameObject.name}, {transform.position}" );
            GachaFxByOne.Instance.ChangeImageCard(idx);
        }
    }
}

