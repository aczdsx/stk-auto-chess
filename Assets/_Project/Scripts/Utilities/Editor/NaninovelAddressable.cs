using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class NaninovelAddressableSetting
{
    [MenuItem("Tools/Naninovel/Setting Addressable")]
    public static void SettingNaninovelAddressable()
    {
        // Assets/StellaKnights/Dialogue/Localization/en/Scripts/0-0(Closing).nani
        // Naninovel/Localization/en/Scripts/0-0(Closing)
        
        var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
        
        var localGroupAsset =
            AssetDatabase.LoadAssetAtPath<AddressableAssetGroup>("Assets/AddressableAssetsData/AssetGroups/LocalGroup.asset");

        // base script
        var sourceScripts = Directory.GetFiles($"{Application.dataPath}/StellaKnights/Dialogue/Scripts/", "*.nani");
        foreach (var d in sourceScripts)
        {
            var assetPath = d.Substring(d.IndexOf("/Assets/") + 1);
            var address = assetPath.Substring(assetPath.IndexOf("/Scripts/") + 1);
            
            
            address = address.Remove(address.Length - 5).Insert(0, "Naninovel/");
            try
            {   
                var findData = localGroupAsset.entries.First(a => a.AssetPath == assetPath);
            }
            catch (InvalidOperationException e)
            {
                var entry = addressableSettings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), localGroupAsset);
                entry.SetAddress(address, false);
                entry.SetLabel("Naninovel", true);
             
                Debug.Log($"{assetPath} added.");
                
                AssetDatabase.SaveAssets();
            }
        }
        
        // localization script
        var localizeScripts = Directory.GetFiles($"{Application.dataPath}/StellaKnights/Dialogue/Localization/", "*.nani", SearchOption.AllDirectories);
        foreach (var d in localizeScripts)
        {
            var assetPath = d.Substring(d.IndexOf("/Assets/") + 1).Replace('\\', '/');

            var address = assetPath.Replace("Assets/StellaKnights/Dialogue/", "Naninovel/");
            address = address.Remove(address.Length - 5);
            try
            {   
                var findData = localGroupAsset.entries.First(a => a.AssetPath == assetPath);
            }
            catch (InvalidOperationException e)
            {
                var entry = addressableSettings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), localGroupAsset);
                entry.SetAddress(address, false);
                entry.SetLabel("Naninovel", true);
             
                Debug.Log($"{assetPath} added.");
                
                AssetDatabase.SaveAssets();
            }
        }
    }
}
