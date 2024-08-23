using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrialVfx : MonoBehaviour
{
    public void PlaySfx(string name)
    {
        SoundManager.Instance.PlaySFX(name);
    }
}
