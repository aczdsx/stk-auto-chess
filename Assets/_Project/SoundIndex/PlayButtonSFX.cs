using UnityEngine;
using UnityEngine.UI;

public class PlayButtonSFX : MonoBehaviour
{
    [SerializeField] private SFXIndex sfxIndex = SFXIndex.ui_btn_touch;
    
    private void Start()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnClickSound);
        }
    }

    public void OnClickSound()
    {
        // if(SoundManager.Loaded)
        //     SoundManager.Instance.PlaySFX(sfxIndex);
    }
}
