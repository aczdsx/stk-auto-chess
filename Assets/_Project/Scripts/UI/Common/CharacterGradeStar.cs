using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class CharacterGradeStar : MonoBehaviour
    {
        [SerializeField] private GameObject _onObject;
        [SerializeField] private GameObject _offObject;

        public void SetStar(bool isOn)
        {
            _onObject.SetActive(isOn);
            _offObject.SetActive(!isOn);
        }
    }
}
