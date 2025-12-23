using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using CookApps.TeamBattle;

namespace CookApps.AutoBattler.Editor
{
    public class SpriteLoaderAutoSetup : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<string> logs = new List<string>();
        private bool isProcessing = false;
        private int processedCount = 0;
        private int totalCount = 0;
        private int modifiedCount = 0;

        [MenuItem("Tools/SpriteLoader Auto Setup")]
        public static void ShowWindow()
        {
            GetWindow<SpriteLoaderAutoSetup>("SpriteLoader Auto Setup");
        }

        void OnGUI()
        {
            GUILayout.Label("SpriteLoader Auto Setup Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "이 도구는 모든 프리팹을 검사하여:\n" +
                "1. SpriteLoader 타입의 SerializeField를 찾습니다\n" +
                "2. 해당 필드와 연결된 GameObject를 찾습니다\n" +
                "3. Image/SpriteRenderer가 있는 GameObject에 SpriteLoader를 추가합니다\n" +
                "4. SerializeField에 자동으로 연결합니다",
                MessageType.Info);

            GUILayout.Space(10);

            GUI.enabled = !isProcessing;
            if (GUILayout.Button("모든 프리팹 처리 시작", GUILayout.Height(30)))
            {
                ProcessAllPrefabs();
            }
            GUI.enabled = true;

            GUILayout.Space(10);

            if (isProcessing)
            {
                EditorGUI.ProgressBar(
                    EditorGUILayout.GetControlRect(GUILayout.Height(20)),
                    totalCount > 0 ? (float)processedCount / totalCount : 0,
                    $"처리 중... ({processedCount}/{totalCount})");
            }

            GUILayout.Space(10);
            GUILayout.Label($"처리된 프리팹: {processedCount} / 수정된 프리팹: {modifiedCount}", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("로그:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            foreach (var log in logs)
            {
                EditorGUILayout.LabelField(log, EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        void ProcessAllPrefabs()
        {
            logs.Clear();
            processedCount = 0;
            modifiedCount = 0;
            isProcessing = true;

            string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" });
            totalCount = allPrefabs.Length;

            AddLog($"총 {totalCount}개의 프리팹을 검사합니다...");

            try
            {
                for (int i = 0; i < allPrefabs.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(allPrefabs[i]);
                    if (ProcessPrefab(path))
                    {
                        modifiedCount++;
                    }
                    processedCount++;

                    if (i % 10 == 0)
                    {
                        EditorUtility.DisplayProgressBar("프리팹 처리 중",
                            $"처리 중... {i + 1}/{allPrefabs.Length}",
                            (float)(i + 1) / allPrefabs.Length);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                isProcessing = false;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                AddLog($"완료! 총 {modifiedCount}개의 프리팹이 수정되었습니다.");
            }
        }

        bool ProcessPrefab(string prefabPath)
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
                return false;

            bool modified = false;

            // 프리팹의 모든 컴포넌트를 검사
            Component[] allComponents = prefabAsset.GetComponentsInChildren<Component>(true);

            foreach (Component component in allComponents)
            {
                if (component == null)
                    continue;

                if (ProcessComponent(component, prefabPath))
                {
                    modified = true;
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(prefabAsset);
                PrefabUtility.SavePrefabAsset(prefabAsset);
                AddLog($"✓ 수정됨: {prefabPath}");
            }

            return modified;
        }

        bool ProcessComponent(Component component, string prefabPath)
        {
            bool modified = false;
            System.Type componentType = component.GetType();

            // 모든 SerializeField 수집
            FieldInfo[] fields = componentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // Image/SpriteRenderer 타입의 SerializeField 찾기
            var imageFields = new Dictionary<string, FieldInfo>();
            foreach (FieldInfo field in fields)
            {
                bool isSerializeField = field.IsPublic ||
                    field.GetCustomAttributes(typeof(SerializeField), true).Length > 0;

                if (!isSerializeField)
                    continue;

                if (field.FieldType == typeof(Image) || field.FieldType == typeof(SpriteRenderer))
                {
                    imageFields[field.Name] = field;
                }
            }

            // SpriteLoader 필드 처리
            foreach (FieldInfo field in fields)
            {
                // SpriteLoader 타입이 아니면 건너뛰기
                if (field.FieldType != typeof(SpriteLoader))
                    continue;

                // SerializeField 특성이 있는지 확인
                bool isSerializeField = field.IsPublic ||
                    field.GetCustomAttributes(typeof(SerializeField), true).Length > 0;

                if (!isSerializeField)
                    continue;

                // 현재 필드 값 가져오기
                SpriteLoader currentLoader = field.GetValue(component) as SpriteLoader;

                // 이미 연결되어 있고 올바르게 설정되어 있으면 건너뛰기
                if (currentLoader != null && IsLoaderProperlyConfigured(currentLoader))
                    continue;

                // 매칭되는 Image/SpriteRenderer 필드 찾기
                FieldInfo matchedImageField = FindMatchingImageField(field.Name, imageFields);

                GameObject targetGameObject = null;
                Image imageComponent = null;
                SpriteRenderer spriteRendererComponent = null;

                if (matchedImageField != null)
                {
                    // 매칭된 필드의 값 가져오기
                    object imageFieldValue = matchedImageField.GetValue(component);

                    if (imageFieldValue is Image img)
                    {
                        imageComponent = img;
                        targetGameObject = img.gameObject;
                    }
                    else if (imageFieldValue is SpriteRenderer sr)
                    {
                        spriteRendererComponent = sr;
                        targetGameObject = sr.gameObject;
                    }
                }

                // 매칭 실패 시 GameObject 이름으로 찾기
                if (targetGameObject == null)
                {
                    targetGameObject = FindTargetGameObject(component.gameObject, field.Name);

                    if (targetGameObject != null)
                    {
                        imageComponent = targetGameObject.GetComponent<Image>();
                        spriteRendererComponent = targetGameObject.GetComponent<SpriteRenderer>();
                    }
                }

                if (targetGameObject == null || (imageComponent == null && spriteRendererComponent == null))
                {
                    AddLog($"  ✗ 매칭 실패: {componentType.Name}.{field.Name} - 대상을 찾을 수 없음");
                    continue;
                }

                // SpriteLoader 추가 또는 가져오기
                SpriteLoader loader = targetGameObject.GetComponent<SpriteLoader>();
                if (loader == null)
                {
                    loader = targetGameObject.AddComponent<SpriteLoader>();
                    AddLog($"  + SpriteLoader 추가: {GetGameObjectPath(targetGameObject)} (필드: {componentType.Name}.{field.Name})");
                }

                // SpriteLoader 설정
                SetupSpriteLoader(loader, imageComponent, spriteRendererComponent);

                // 필드에 연결
                field.SetValue(component, loader);
                EditorUtility.SetDirty(component);
                EditorUtility.SetDirty(loader);

                modified = true;
                if (matchedImageField != null)
                {
                    AddLog($"  → 연결됨: {componentType.Name}.{field.Name} ↔ {matchedImageField.Name} → {GetGameObjectPath(targetGameObject)}");
                }
                else
                {
                    AddLog($"  → 연결됨: {componentType.Name}.{field.Name} → {GetGameObjectPath(targetGameObject)}");
                }
            }

            return modified;
        }

        FieldInfo FindMatchingImageField(string loaderFieldName, Dictionary<string, FieldInfo> imageFields)
        {
            if (imageFields.Count == 0)
                return null;

            // SpriteLoader 필드명에서 베이스 이름 추출
            string loaderBaseName = GetBaseName(loaderFieldName, new[] { "SpriteLoader", "spriteLoader", "Loader", "loader" });

            // 각 Image/SpriteRenderer 필드와 비교
            FieldInfo bestMatch = null;
            int bestMatchScore = 0;

            foreach (var kvp in imageFields)
            {
                string imageFieldName = kvp.Key;
                string imageBaseName = GetBaseName(imageFieldName, new[] { "Image", "image", "SpriteRenderer", "spriteRenderer", "Sprite", "sprite" });

                // 매칭 점수 계산
                int score = CalculateMatchScore(loaderBaseName, imageBaseName);

                if (score > bestMatchScore)
                {
                    bestMatchScore = score;
                    bestMatch = kvp.Value;
                }
            }

            // 최소 점수 이상일 때만 매칭으로 간주
            return bestMatchScore >= 50 ? bestMatch : null;
        }

        string GetBaseName(string fieldName, string[] suffixesToRemove)
        {
            string baseName = fieldName;

            // suffix 제거
            foreach (string suffix in suffixesToRemove)
            {
                if (baseName.EndsWith(suffix))
                {
                    baseName = baseName.Substring(0, baseName.Length - suffix.Length);
                    break;
                }
            }

            return baseName;
        }

        int CalculateMatchScore(string name1, string name2)
        {
            // 정규화 (소문자, _ 제거)
            string normalized1 = name1.ToLower().Replace("_", "");
            string normalized2 = name2.ToLower().Replace("_", "");

            // 완전 일치
            if (normalized1 == normalized2)
                return 100;

            // 한쪽이 다른쪽을 포함
            if (normalized1.Contains(normalized2) || normalized2.Contains(normalized1))
            {
                // 길이가 비슷할수록 높은 점수
                int lengthDiff = System.Math.Abs(normalized1.Length - normalized2.Length);
                return 80 - lengthDiff * 2;
            }

            // 공통 부분 비율 계산
            int commonLength = GetCommonPrefixLength(normalized1, normalized2);
            int maxLength = System.Math.Max(normalized1.Length, normalized2.Length);

            if (maxLength == 0)
                return 0;

            int ratio = (commonLength * 100) / maxLength;
            return ratio > 50 ? ratio : 0;
        }

        int GetCommonPrefixLength(string s1, string s2)
        {
            int minLength = System.Math.Min(s1.Length, s2.Length);
            int count = 0;

            for (int i = 0; i < minLength; i++)
            {
                if (s1[i] == s2[i])
                    count++;
                else
                    break;
            }

            return count;
        }

        bool IsLoaderProperlyConfigured(SpriteLoader loader)
        {
            if (loader == null)
                return false;

            SerializedObject so = new SerializedObject(loader);
            SerializedProperty isSpriteRendererProp = so.FindProperty("isSpriteRenderer");
            SerializedProperty targetRendererProp = so.FindProperty("targetRenderer");
            SerializedProperty targetImageProp = so.FindProperty("targetImage");

            bool isSpriteRenderer = isSpriteRendererProp.boolValue;

            if (isSpriteRenderer)
            {
                return targetRendererProp.objectReferenceValue != null;
            }
            else
            {
                return targetImageProp.objectReferenceValue != null;
            }
        }

        void SetupSpriteLoader(SpriteLoader loader, Image imageComponent, SpriteRenderer spriteRendererComponent)
        {
            SerializedObject so = new SerializedObject(loader);
            SerializedProperty isSpriteRendererProp = so.FindProperty("isSpriteRenderer");
            SerializedProperty targetRendererProp = so.FindProperty("targetRenderer");
            SerializedProperty targetImageProp = so.FindProperty("targetImage");

            if (spriteRendererComponent != null)
            {
                isSpriteRendererProp.boolValue = true;
                targetRendererProp.objectReferenceValue = spriteRendererComponent;
                targetImageProp.objectReferenceValue = null;
            }
            else if (imageComponent != null)
            {
                isSpriteRendererProp.boolValue = false;
                targetRendererProp.objectReferenceValue = null;
                targetImageProp.objectReferenceValue = imageComponent;
            }

            so.ApplyModifiedProperties();
        }

        GameObject FindTargetGameObject(GameObject baseGameObject, string fieldName)
        {
            // 필드 이름에서 "SpriteLoader" 부분을 제거하고 Image/SpriteRenderer를 찾기
            string baseName = fieldName;

            // 일반적인 네이밍 패턴들
            string[] suffixesToRemove = new string[]
            {
                "SpriteLoader",
                "spriteLoader",
                "Loader",
                "loader"
            };

            foreach (string suffix in suffixesToRemove)
            {
                if (baseName.EndsWith(suffix))
                {
                    baseName = baseName.Substring(0, baseName.Length - suffix.Length);
                    break;
                }
            }

            // _로 시작하는 경우 제거
            if (baseName.StartsWith("_"))
                baseName = baseName.Substring(1);

            // 1. 같은 GameObject에서 찾기
            Image img = baseGameObject.GetComponent<Image>();
            SpriteRenderer sr = baseGameObject.GetComponent<SpriteRenderer>();
            if (img != null || sr != null)
                return baseGameObject;

            // 2. 자식 중에서 정확한 이름으로 찾기
            Transform[] children = baseGameObject.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                // 이름이 baseName과 비슷하거나 포함하는 경우
                if (child.name.Contains(baseName) || baseName.Contains(child.name))
                {
                    img = child.GetComponent<Image>();
                    sr = child.GetComponent<SpriteRenderer>();
                    if (img != null || sr != null)
                        return child.gameObject;
                }
            }

            // 3. 원본 필드 이름으로 다시 시도
            foreach (Transform child in children)
            {
                if (child.name.Replace("_", "").Equals(fieldName.Replace("_", ""), System.StringComparison.OrdinalIgnoreCase))
                {
                    img = child.GetComponent<Image>();
                    sr = child.GetComponent<SpriteRenderer>();
                    if (img != null || sr != null)
                        return child.gameObject;
                }
            }

            return null;
        }

        string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        void AddLog(string message)
        {
            logs.Add(message);
            Repaint();
        }
    }
}