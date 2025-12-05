using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class FindTextureReferencesWindow : EditorWindow
{
    private Texture2D sourceTexture;
    private Texture2D targetTexture;
    private List<ReferenceInfo> referencedPaths = new List<ReferenceInfo>();
    private Vector2 scroll;
    private bool includeInactive = true;
    private bool searchInScenes = true;
    private bool searchInPrefabs = true;
    private bool searchInMaterials = true;
    private bool searchInScriptableObjects = true;
    private bool showReplaceOptions = false;

    private class ReferenceInfo
    {
        public string Path;
        public string ComponentPath;
        public string ComponentType;
        public Object ReferenceObject;
        public bool IsSelected = true;

        public override string ToString()
        {
            return $"{Path} - {ComponentPath} ({ComponentType})";
        }
    }

    [MenuItem("Tools/Find Texture2D References (UI)")]
    public static void ShowWindow()
    {
        GetWindow<FindTextureReferencesWindow>("Texture2D Finder");
    }

    void OnGUI()
    {
        GUILayout.Label("🔍 Texture2D 참조 찾기", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        sourceTexture = (Texture2D)EditorGUILayout.ObjectField("🔍 찾을 텍스처", sourceTexture, typeof(Texture2D), false);
        targetTexture = (Texture2D)EditorGUILayout.ObjectField("🎯 교체할 텍스처", targetTexture, typeof(Texture2D), false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        GUILayout.Label("검색 옵션", EditorStyles.boldLabel);
        includeInactive = EditorGUILayout.Toggle("비활성화된 오브젝트 포함", includeInactive);
        searchInScenes = EditorGUILayout.Toggle("씬에서 검색", searchInScenes);
        searchInPrefabs = EditorGUILayout.Toggle("프리팹에서 검색", searchInPrefabs);
        searchInMaterials = EditorGUILayout.Toggle("머티리얼에서 검색", searchInMaterials);
        searchInScriptableObjects = EditorGUILayout.Toggle("ScriptableObject에서 검색", searchInScriptableObjects);

        if (GUILayout.Button("검색 시작") && sourceTexture != null)
        {
            FindReferences();
            showReplaceOptions = referencedPaths.Count > 0 && targetTexture != null;
        }

        if (referencedPaths.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label($"📄 참조 결과 ({referencedPaths.Count}개):", EditorStyles.boldLabel);

            if (showReplaceOptions)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("전체 선택"))
                {
                    foreach (var info in referencedPaths)
                    {
                        info.IsSelected = true;
                    }
                }
                if (GUILayout.Button("전체 해제"))
                {
                    foreach (var info in referencedPaths)
                    {
                        info.IsSelected = false;
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("선택된 항목 교체"))
                {
                    ReplaceSelectedTextures();
                }
            }

            scroll = GUILayout.BeginScrollView(scroll);
            foreach (var info in referencedPaths)
            {
                EditorGUILayout.BeginHorizontal();
                if (showReplaceOptions)
                {
                    info.IsSelected = EditorGUILayout.Toggle(info.IsSelected, GUILayout.Width(20));
                }
                if (GUILayout.Button(info.ToString(), EditorStyles.linkLabel))
                {
                    if (info.ReferenceObject != null)
                    {
                        Selection.activeObject = info.ReferenceObject;
                        EditorGUIUtility.PingObject(info.ReferenceObject);
                    }
                }
                if (GUILayout.Button("🔍", GUILayout.Width(30)))
                {
                    ShowInHierarchy(info);
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
    }

    private void ReplaceSelectedTextures()
    {
        if (targetTexture == null) return;

        int replacedCount = 0;
        foreach (var info in referencedPaths.Where(x => x.IsSelected))
        {
            if (ReplaceTexture(info))
            {
                replacedCount++;
            }
        }

        Debug.Log($"✅ {replacedCount}개의 텍스처가 교체되었습니다.");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 교체 후 검증
        EditorApplication.delayCall += () =>
        {
            FindReferences();
            if (referencedPaths.Count > 0)
            {
                Debug.LogWarning($"⚠️ 아직 {referencedPaths.Count}개의 텍스처가 남아있습니다. 다시 시도해보세요.");
            }
            else
            {
                Debug.Log("✅ 모든 텍스처가 성공적으로 교체되었습니다!");
            }
        };
    }

    private bool ReplaceTexture(ReferenceInfo info)
    {
        var obj = AssetDatabase.LoadAssetAtPath<GameObject>(info.Path);
        if (obj == null) return false;

        GameObject prefabInstance = null;
        try
        {
            prefabInstance = PrefabUtility.InstantiatePrefab(obj) as GameObject;
            if (prefabInstance == null) return false;

            var pathParts = info.ComponentPath.Split('/');
            Transform current = prefabInstance.transform;
            
            foreach (var part in pathParts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                current = current.Find(part);
                if (current == null) break;
            }

            if (current == null)
            {
                return false;
            }

            bool replaced = false;
            switch (info.ComponentType)
            {
                case "Image":
                    var image = current.GetComponent<UnityEngine.UI.Image>();
                    if (image != null)
                    {
                        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(targetTexture));
                        if (sprite != null)
                        {
                            image.sprite = sprite;
                            replaced = true;
                        }
                    }
                    break;

                case "RawImage":
                    var rawImage = current.GetComponent<UnityEngine.UI.RawImage>();
                    if (rawImage != null)
                    {
                        rawImage.texture = targetTexture;
                        replaced = true;
                    }
                    break;

                case "SpriteRenderer":
                    var renderer = current.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(targetTexture));
                        if (sprite != null)
                        {
                            renderer.sprite = sprite;
                            replaced = true;
                        }
                    }
                    break;
            }

            if (replaced)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(prefabInstance, info.Path, InteractionMode.AutomatedAction);
            }

            return replaced;
        }
        finally
        {
            if (prefabInstance != null)
            {
                DestroyImmediate(prefabInstance);
            }
        }
    }

    private void ShowInHierarchy(ReferenceInfo info)
    {
        var obj = AssetDatabase.LoadAssetAtPath<GameObject>(info.Path);
        if (obj != null)
        {
            var prefabInstance = PrefabUtility.InstantiatePrefab(obj) as GameObject;
            if (prefabInstance != null)
            {
                var pathParts = info.ComponentPath.Split('/');
                Transform current = prefabInstance.transform;
                
                foreach (var part in pathParts)
                {
                    if (string.IsNullOrEmpty(part)) continue;
                    current = current.Find(part);
                    if (current == null) break;
                }

                if (current != null)
                {
                    Selection.activeGameObject = current.gameObject;
                    SceneView.FrameLastActiveSceneView();
                }

                PrefabUtility.UnloadPrefabContents(prefabInstance);
            }
        }
    }

    void FindReferences()
    {
        if (sourceTexture == null) return;

        string texturePath = AssetDatabase.GetAssetPath(sourceTexture);
        string textureGuid = AssetDatabase.AssetPathToGUID(texturePath);
        referencedPaths.Clear();

        // 씬에서 검색
        if (searchInScenes)
        {
            string[] scenePaths = Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories);
            foreach (string scenePath in scenePaths)
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                var rootObjects = scene.GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    SearchInGameObject(root, textureGuid, scenePath);
                }
            }
        }

        // 프리팹에서 검색
        if (searchInPrefabs)
        {
            string[] prefabPaths = Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories);
            foreach (string prefabPath in prefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    SearchInGameObject(prefab, textureGuid, prefabPath);
                }
            }
        }

        // ScriptableObject에서 검색
        if (searchInScriptableObjects)
        {
            string[] assetPaths = Directory.GetFiles("Assets", "*.asset", SearchOption.AllDirectories);
            foreach (string assetPath in assetPaths)
            {
                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (obj != null)
                {
                    var serializedObject = new SerializedObject(obj);
                    var iterator = serializedObject.GetIterator();
                    bool found = false;
                    while (iterator.NextVisible(true))
                    {
                        if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            var reference = iterator.objectReferenceValue;
                            if (reference == sourceTexture)
                            {
                                referencedPaths.Add(new ReferenceInfo
                                {
                                    Path = assetPath,
                                    ComponentPath = iterator.propertyPath,
                                    ComponentType = obj.GetType().Name,
                                    ReferenceObject = obj
                                });
                                break;
                            }
                        }
                    }
                }
            }
        }

        Debug.Log($"✅ '{texturePath}' 을 참조한 에셋 {referencedPaths.Count}개 찾음");
    }

    private void SearchInGameObject(GameObject gameObject, string textureGuid, string assetPath)
    {
        if (!includeInactive && !gameObject.activeInHierarchy) return;

        // Image 컴포넌트 검사
        var images = gameObject.GetComponentsInChildren<UnityEngine.UI.Image>(includeInactive);
        foreach (var image in images)
        {
            if (image.sprite != null)
            {
                string spritePath = AssetDatabase.GetAssetPath(image.sprite);
                string spriteGuid = AssetDatabase.AssetPathToGUID(spritePath);
                if (spriteGuid == textureGuid)
                {
                    string componentPath = GetFullPath(image.transform, gameObject.transform);
                    referencedPaths.Add(new ReferenceInfo
                    {
                        Path = assetPath,
                        ComponentPath = componentPath,
                        ComponentType = "Image",
                        ReferenceObject = image.gameObject
                    });
                }
            }
        }

        // RawImage 컴포넌트 검사
        var rawImages = gameObject.GetComponentsInChildren<UnityEngine.UI.RawImage>(includeInactive);
        foreach (var rawImage in rawImages)
        {
            if (rawImage.texture == sourceTexture)
            {
                string componentPath = GetFullPath(rawImage.transform, gameObject.transform);
                referencedPaths.Add(new ReferenceInfo
                {
                    Path = assetPath,
                    ComponentPath = componentPath,
                    ComponentType = "RawImage",
                    ReferenceObject = rawImage.gameObject
                });
            }
        }

        // SpriteRenderer 컴포넌트 검사
        var spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>(includeInactive);
        foreach (var renderer in spriteRenderers)
        {
            if (renderer.sprite != null)
            {
                string spritePath = AssetDatabase.GetAssetPath(renderer.sprite);
                string spriteGuid = AssetDatabase.AssetPathToGUID(spritePath);
                if (spriteGuid == textureGuid)
                {
                    string componentPath = GetFullPath(renderer.transform, gameObject.transform);
                    referencedPaths.Add(new ReferenceInfo
                    {
                        Path = assetPath,
                        ComponentPath = componentPath,
                        ComponentType = "SpriteRenderer",
                        ReferenceObject = renderer.gameObject
                    });
                }
            }
        }
    }

    private string GetFullPath(Transform transform, Transform root)
    {
        List<string> path = new List<string>();
        Transform current = transform;

        while (current != root && current != null)
        {
            path.Add(current.name);
            current = current.parent;
        }

        path.Reverse();
        return string.Join("/", path);
    }
}
