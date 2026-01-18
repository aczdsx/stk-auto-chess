using UnityEngine;

public class TrialVfx : MonoBehaviour
{
    public void PlaySfx(string name)
    {
        SoundManager.Instance.PlaySFX(name);
    }
}
