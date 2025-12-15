using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CookApps.Editor
{
    public class FileNameBatchReplacer : EditorWindow
    {
        private Object _targetFolder;
        private Vector2 _scrollPosition;
        private Vector2 _mappingScrollPosition;
        private List<string> _matchedFiles = new List<string>();
        private bool _includeSubfolders = true;
        private readonly List<MappingEntry> _mappings = new List<MappingEntry> { new MappingEntry() };
        private string _csvFilePath = string.Empty;
        private bool _csvHasHeader = true;
        
        // 비동기 검색을 위한 변수들
        private bool _isSearching = false;
        private string[] _searchGuids;
        private int _searchIndex = 0;
        private string _searchFolderPath;
        private const int FILES_PER_FRAME = 50; // 프레임당 처리할 파일 수

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

        private void OnEnable()
        {
            EditorApplication.update += OnUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
        }

        private void OnUpdate()
        {
            if (_isSearching)
            {
                ProcessSearchAsync();
            }
        }

        private void ProcessSearchAsync()
        {
            if (_searchGuids == null || _searchIndex >= _searchGuids.Length)
            {
                // 검색 완료
                FinishSearch();
                return;
            }

            int endIndex = Mathf.Min(_searchIndex + FILES_PER_FRAME, _searchGuids.Length);
            
            for (int i = _searchIndex; i < endIndex; i++)
            {
                string guid = _searchGuids[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                
                bool isFolder = AssetDatabase.IsValidFolder(assetPath);
                
                // 하위 폴더 포함 옵션 체크
                if (!_includeSubfolders && !isFolder)
                {
                    string relativePath = assetPath.Replace(_searchFolderPath + "/", "");
                    if (relativePath.Contains("/"))
                    {
                        continue;
                    }
                }

                string name = isFolder ? Path.GetFileName(assetPath) : Path.GetFileName(assetPath);

                // 각 매핑별로 개별적으로 매칭 확인 (카운트 계산)
                foreach (var mapping in _mappings)
                {
                    if (mapping.Enabled && !string.IsNullOrEmpty(mapping.Search))
                    {
                        if (IsExactMatch(name, mapping.Search))
                        {
                            mapping.MatchCount++;
                        }
                    }
                }

                // 매핑 적용 시도 (변경이 있는지 확인) - 파일과 폴더 모두 처리
                if (TryApplyMappings(name, out _))
                {
                    _matchedFiles.Add(assetPath);
                }
            }

            _searchIndex = endIndex;

            // 진행 상황 표시
            float progress = (float)_searchIndex / _searchGuids.Length;
            EditorUtility.DisplayProgressBar("파일 검색 중...", 
                $"파일 검색 중... ({_searchIndex}/{_searchGuids.Length})", progress);

            // Repaint 제거 - 검색 완료 후에만 호출
        }

        private void FinishSearch()
        {
            _isSearching = false;
            EditorUtility.ClearProgressBar();
            
            // 검색 완료 후 한 번만 Repaint
            Repaint();
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

            // 엑셀 파일 로드 섹션
            DrawExcelLoadSection();

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
                EditorGUILayout.LabelField($"총 {_matchedFiles.Count}개 항목이 검색되었습니다.", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                // 결과 리스트 - 변경 미리보기 제거
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(250));
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                foreach (var file in _matchedFiles)
                {
                    bool isFolder = AssetDatabase.IsValidFolder(file);
                    string name = Path.GetFileName(file);
                    string directory = Path.GetDirectoryName(file).Replace('\\', '/');
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(isFolder ? "[폴더]" : "[파일]", GUILayout.Width(50));
                    EditorGUILayout.LabelField(name, EditorStyles.wordWrappedLabel);
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
                        $"{_matchedFiles.Count}개의 항목명을 교체하시겠습니까?\n\n이 작업은 되돌릴 수 없습니다!",
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

            _searchFolderPath = AssetDatabase.GetAssetPath(_targetFolder);
            if (!AssetDatabase.IsValidFolder(_searchFolderPath))
            {
                return;
            }

            // 각 매핑별 카운트 초기화
            foreach (var mapping in _mappings)
            {
                mapping.MatchCount = 0;
            }

            // 모든 파일 GUID 가져오기
            _searchGuids = AssetDatabase.FindAssets("", new[] { _searchFolderPath });
            _searchIndex = 0;
            _isSearching = true;

            // 첫 프레임 처리 시작
            ProcessSearchAsync();
        }
        
        /// <summary>
        /// 각 매핑별로 매칭되는 파일 수 계산
        /// </summary>
        private void CalculateMappingCounts()
        {
            if (_targetFolder == null || _matchedFiles.Count == 0)
            {
                foreach (var mapping in _mappings)
                {
                    mapping.MatchCount = 0;
                }
                return;
            }

            string folderPath = AssetDatabase.GetAssetPath(_targetFolder);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            // 모든 파일 검색
            string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });

            // 각 매핑별 카운트 초기화
            foreach (var mapping in _mappings)
            {
                mapping.MatchCount = 0;
            }

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

                // 각 매핑별로 개별적으로 매칭 확인
                foreach (var mapping in _mappings)
                {
                    if (mapping.Enabled && !string.IsNullOrEmpty(mapping.Search))
                    {
                        if (IsExactMatch(fileName, mapping.Search))
                        {
                            mapping.MatchCount++;
                        }
                    }
                }
            }
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
        /// CSV 파일 로드 섹션 UI
        /// </summary>
        private void DrawExcelLoadSection()
        {
            EditorGUILayout.LabelField("CSV 파일에서 매핑 불러오기", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // CSV 파일 경로
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("CSV 파일", GUILayout.Width(120));
            EditorGUILayout.TextField(_csvFilePath, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("찾아보기", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFilePanel("CSV 파일 선택", "", "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    _csvFilePath = path;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 헤더 행 포함 여부
            _csvHasHeader = EditorGUILayout.Toggle("헤더 행 포함 (첫 번째 행 스킵)", _csvHasHeader);
            
            EditorGUILayout.Space(5);
            
            // CSV 파일 형식 안내
            EditorGUILayout.HelpBox(
                "CSV 파일 형식:\n" +
                "- 첫 번째 열: Search (검색할 문자열)\n" +
                "- 두 번째 열: Replace (교체할 문자열)\n" +
                "- 쉼표(,)로 구분\n" +
                "- 엑셀에서 '다른 이름으로 저장' → 'CSV UTF-8(쉼표로 구분)(*.csv)' 선택",
                MessageType.Info);
            
            EditorGUILayout.Space(5);
            
            // 로드 버튼
            GUI.enabled = !string.IsNullOrEmpty(_csvFilePath) && File.Exists(_csvFilePath);
            if (GUILayout.Button("CSV에서 매핑 불러오기", GUILayout.Height(30)))
            {
                LoadMappingsFromCsv();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// CSV 파일에서 매핑 데이터를 읽어옵니다.
        /// </summary>
        private void LoadMappingsFromCsv()
        {
            if (string.IsNullOrEmpty(_csvFilePath) || !File.Exists(_csvFilePath))
            {
                EditorUtility.DisplayDialog("오류", "CSV 파일을 선택해주세요.", "확인");
                return;
            }

            try
            {
                List<MappingEntry> loadedMappings = new List<MappingEntry>();
                int loadedCount = 0;
                int skippedCount = 0;

                // CSV 파일 읽기
                string[] lines = File.ReadAllLines(_csvFilePath, Encoding.UTF8);
                
                if (lines.Length == 0)
                {
                    EditorUtility.DisplayDialog("오류", "CSV 파일이 비어있습니다.", "확인");
                    return;
                }

                int startIndex = _csvHasHeader ? 1 : 0; // 헤더가 있으면 1행부터, 없으면 0행부터

                for (int i = startIndex; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        skippedCount++;
                        continue;
                    }

                    // CSV 파싱 (쉼표로 구분, 따옴표 처리)
                    string[] columns = ParseCsvLine(line);
                    
                    if (columns.Length < 1)
                    {
                        skippedCount++;
                        continue;
                    }

                    string searchValue = columns[0]?.Trim() ?? string.Empty;
                    string replaceValue = columns.Length > 1 ? (columns[1]?.Trim() ?? string.Empty) : string.Empty;

                    // Search 값이 없으면 스킵
                    if (string.IsNullOrEmpty(searchValue))
                    {
                        skippedCount++;
                        continue;
                    }

                    loadedMappings.Add(new MappingEntry
                    {
                        Enabled = true,
                        Search = searchValue,
                        Replace = replaceValue
                    });
                    loadedCount++;
                }

                if (loadedCount == 0)
                {
                    EditorUtility.DisplayDialog("알림", "CSV 파일에서 유효한 매핑 데이터를 찾을 수 없습니다.", "확인");
                    return;
                }

                // 기존 매핑에 추가할지 물어보기
                bool addToExisting = EditorUtility.DisplayDialog(
                    "매핑 불러오기",
                    $"{loadedCount}개의 매핑을 불러왔습니다.\n" +
                    $"스킵된 행: {skippedCount}개\n\n" +
                    "기존 매핑에 추가하시겠습니까? (아니오를 선택하면 기존 매핑을 모두 교체합니다.)",
                    "추가", "교체");

                if (!addToExisting)
                {
                    _mappings.Clear();
                }

                _mappings.AddRange(loadedMappings);

                EditorUtility.DisplayDialog("완료", $"매핑을 성공적으로 불러왔습니다.\n\n불러온 매핑: {loadedCount}개", "확인");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("오류", $"CSV 파일을 읽는 중 오류가 발생했습니다:\n\n{e.Message}", "확인");
                Debug.LogError($"CSV Load Error: {e}");
            }
        }

        /// <summary>
        /// CSV 라인을 파싱합니다. 따옴표로 감싸진 필드도 처리합니다.
        /// </summary>
        private string[] ParseCsvLine(string line)
        {
            List<string> fields = new List<string>();
            bool inQuotes = false;
            StringBuilder currentField = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // 이스케이프된 따옴표 ("")
                        currentField.Append('"');
                        i++; // 다음 따옴표 건너뛰기
                    }
                    else
                    {
                        // 따옴표 시작/끝
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // 필드 구분자
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            // 마지막 필드 추가
            fields.Add(currentField.ToString());

            return fields.ToArray();
        }

        /// <summary>
        /// 매핑 리스트 UI
        /// </summary>
        private void DrawMappingsList()
        {
            EditorGUILayout.LabelField("교체 매핑 (다수 적용 가능)", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 스크롤뷰 추가
            _mappingScrollPosition = EditorGUILayout.BeginScrollView(_mappingScrollPosition, GUILayout.Height(150));

            for (int i = 0; i < _mappings.Count; i++)
            {
                var mapping = _mappings[i];

                EditorGUILayout.BeginHorizontal();

                // 매칭 파일 수 표시 (좌측)
                string countText = _matchedFiles.Count > 0 ? $"({mapping.MatchCount})" : "";
                EditorGUILayout.LabelField(countText, GUILayout.Width(50));
                
                mapping.Enabled = EditorGUILayout.Toggle(mapping.Enabled, GUILayout.Width(20));
                mapping.Search = EditorGUILayout.TextField(mapping.Search, GUILayout.Width(150));
                EditorGUILayout.LabelField("→", GUILayout.Width(15));
                mapping.Replace = EditorGUILayout.TextField(mapping.Replace, GUILayout.Width(150));

                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    _mappings.RemoveAt(i);
                    i--;
                    continue;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

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
        ///     "EffectCodeSkill1304021"에서 "1304021"을 찾으면 true (숫자-문자 경계 인식)
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
            
            // 정규식을 사용하여 구분자(언더스코어, 하이픈, 공백, 점 등) 또는 숫자-문자 경계 앞뒤에서 정확히 일치하는지 확인
            // 패턴: 구분자, 숫자-문자 경계, 대소문자 경계 앞뒤 또는 문자열 시작/끝에서 정확히 일치
            // (?<=...) : lookbehind (앞에 오는 것)
            // (?=...) : lookahead (뒤에 오는 것)
            // \d : 숫자, \D : 비숫자, [a-z] : 소문자, [A-Z] : 대문자
            string escapedSearch = Regex.Escape(searchString);
            
            // 경계 조건: 문자열 시작/끝, 구분자, 숫자-문자 경계, 대소문자 경계
            // 앞 경계: ^ 또는 구분자 또는 (숫자 다음 비숫자) 또는 (비숫자 다음 숫자) 또는 (소문자 다음 대문자)
            // 뒤 경계: $ 또는 구분자 또는 (숫자 다음 비숫자) 또는 (비숫자 다음 숫자) 또는 (소문자 다음 대문자)
            string beforeBoundary = @"(?:^|[_\-\.\s\(\)\[\]]|(?<=\d)(?=\D)|(?<=\D)(?=\d)|(?<=[a-z])(?=[A-Z]))";
            string afterBoundary = @"(?:(?=\d)(?<=\D)|(?=\D)(?<=\d)|(?=[a-z])(?<=[A-Z])|[_\-\.\s\(\)\[\]]|$)";
            string pattern = beforeBoundary + escapedSearch + afterBoundary;
            
            return Regex.IsMatch(nameWithoutExtension, pattern);
        }

        /// <summary>
        /// 파일명에서 검색 문자열을 정확히 일치하는 부분만 교체
        /// 구분자(언더스코어, 하이픈 등) 또는 숫자-문자 경계로 구분된 부분만 교체하여 부분 문자열 교체를 방지
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
            // 구분자(언더스코어, 하이픈, 공백, 점 등) 또는 숫자-문자 경계 앞뒤에서 일치하는 경우만 교체
            
            // 패턴: 구분자, 숫자-문자 경계, 대소문자 경계 앞뒤 또는 문자열 시작/끝에서 정확히 일치
            string escapedSearch = Regex.Escape(searchString);
            
            // 경계 조건: 문자열 시작/끝, 구분자, 숫자-문자 경계, 대소문자 경계
            // 앞 경계: ^ 또는 구분자 또는 (숫자 다음 비숫자) 또는 (비숫자 다음 숫자) 또는 (소문자 다음 대문자)
            // 뒤 경계: $ 또는 구분자 또는 (숫자 다음 비숫자) 또는 (비숫자 다음 숫자) 또는 (소문자 다음 대문자)
            // 실제 구분자 문자는 그룹으로 캡처 (교체 시 사용)
            string beforeBoundary = @"(?:(^|[_\-\.\s\(\)\[\]])|(?<=\d)(?=\D)|(?<=\D)(?=\d)|(?<=[a-z])(?=[A-Z]))";
            string afterBoundary = @"(?:(?=\d)(?<=\D)|(?=\D)(?<=\d)|(?=[a-z])(?<=[A-Z])|([_\-\.\s\(\)\[\]]|$))";

            
            string pattern = beforeBoundary + escapedSearch + afterBoundary;
            
            // MatchEvaluator를 사용하여 $ 문자 문제 해결
            // lookahead/lookbehind는 그룹에 캡처되지 않으므로, 실제 구분자만 처리
            string result = Regex.Replace(nameWithoutExtension, pattern, match =>
            {
                // Groups[1]: 앞의 실제 구분자 (^ 또는 구분자 문자) - 있으면 사용, 없으면 빈 문자열
                // Groups[2]: 뒤의 실제 구분자 (구분자 문자 또는 $) - 있으면 사용, 없으면 빈 문자열
                string before = "";
                string after = "";
                
                // 앞 구분자 확인 (Groups[1])
                if (match.Groups[1].Success && match.Groups[1].Length > 0)
                {
                    before = match.Groups[1].Value;
                }
                
                // 뒤 구분자 확인 (Groups[2])
                if (match.Groups[2].Success && match.Groups[2].Length > 0)
                {
                    after = match.Groups[2].Value;
                }
                
                return before + replaceString + after;
            });
            
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
            public int MatchCount = 0; // 매칭되는 파일 수
        }

        private void ReplaceFileNames()
        {
            int successCount = 0;
            int failCount = 0;
            List<string> failedFiles = new List<string>();

            // 1단계: 모든 파일의 새 이름을 미리 계산 (원본 파일명 기준)
            Dictionary<string, string> renameMap = new Dictionary<string, string>();
            HashSet<string> newNames = new HashSet<string>();
            List<string> duplicateNewNames = new List<string>();

            foreach (var filePath in _matchedFiles)
            {
                try
                {
                    // 현재 파일이 실제로 존재하는지 확인 (이미 변경되었을 수 있음)
                    if (!File.Exists(filePath) && !Directory.Exists(filePath) && AssetDatabase.LoadAssetAtPath<Object>(filePath) == null)
                    {
                        // 파일이 이미 변경되었거나 삭제된 경우, GUID로 다시 찾기 시도
                        string guid = AssetDatabase.AssetPathToGUID(filePath);
                        if (string.IsNullOrEmpty(guid))
                        {
                            failedFiles.Add($"{filePath} (파일을 찾을 수 없음)");
                            continue;
                        }
                        // GUID로 찾은 경로는 이미 변경된 경로일 수 있으므로 스킵
                        continue;
                    }

                    string directory = Path.GetDirectoryName(filePath).Replace('\\', '/');
                    string originalName = Path.GetFileName(filePath);
                    
                    // 원본 파일명 기준으로 새 이름 계산
                    if (!TryApplyMappings(originalName, out var newName) || originalName == newName)
                    {
                        continue;
                    }
                    
                    string newPath = $"{directory}/{newName}";

                    // 새 이름이 이미 다른 파일의 새 이름과 중복되는지 확인
                    if (newNames.Contains(newPath))
                    {
                        duplicateNewNames.Add(newPath);
                        failedFiles.Add($"{filePath} → {newName} (다른 파일과 새 이름 충돌)");
                        continue;
                    }

                    // 새 이름이 이미 존재하는 파일/폴더와 충돌하는지 확인
                    if (File.Exists(newPath) || Directory.Exists(newPath) || AssetDatabase.LoadAssetAtPath<Object>(newPath) != null)
                    {
                        // 충돌하는 파일이 현재 리스트에 있는지 확인
                        bool isInList = false;
                        foreach (var otherPath in _matchedFiles)
                        {
                            if (otherPath == newPath)
                            {
                                isInList = true;
                                break;
                            }
                        }
                        
                        if (!isInList)
                        {
                            failedFiles.Add($"{filePath} → {newName} (이미 존재하는 이름)");
                            continue;
                        }
                    }

                    renameMap[filePath] = newPath;
                    newNames.Add(newPath);
                }
                catch (System.Exception e)
                {
                    failedFiles.Add($"{filePath} (예외: {e.Message})");
                }
            }

            // 중복된 새 이름이 있으면 해당 항목들 모두 제거
            if (duplicateNewNames.Count > 0)
            {
                var toRemove = new List<string>();
                foreach (var kvp in renameMap)
                {
                    if (duplicateNewNames.Contains(kvp.Value))
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
                foreach (var key in toRemove)
                {
                    renameMap.Remove(key);
                }
            }

            // 2단계: 실제 파일명 변경 실행
            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (var kvp in renameMap)
                {
                    try
                    {
                        string originalPath = kvp.Key;
                        string newPath = kvp.Value;

                        // 파일이 여전히 존재하는지 다시 확인
                        if (!File.Exists(originalPath) && !Directory.Exists(originalPath) && AssetDatabase.LoadAssetAtPath<Object>(originalPath) == null)
                        {
                            failedFiles.Add($"{originalPath} (변경 전 파일이 사라짐)");
                            failCount++;
                            continue;
                        }

                        // 새 경로가 이미 존재하는지 다시 확인
                        if (File.Exists(newPath) || Directory.Exists(newPath) || AssetDatabase.LoadAssetAtPath<Object>(newPath) != null)
                        {
                            // 자기 자신이 아닌 경우에만 실패 처리
                            if (originalPath != newPath)
                            {
                                failedFiles.Add($"{originalPath} → {Path.GetFileName(newPath)} (이미 존재하는 이름)");
                                failCount++;
                                continue;
                            }
                        }

                        // 파일/폴더명 변경
                        string error = AssetDatabase.MoveAsset(originalPath, newPath);
                        if (string.IsNullOrEmpty(error))
                        {
                            successCount++;
                        }
                        else
                        {
                            failedFiles.Add($"{originalPath} (에러: {error})");
                            failCount++;
                        }
                    }
                    catch (System.Exception e)
                    {
                        failedFiles.Add($"{kvp.Key} (예외: {e.Message})");
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
            string message = $"항명명 교체 완료!\n\n성공: {successCount}개\n실패: {failCount}개";
            if (failedFiles.Count > 0)
            {
                message += "\n\n실패한 항목:\n";
                foreach (var failed in failedFiles)
                {
                    message += $"- {failed}\n";
                }
            }

            EditorUtility.DisplayDialog("항명명 교체 결과", message, "확인");

            // 검색 결과 초기화
            _matchedFiles.Clear();
        }
    }
}

