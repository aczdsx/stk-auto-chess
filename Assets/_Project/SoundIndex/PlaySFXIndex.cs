using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySFXIndex : MonoBehaviour
{
    [SerializeField] private SFXIndex sfxIndex = SFXIndex.Null;
    // Start is called before the first frame update
    void Start()
    {
        if (sfxIndex != SFXIndex.Null)
        {
            // SoundManager.Instance.PlaySFX(sfxIndex);
        }
    }
}
