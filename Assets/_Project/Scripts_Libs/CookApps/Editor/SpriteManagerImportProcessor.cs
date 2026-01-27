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

        [MenuItem("Tools/Update SpriteManager")]
        public static void UpdateRefs()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return;

            // 해시 충돌 검증용 딕셔너리
            var spriteHashToName = new System.Collections.Generic.Dictionary<ulong, string>();
            var atlasHashToGuid = new System.Collections.Generic.Dictionary<ulong, string>();
            bool hasCollision = false;

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

                        // Atlas GUID 해시 충돌 검증
                        ulong atlasHash = guid.djb2Hash();
                        if (atlasHashToGuid.TryGetValue(atlasHash, out var existingGuid))
                        {
                            Debug.LogError($"[SpriteManager] Atlas GUID hash collision detected!\n  Hash: {atlasHash}\n  GUID1: {existingGuid}\n  GUID2: {guid}");
                            hasCollision = true;
                        }
                        else
                        {
                            atlasHashToGuid[atlasHash] = guid;
                        }

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

                        // Standalone sprite 이름 해시 충돌 검증
                        ulong spriteHash = fileName.djb2Hash();
                        if (spriteHashToName.TryGetValue(spriteHash, out var existingName))
                        {
                            if (existingName != fileName)
                            {
                                Debug.LogError($"[SpriteManager] Standalone sprite name hash collision detected!\n  Hash: {spriteHash}\n  Name1: {existingName}\n  Name2: {fileName}");
                                hasCollision = true;
                            }
                        }
                        else
                        {
                            spriteHashToName[spriteHash] = fileName;
                        }

                        CachedSpriteManager.spriteRefs.Add(fileName.djb2Hash(), new AssetReferenceT<Sprite>(guid));
                    }
                }
            }

            UpdateAtlasManagerScriptableObject(spriteHashToName, ref hasCollision);

            if (hasCollision)
            {
                Debug.LogError("[SpriteManager] Hash collision detected! Please rename the conflicting sprites/atlases.");
            }

            // 변경 사항 저장
            EditorUtility.SetDirty(CachedSpriteManager);
            AssetDatabase.SaveAssets();
            Debug.Log("AtlasManagerScriptableObject가 업데이트되었습니다.");
        }

        private static void UpdateAtlasManagerScriptableObject(
            System.Collections.Generic.Dictionary<ulong, string> spriteHashToName,
            ref bool hasCollision)
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
                        string spriteName = sprite.name.Replace("(Clone)", "");
                        ulong spriteHash = spriteName.djb2Hash();

                        // Atlas 내 스프라이트 이름 해시 충돌 검증
                        if (spriteHashToName.TryGetValue(spriteHash, out var existingName))
                        {
                            if (existingName != spriteName)
                            {
                                Debug.LogError($"[SpriteManager] Atlas sprite name hash collision detected!\n  Hash: {spriteHash}\n  Name1: {existingName}\n  Name2: {spriteName} (in atlas: {spriteAtlas.name})");
                                hasCollision = true;
                            }
                        }
                        else
                        {
                            spriteHashToName[spriteHash] = spriteName;
                        }

                        // 스프라이트 이름과 해당 atlasRef 저장
                        CachedSpriteManager.spriteNameToAtlasDict[spriteHash] = atlasRef.AssetGUID.djb2Hash();
                    }
                }
            }
        }
    }
}