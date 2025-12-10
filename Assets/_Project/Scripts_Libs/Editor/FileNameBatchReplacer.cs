using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CookApps.Editor
{
    public class FileNameBatchReplacer : EditorWindow
    {
        private Object _targetFolder;
        private Vector2 _scrollPosition;
        private List<string> _matchedFiles = new List<string>();
        private bool _includeSubfolders = true;
        private readonly List<MappingEntry> _mappings = new List<MappingEntry> { new MappingEntry() };

        [MenuItem("Tools/File Name Batch Replacer")]
        private static void Open()
        {
            var window = GetWindow<FileNameBatchReplacer>("File Name Batch Replacer");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private void OnGUI()
        {
            // 헤더
            DrawHeader();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(5);

            // 설정 섹션
            DrawSettingsSection();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(5);

            // 검색 결과 섹션
            DrawResultsSection();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("파일명 일괄 교체 도구", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("특정 폴더의 하위 폴더까지 검색하여, 구분자(언더스코어/하이픈 등) 단위로 정확히 일치하는 부분만 교체합니다.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("설정", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 폴더 선택
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("대상 폴더", GUILayout.Width(120));
            _targetFolder = EditorGUILayout.ObjectField(_targetFolder, typeof(Object), false);
            EditorGUILayout.EndHorizontal();
            
            if (_targetFolder != null && !AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(_targetFolder)))
            {
                EditorGUILayout.HelpBox("폴더를 선택해주세요.", MessageType.Warning);
                _targetFolder = null;
            }
            else if (_targetFolder != null)
            {
                string folderPath = AssetDatabase.GetAssetPath(_targetFolder);
                EditorGUILayout.LabelField($"경로: {folderPath}", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(5);

            // 옵션
            _includeSubfolders = EditorGUILayout.Toggle("하위 폴더 포함", _includeSubfolders);

            EditorGUILayout.Space(8);

            // 매핑 목록
            DrawMappingsList();

            EditorGUILayout.Space(5);

            // 검색 버튼
            GUI.enabled = _targetFolder != null && HasValidMapping();
            if (GUILayout.Button("파일 검색", GUILayout.Height(30)))
            {
                SearchFiles();
            }
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        private void DrawResultsSection()
        {
            EditorGUILayout.LabelField("검색 결과", EditorStyles.boldLabel);
            
            if (_matchedFiles.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"총 {_matchedFiles.Count}개 파일이 검색되었습니다.", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                // 결과 리스트
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(250));
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                foreach (var file in _matchedFiles)
                {
                    string fileName = Path.GetFileName(file);
                    string newFileName = TryApplyMappings(fileName, out var mappedName) ? mappedName : fileName;
                    string directory = Path.GetDirectoryName(file).Replace('\\', '/');
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("기존:", GUILayout.Width(50));
                    EditorGUILayout.LabelField(fileName, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("변경:", GUILayout.Width(50));
                    EditorGUILayout.LabelField(newFileName, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.LabelField($"경로: {directory}", EditorStyles.miniLabel);
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(5);

                // 실행 버튼
                EditorGUILayout.BeginHorizontal();
                
                GUI.enabled = HasValidMapping();
                if (GUILayout.Button("파일명 교체 실행", GUILayout.Height(35)))
                {
                    if (EditorUtility.DisplayDialog("파일명 교체 확인",
                        $"{_matchedFiles.Count}개의 파일명을 교체하시겠습니까?\n\n이 작업은 되돌릴 수 없습니다!",
                        "확인", "취소"))
                    {
                        ReplaceFileNames();
                    }
                }
                GUI.enabled = true;
                
                if (GUILayout.Button("초기화", GUILayout.Height(35), GUILayout.Width(100)))
                {
                    _matchedFiles.Clear();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            else if (_targetFolder != null && HasValidMapping())
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.HelpBox("검색 결과가 없습니다.", MessageType.Info);
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.HelpBox("폴더를 선택하고 검색할 문자열을 입력한 후 '파일 검색' 버튼을 클릭하세요.", MessageType.Info);
                EditorGUILayout.EndVertical();
            }
        }

        private void SearchFiles()
        {
            _matchedFiles.Clear();

            if (_targetFolder == null || !HasValidMapping())
            {
                return;
            }

            string folderPath = AssetDatabase.GetAssetPath(_targetFolder);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            // 모든 파일 검색
            string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                
                // 폴더가 아닌 파일만 처리
                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }

                // 하위 폴더 포함 옵션 체크
                if (!_includeSubfolders)
                {
                    string relativePath = assetPath.Replace(folderPath + "/", "");
                    if (relativePath.Contains("/"))
                    {
                        continue;
                    }
                }

                string fileName = Path.GetFileName(assetPath);

                if (TryApplyMappings(fileName, out _))
                {
                    _matchedFiles.Add(assetPath);
                }
            }

            _matchedFiles.Sort();
        }

        /// <summary>
        /// 매핑 중 유효한 항목이 하나라도 있는지 확인
        /// </summary>
        private bool HasValidMapping()
        {
            foreach (var mapping in _mappings)
            {
                if (mapping.Enabled && !string.IsNullOrEmpty(mapping.Search))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 매핑 리스트 UI
        /// </summary>
        private void DrawMappingsList()
        {
            EditorGUILayout.LabelField("교체 매핑 (다수 적용 가능)", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            for (int i = 0; i < _mappings.Count; i++)
            {
                var mapping = _mappings[i];

                EditorGUILayout.BeginHorizontal();

                mapping.Enabled = EditorGUILayout.Toggle(mapping.Enabled, GUILayout.Width(20));
                mapping.Search = EditorGUILayout.TextField(mapping.Search, GUILayout.Width(180));
                EditorGUILayout.LabelField("→", GUILayout.Width(15));
                mapping.Replace = EditorGUILayout.TextField(mapping.Replace, GUILayout.Width(180));

                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    _mappings.RemoveAt(i);
                    i--;
                    continue;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("매핑 추가", GUILayout.Height(24)))
            {
                _mappings.Add(new MappingEntry());
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 파일명에서 검색 문자열이 정확히 일치하는지 확인
        /// 부분 문자열이 아닌 정확한 매칭만 허용
        /// 예: "Character_1234"에서 "123"을 찾으면 false (1234의 일부이므로)
        ///     "Character_123"에서 "123"을 찾으면 true (정확히 일치)
        /// </summary>
        private bool IsExactMatch(string fileName, string searchString)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(searchString))
            {
                return false;
            }

            // 확장자 제거
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            
            // 파일명 전체가 정확히 일치하는 경우
            if (nameWithoutExtension == searchString)
            {
                return true;
            }
            
            // 정규식을 사용하여 구분자(언더스코어, 하이픈, 공백, 점 등) 앞뒤에서 정확히 일치하는지 확인
            // 패턴: 구분자 앞뒤 또는 문자열 시작/끝에서 정확히 일치
            string pattern = @"(^|[_\-\.\s\(\)\[\]])" + Regex.Escape(searchString) + @"([_\-\.\s\(\)\[\]]|$)";
            
            return Regex.IsMatch(nameWithoutExtension, pattern);
        }

        /// <summary>
        /// 파일명에서 검색 문자열을 정확히 일치하는 부분만 교체
        /// 구분자(언더스코어, 하이픈 등)로 구분된 부분만 교체하여 부분 문자열 교체를 방지
        /// </summary>
        private string ReplaceExactMatch(string fileName, string searchString, string replaceString)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(searchString))
            {
                return fileName;
            }

            string extension = Path.GetExtension(fileName);
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            
            // 전체 파일명이 정확히 일치하는 경우
            if (nameWithoutExtension == searchString)
            {
                return replaceString + extension;
            }
            
            // 정규식을 사용하여 단어 경계에서 정확히 일치하는 부분만 교체
            // 구분자(언더스코어, 하이픈, 공백, 점 등) 앞뒤에서 일치하는 경우만 교체
            
            // 패턴: 구분자 앞뒤 또는 문자열 시작/끝에서 정확히 일치
            string pattern = @"(^|[_\-\.\s\(\)\[\]])" + Regex.Escape(searchString) + @"([_\-\.\s\(\)\[\]]|$)";
            string replacement = "$1" + replaceString + "$2";
            
            string result = Regex.Replace(nameWithoutExtension, pattern, replacement);
            
            // 변경이 없었다면 원본 반환
            if (result == nameWithoutExtension)
            {
                return fileName;
            }
            
            return result + extension;
        }

        /// <summary>
        /// 여러 매핑을 순차적으로 적용하여 변경 여부와 결과를 반환
        /// </summary>
        private bool TryApplyMappings(string fileName, out string newFileName)
        {
            newFileName = fileName;

            foreach (var mapping in _mappings)
            {
                if (!mapping.Enabled || string.IsNullOrEmpty(mapping.Search))
                    continue;

                newFileName = ReplaceExactMatch(newFileName, mapping.Search, mapping.Replace ?? string.Empty);
            }

            return newFileName != fileName;
        }

        [System.Serializable]
        private class MappingEntry
        {
            public bool Enabled = true;
            public string Search = string.Empty;
            public string Replace = string.Empty;
        }

        private void ReplaceFileNames()
        {
            int successCount = 0;
            int failCount = 0;
            List<string> failedFiles = new List<string>();

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (var filePath in _matchedFiles)
                {
                    try
                    {
                        string directory = Path.GetDirectoryName(filePath).Replace('\\', '/');
                        string fileName = Path.GetFileName(filePath);
                        
                        // 정확히 일치하는 부분만 교체 (모든 매핑 적용)
                        if (!TryApplyMappings(fileName, out var newFileName) || fileName == newFileName)
                        {
                            continue;
                        }
                        string newFilePath = $"{directory}/{newFileName}";

                        // 파일명이 변경되지 않은 경우 스킵
                        if (fileName == newFileName)
                        {
                            continue;
                        }

                        // 이미 같은 이름의 파일이 있는지 확인
                        if (File.Exists(newFilePath) || AssetDatabase.LoadAssetAtPath<Object>(newFilePath) != null)
                        {
                            failedFiles.Add($"{filePath} (이미 존재하는 파일명: {newFileName})");
                            failCount++;
                            continue;
                        }

                        // 파일명 변경
                        string error = AssetDatabase.MoveAsset(filePath, newFilePath);
                        if (string.IsNullOrEmpty(error))
                        {
                            successCount++;
                        }
                        else
                        {
                            failedFiles.Add($"{filePath} (에러: {error})");
                            failCount++;
                        }
                    }
                    catch (System.Exception e)
                    {
                        failedFiles.Add($"{filePath} (예외: {e.Message})");
                        failCount++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            // 결과 표시
            string message = $"파일명 교체 완료!\n\n성공: {successCount}개\n실패: {failCount}개";
            if (failedFiles.Count > 0)
            {
                message += "\n\n실패한 파일:\n";
                foreach (var failed in failedFiles)
                {
                    message += $"- {failed}\n";
                }
            }

            EditorUtility.DisplayDialog("파일명 교체 결과", message, "확인");

            // 검색 결과 초기화
            _matchedFiles.Clear();
        }
    }
}

