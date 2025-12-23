using CookApps.TeamBattle;
using UnityEditor;
using UnityEngine;

namespace CookApps.Editor
{
    [CustomEditor(typeof(SpriteManagerScriptableObject))]
    public class AtlasManagerScriptableObjectEditor : UnityEditor.Editor
    {
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
                foreach (string pathGuid in so.folderPathGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(pathGuid);
                    EditorGUILayout.LabelField("- " + path);
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
        }
    }
}
