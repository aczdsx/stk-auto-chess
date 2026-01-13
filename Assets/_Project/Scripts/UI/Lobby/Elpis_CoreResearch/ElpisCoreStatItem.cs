using CookApps.TeamBattle;
using Cysharp.Text;
using TMPro;
using UnityEngine;

public class ElpisCoreStatItem : CachedMonoBehaviour
{
    [SerializeField] private TMP_Text statTitleText;
    [SerializeField] private TMP_Text statValueText;

    public void Set(string titleKey, string valueKey)
    {
        CachedGo.SetActive(!valueKey.Equals("0"));
        
        //TODO : localization
        statTitleText.text = titleKey;
        statValueText.text = ZString.Format("+ {0}", valueKey);
    }
}