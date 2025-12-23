using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
using UnityEngine.AddressableAssets;

namespace CookApps.Editor
{

[InitializeOnLoad]
public static class AddressableAssetMonitor
{
    static AddressableAssetMonitor()
    {
        // AddressableAssetSettings을 가져옵니다.
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

        if (settings != null)
        {
            // 그룹에 대한 변경 이벤트를 등록합니다.
            settings.OnModification += OnAddressableAssetModified;
        }
    }

    // AddressableAssetSettings 변경 시 실행되는 메서드
    private static void OnAddressableAssetModified(AddressableAssetSettings settings, AddressableAssetSettings.ModificationEvent evt, object obj)
    {
        AddressableAssetEntry entry = obj as AddressableAssetEntry;
        if (entry != null)
        {
            Debug.Log($"Addressable 에셋 변경됨: {entry.address}");

            if (!entry.address.Contains("SpriteAtlas"))
                return;

            SpriteManagerImportProcessor.CheckAndUpdateRefs(entry.AssetPath);
        }
    }
}

public class SpriteManagerImportProcessor : AssetPostprocessor
{
    private static SpriteManagerScriptableObject cachedSpriteManager;
    private static ProjectFolderSettings projectFolderSettings;

    private static ProjectFolderSettings ProjectFolders => projectFolderSettings ??= ProjectFolderSettingsProvider.GetOrCreateSettings();

    private static string SpriteManagerAssetPath => ProjectFolderSettingsProvider.BuildChildPath(ProjectFolders.DataFolderPath, "SpriteManager.asset");

    private static SpriteManagerScriptableObject CachedSpriteManager
    {
        get
        {
            cachedSpriteManager ??= AssetDatabase.LoadAssetAtPath<SpriteManagerScriptableObject>(SpriteManagerAssetPath);
            if (cachedSpriteManager == null)
            {
                ProjectFolderSettingsProvider.EnsureFolderHierarchy(ProjectFolders.RootFolderPath);
                ProjectFolderSettingsProvider.EnsureFolderHierarchy(ProjectFolders.DataFolderPath);

                cachedSpriteManager = ScriptableObject.CreateInstance<SpriteManagerScriptableObject>();
                AssetDatabase.CreateAsset(cachedSpriteManager, SpriteManagerAssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"RabbitDog: Created default AtlasManager asset at '{SpriteManagerAssetPath}'.");
            }

            cachedSpriteManager = AssetDatabase.LoadAssetAtPath<SpriteManagerScriptableObject>(SpriteManagerAssetPath);
            return cachedSpriteManager;
        }
    }

    public static void ClearCachedSpriteManager()
    {
        cachedSpriteManager = null;
        UpdateRefs();
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        var hasFolderChanged = false;
        foreach (var folderPathGuid in CachedSpriteManager.folderPathGuids)
        {
            var folderPath = AssetDatabase.GUIDToAssetPath(folderPathGuid);
            foreach (var assetPath in importedAssets)
            {
                if (assetPath.Contains(folderPath))
                {
                    hasFolderChanged = true;
                    break;
                }
            }

            if (hasFolderChanged)
                break;

            foreach (var assetPath in deletedAssets)
            {
                if (assetPath.Contains(folderPath))
                {
                    hasFolderChanged = true;
                    break;
                }
            }

            if (hasFolderChanged)
                break;

            foreach (var assetPath in movedAssets)
            {
                if (assetPath.Contains(folderPath))
                {
                    hasFolderChanged = true;
                    break;
                }
            }

            if (hasFolderChanged)
                break;

            foreach (var assetPath in movedFromAssetPaths)
            {
                if (assetPath.Contains(folderPath))
                {
                    hasFolderChanged = true;
                    break;
                }
            }

            if (hasFolderChanged)
                break;
        }

        if (hasFolderChanged)
        {
            UpdateRefs();
        }
    }

    public static void CheckAndUpdateRefs(string changedAssetPath)
    {
        foreach (var folderGuid in CachedSpriteManager.folderPathGuids)
        {
            string folderPath = AssetDatabase.GUIDToAssetPath(folderGuid);
            if (changedAssetPath.Contains(folderPath))
            {
                UpdateRefs();
                break;
            }
        }
    }

    public static void UpdateRefs()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
            return;

        CachedSpriteManager.atlasRefs.Clear();
        CachedSpriteManager.spriteRefs.Clear();
        foreach (var folderGuid in CachedSpriteManager.folderPathGuids)
        {
            string folderPath = AssetDatabase.GUIDToAssetPath(folderGuid);
            var searchInFolder = new[] { folderPath };
            // sprite in atlas
            {
                string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas", searchInFolder);
                foreach (var guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    SpriteAtlas asset = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(assetPath);
                    if (asset == null)
                        continue;

                    AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                    if (entry == null)
                        continue;

                    CachedSpriteManager.atlasRefs.Add(new AssetReferenceT<SpriteAtlas>(guid));
                }
            }
            // standalone sprite
            {
                string[] guids = AssetDatabase.FindAssets("t:Sprite", searchInFolder);
                foreach (var guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                    Sprite asset = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (asset == null)
                        continue;

                    AddressableAssetEntry entry = settings.FindAssetEntry(guid);
                    if (entry == null)
                        continue;

                    CachedSpriteManager.spriteRefs.Add(fileName.djb2Hash(), new AssetReferenceT<Sprite>(guid));
                }
            }
        }

        UpdateAtlasManagerScriptableObject();

        // 변경 사항 저장
        EditorUtility.SetDirty(CachedSpriteManager);
        AssetDatabase.SaveAssets();
        Debug.Log("AtlasManagerScriptableObject가 업데이트되었습니다.");
    }

    private static void UpdateAtlasManagerScriptableObject()
    {
        // Dictionary 초기화
        CachedSpriteManager.spriteNameToAtlasDict.Clear();

        // 각 AssetReferenceT<SpriteAtlas>에서 스프라이트 이름 추출 후 딕셔너리에 저장
        foreach (var atlasRef in CachedSpriteManager.atlasRefs)
        {
            SpriteAtlas spriteAtlas = atlasRef.editorAsset;

            if (spriteAtlas != null)
            {
                Sprite[] sprites = new Sprite[spriteAtlas.spriteCount];
                spriteAtlas.GetSprites(sprites);

                foreach (var sprite in sprites)
                {
                    // 스프라이트 이름과 해당 atlasRef 저장
                    CachedSpriteManager.spriteNameToAtlasDict[sprite.name.Replace("(Clone)", "").djb2Hash()] = atlasRef.AssetGUID.djb2Hash();
                }
            }
        }
    }
}
}