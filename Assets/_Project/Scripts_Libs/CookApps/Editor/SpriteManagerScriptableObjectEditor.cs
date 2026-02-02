using CookApps.TeamBattle;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace CookApps.Editor
{
    [CustomEditor(typeof(SpriteManagerScriptableObject))]
    public class SpriteManagerScriptableObjectEditor : UnityEditor.Editor
    {
        private bool showAtlases;
        private bool showSprites;

        public override void OnInspectorGUI()
        {
            SpriteManagerScriptableObject so = (SpriteManagerScriptableObject)target;

            // 현재 저장된 폴더 경로들을 표시합니다.
            EditorGUILayout.LabelField("Folder Paths:");
            if (so.folderPathGuids.Count == 0)
            {
                EditorGUILayout.HelpBox("No folders added. Drag & drop folders below.", MessageType.Info);
            }
            else
            {
                string guidToRemove = null;

                for (int i = 0; i < so.folderPathGuids.Count; i++)
                {
                    string pathGuid = so.folderPathGuids[i];
                    string path = AssetDatabase.GUIDToAssetPath(pathGuid);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("- " + path);

                    if (GUILayout.Button("X", GUILayout.Width(30)))
                    {
                        guidToRemove = pathGuid;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                // 제거 버튼이 클릭되었을 때 처리
                if (guidToRemove != null)
                {
                    so.folderPathGuids.Remove(guidToRemove);
                    SpriteManagerImportProcessor.ClearCachedSpriteManager();
                    EditorUtility.SetDirty(so);
                }
            }

            // 드래그 앤 드롭 영역 생성
            Event evt = Event.current;
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop Folders Here");

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            string path = AssetDatabase.GetAssetPath(draggedObject);
                            string guid = AssetDatabase.AssetPathToGUID(path);
                            if (AssetDatabase.IsValidFolder(path) && !so.folderPathGuids.Contains(guid))
                            {
                                so.folderPathGuids.Add(guid);
                                SpriteManagerImportProcessor.ClearCachedSpriteManager();
                                EditorUtility.SetDirty(so);
                            }
                        }
                    }

                    break;
            }

            // 리스트에 저장된 폴더 경로를 모두 지우는 버튼
            if (GUILayout.Button("Clear Folder Paths"))
            {
                so.folderPathGuids.Clear();
                SpriteManagerImportProcessor.ClearCachedSpriteManager();
                EditorUtility.SetDirty(so);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Registered Assets", EditorStyles.boldLabel);

            showAtlases = EditorGUILayout.Foldout(showAtlases, $"Atlases ({so.atlasRefs.Count})");
            if (showAtlases)
            {
                EditorGUI.indentLevel++;
                GUI.enabled = false;
                foreach (var atlasRef in so.atlasRefs)
                {
                    EditorGUILayout.ObjectField(atlasRef?.editorAsset, typeof(SpriteAtlas), false);
                }
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }

            showSprites = EditorGUILayout.Foldout(showSprites, $"Sprites ({so.spriteRefs.Count})");
            if (showSprites)
            {
                EditorGUI.indentLevel++;
                GUI.enabled = false;
                foreach (var kvp in so.spriteRefs)
                {
                    EditorGUILayout.ObjectField(kvp.Value?.editorAsset, typeof(Sprite), false);
                }
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
        }
    }
}
