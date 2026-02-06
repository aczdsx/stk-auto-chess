using CookApps.TeamBattle;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class ElpisCoreStatItem : CachedMonoBehaviour
{
    [SerializeField] private TMP_Text statTitleText;
    [SerializeField] private TMP_Text statValueText;
    [SerializeField] private SpriteLoader statIconSpriteLoader;

    public void SetSprite(string titleKey)
    {
        if (titleKey.Equals("none"))
            gameObject.SetActive(false);
        
        statIconSpriteLoader.SetSprite(GetSpriteName(titleKey)).Forget();
    }

    public void Set(string titleKey, string valueKey)
    {
        CachedGo.SetActive(!valueKey.Equals("0"));
        
        statTitleText.text = titleKey;
        statValueText.text = ZString.Format("+ {0}", valueKey);
    }

    private string GetSpriteName(string titleKey)
    {
        if (titleKey.Contains("ATTACK") || titleKey.Contains("DAMAGE") || titleKey.Contains("ATK"))
        {
            return "Icon_Damage_74";
        }
        else if(titleKey.Contains("DEFENSE") || titleKey.Contains("DEF"))
        {
            return "Icon_Defense_74";
        }
        else if (titleKey.Contains("HEALTH"))
        {
            return "Icon_Power_74";
        }
        
        return "none";
    }
}