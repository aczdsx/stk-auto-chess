#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CookApps.AutoBattler;
using CookApps.AutoChess.View;

namespace CookApps.AutoChess.Editor
{
    /// <summary>
    /// InGameTileView → BoardTileView 마이그레이션 에디터 툴.
    /// Project 창에서 프리팹을 선택한 뒤 메뉴 실행.
    /// InGameStage → BoardGridView도 함께 마이그레이션.
    ///
    /// 사용 순서:
    /// 1) InGameTileView.prefab (소스 타일 프리팹) 선택 → 마이그레이션
    /// 2) Stage 프리팹 선택 → 마이그레이션 (네스티드 타일은 소스에서 이미 변환된 BoardTileView 사용)
    /// </summary>
    public static class InGameTileViewMigrator
    {
        [MenuItem("Tools/AutoChess/Migrate InGameTileView → BoardTileView (Dry Run)")]
        public static void DryRun()
        {
            Migrate(dryRun: true);
        }

        [MenuItem("Tools/AutoChess/Migrate InGameTileView → BoardTileView")]
        public static void Run()
        {
            var prefabs = GetSelectedPrefabs();
            if (prefabs.Length == 0)
            {
                Debug.LogWarning("[마이그레이션] Project 창에서 프리팹을 선택해주세요.");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "InGameTileView 마이그레이션",
                $"선택된 {prefabs.Length}개 프리팹의 InGameTileView를 BoardTileView로 마이그레이션합니다.\n" +
                "InGameStage도 BoardGridView로 변환합니다.\n\n" +
                "계속하시겠습니까?",
                "실행", "취소"))
            {
                return;
            }

            Migrate(dryRun: false);
        }

        [MenuItem("Tools/AutoChess/Migrate InGameTileView → BoardTileView (Dry Run)", true)]
        [MenuItem("Tools/AutoChess/Migrate InGameTileView → BoardTileView", true)]
        private static bool Validate()
        {
            return GetSelectedPrefabs().Length > 0;
        }

        private static string[] GetSelectedPrefabs()
        {
            var prefabPaths = new List<string>();

            foreach (var obj in Selection.objects)
            {
                if (obj is GameObject go)
                {
                    string path = AssetDatabase.GetAssetPath(go);
                    if (!string.IsNullOrEmpty(path) && path.EndsWith(".prefab"))
                        prefabPaths.Add(path);
                }
            }

            return prefabPaths.ToArray();
        }

        private static void Migrate(bool dryRun)
        {
            string[] prefabPaths = GetSelectedPrefabs();
            if (prefabPaths.Length == 0)
            {
                Debug.LogWarning("프리팹을 선택해주세요.");
                return;
            }

            int totalTiles = 0;
            int totalStages = 0;
            int totalPrefabs = 0;

            foreach (string path in prefabPaths)
            {
                MigratePrefab(path, dryRun, out int tiles, out int stages);
                if (tiles > 0 || stages > 0)
                {
                    totalPrefabs++;
                    totalTiles += tiles;
                    totalStages += stages;
                }
            }

            string mode = dryRun ? "[DRY RUN] " : "";
            Debug.Log($"{mode}마이그레이션 완료: {totalPrefabs}/{prefabPaths.Length}개 프리팹, " +
                      $"{totalTiles}개 타일뷰, {totalStages}개 스테이지 변환");

            if (!dryRun)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static void MigratePrefab(string prefabPath, bool dryRun,
            out int migratedTiles, out int migratedStages)
        {
            migratedTiles = 0;
            migratedStages = 0;

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null) return;

            var oldTileViews = prefabAsset.GetComponentsInChildren<InGameTileView>(true);
            var existingBoardTiles = prefabAsset.GetComponentsInChildren<BoardTileView>(true);
            var oldStage = prefabAsset.GetComponentInChildren<InGameStage>(true);
            bool hasBoardGridView = prefabAsset.GetComponentInChildren<BoardGridView>(true) != null;

            if (oldTileViews.Length == 0 && existingBoardTiles.Length == 0 && oldStage == null)
            {
                Debug.Log($"변환 대상 없음 — 스킵: {prefabPath}");
                return;
            }

            // InGameStage가 있는데 BoardGridView가 이미 있으면 스킵
            if (oldStage == null && hasBoardGridView && oldTileViews.Length == 0)
            {
                Debug.Log($"이미 마이그레이션 완료 — 스킵: {prefabPath}");
                return;
            }

            string mode = dryRun ? "[DRY RUN] " : "";
            Debug.Log($"{mode}프리팹 처리 중: {prefabPath} " +
                      $"(InGameTileView: {oldTileViews.Length}개, BoardTileView: {existingBoardTiles.Length}개, " +
                      $"InGameStage: {(oldStage != null ? "있음" : "없음")})");

            // ── Dry Run ──
            if (dryRun)
            {
                migratedTiles = oldTileViews.Length > 0 ? oldTileViews.Length : existingBoardTiles.Length;

                if (oldStage != null)
                {
                    var so = new SerializedObject(oldStage);
                    var gridSize = so.FindProperty("_gridSize");
                    int w = gridSize.FindPropertyRelative("x").intValue;
                    int h = gridSize.FindPropertyRelative("y").intValue;
                    bool canMigrate = migratedTiles > 0;
                    Debug.Log($"  InGameStage: gridSize=({w}, {h}) → " +
                              $"{(canMigrate ? "BoardGridView 변환 예정" : "타일 없음")}");
                    if (canMigrate)
                        migratedStages = 1;
                }
                return;
            }

            // ── 소스 프리팹 자동 마이그레이션 ──
            if (oldTileViews.Length > 0)
                AutoMigrateSourcePrefabs(prefabAsset);

            // ── 실제 마이그레이션 ──
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                var tileViews = prefabRoot.GetComponentsInChildren<InGameTileView>(true);

                // 1) InGameTileView → BoardTileView 마이그레이션
                var migrationMap = new Dictionary<InGameTileView, BoardTileView>();
                foreach (var oldView in tileViews)
                {
                    if (IsNestedPrefabOriginal(oldView))
                    {
                        if (oldView.gameObject.TryGetComponent<BoardTileView>(out var existing))
                        {
                            migrationMap[oldView] = existing;
                            migratedTiles++;
                        }
                        continue;
                    }

                    var newView = MigrateTileView(oldView);
                    if (newView != null)
                    {
                        migrationMap[oldView] = newView;
                        migratedTiles++;
                    }
                }

                // 2) InGameStage → BoardGridView
                var stage = prefabRoot.GetComponentInChildren<InGameStage>(true);
                if (stage != null && !IsNestedPrefabOriginal(stage))
                {
                    if (migrationMap.Count > 0)
                    {
                        // InGameTileView 매핑으로 변환
                        MigrateStage(stage, migrationMap);
                        Object.DestroyImmediate(stage);
                        migratedStages = 1;
                    }
                    else
                    {
                        // InGameTileView가 이미 제거됨 → BoardTileView 직접 사용
                        var boardTiles = prefabRoot.GetComponentsInChildren<BoardTileView>(true);
                        if (boardTiles.Length > 0)
                        {
                            MigrateStageDirect(stage, boardTiles);
                            Object.DestroyImmediate(stage);
                            migratedTiles = boardTiles.Length;
                            migratedStages = 1;
                        }
                        else
                        {
                            Debug.LogWarning($"  InGameStage 변환 보류: 매핑 가능한 타일이 없음");
                        }
                    }
                }

                // 3) 기존 InGameTileView 제거 (네스티드 제외)
                foreach (var oldView in tileViews)
                {
                    if (!IsNestedPrefabOriginal(oldView))
                        Object.DestroyImmediate(oldView);
                }

                // 변경사항이 있을 때만 저장
                if (migratedTiles > 0 || migratedStages > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                    Debug.Log($"  저장 완료: 타일 {migratedTiles}개, 스테이지 {migratedStages}개 변환됨");
                }
                else
                {
                    Debug.LogWarning($"  변경사항 없음 — 저장 스킵");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        /// <summary>
        /// 네스티드 InGameTileView의 소스 프리팹을 찾아 BoardTileView가 없으면 자동 추가.
        /// LoadPrefabContents 전에 호출해야 반영됨.
        /// </summary>
        private static void AutoMigrateSourcePrefabs(GameObject prefabAsset)
        {
            var migratedPaths = new HashSet<string>();
            var tileViews = prefabAsset.GetComponentsInChildren<InGameTileView>(true);

            foreach (var tv in tileViews)
            {
                if (!PrefabUtility.IsPartOfPrefabInstance(tv)) continue;

                // 소스 프리팹의 원본 컴포넌트 가져오기
                var sourceComp = PrefabUtility.GetCorrespondingObjectFromSource(tv);
                if (sourceComp == null) continue;

                string sourcePath = AssetDatabase.GetAssetPath(sourceComp);
                if (string.IsNullOrEmpty(sourcePath) || migratedPaths.Contains(sourcePath)) continue;
                migratedPaths.Add(sourcePath);

                // 이미 BoardTileView가 있으면 스킵
                if (sourceComp.gameObject.TryGetComponent<BoardTileView>(out _)) continue;

                Debug.Log($"  소스 프리팹 자동 마이그레이션: {sourcePath}");

                // 소스 프리팹 열어서 BoardTileView 추가
                GameObject sourceRoot = PrefabUtility.LoadPrefabContents(sourcePath);
                try
                {
                    var sourceView = sourceRoot.GetComponent<InGameTileView>();
                    if (sourceView != null)
                    {
                        MigrateTileView(sourceView);
                        PrefabUtility.SaveAsPrefabAsset(sourceRoot, sourcePath);
                        Debug.Log($"    BoardTileView 추가 완료");
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(sourceRoot);
                }
            }

            if (migratedPaths.Count > 0)
                AssetDatabase.SaveAssets();
        }

        private static bool IsNestedPrefabOriginal(Component comp)
        {
            return PrefabUtility.IsPartOfPrefabInstance(comp)
                && !PrefabUtility.IsAddedComponentOverride(comp);
        }

        private static BoardTileView MigrateTileView(InGameTileView oldView)
        {
            GameObject go = oldView.gameObject;

            if (go.TryGetComponent<BoardTileView>(out var existing))
            {
                Debug.LogWarning($"  이미 BoardTileView 존재: {go.name} — 스킵");
                return existing;
            }

            var oldSO = new SerializedObject(oldView);
            var oldBoardSprite = oldSO.FindProperty("_boardSprite").objectReferenceValue as SpriteRenderer;
            var oldActiveObj = oldSO.FindProperty("_activeObj").objectReferenceValue as GameObject;
            var oldAttackActiveObj = oldSO.FindProperty("_attackActiveObj").objectReferenceValue as GameObject;
            var oldNavigateObj = oldSO.FindProperty("_commanderSkillNavigateObj").objectReferenceValue as GameObject;

            var newView = go.AddComponent<BoardTileView>();

            var newSO = new SerializedObject(newView);
            newSO.FindProperty("_boardSprite").objectReferenceValue = oldBoardSprite;
            newSO.FindProperty("_activeObj").objectReferenceValue = oldActiveObj;
            newSO.FindProperty("_attackActiveObj").objectReferenceValue = oldAttackActiveObj;
            newSO.FindProperty("_skillNavigateObj").objectReferenceValue = oldNavigateObj;
            newSO.ApplyModifiedPropertiesWithoutUndo();

            return newView;
        }

        /// <summary>
        /// InGameTileView가 이미 제거된 경우: BoardTileView를 직접 사용하여 BoardGridView 생성.
        /// InGameStage._gridSize로 보드 설정, _tileViews 대신 BoardTileView[] 직접 할당.
        /// </summary>
        private static void MigrateStageDirect(InGameStage stage, BoardTileView[] boardTiles)
        {
            GameObject go = stage.gameObject;

            if (go.TryGetComponent<BoardGridView>(out _))
            {
                Debug.LogWarning($"  이미 BoardGridView 존재: {go.name} — 스킵");
                return;
            }

            var oldSO = new SerializedObject(stage);
            var gridSizeProp = oldSO.FindProperty("_gridSize");
            int boardWidth = gridSizeProp.FindPropertyRelative("x").intValue;
            int boardHeight = gridSizeProp.FindPropertyRelative("y").intValue;

            var gridView = go.AddComponent<BoardGridView>();
            var newSO = new SerializedObject(gridView);

            var tilesProp = newSO.FindProperty("_tiles");
            tilesProp.arraySize = boardTiles.Length;
            for (int i = 0; i < boardTiles.Length; i++)
            {
                tilesProp.GetArrayElementAtIndex(i).objectReferenceValue = boardTiles[i];
            }

            newSO.FindProperty("_boardWidth").intValue = boardWidth;
            newSO.FindProperty("_boardHeight").intValue = boardHeight;

            int combatHeight = boardHeight * 2;
            int tilesPerBoard = boardWidth * combatHeight;
            int boardCount = tilesPerBoard > 0 ? boardTiles.Length / tilesPerBoard : 1;
            if (boardCount < 1) boardCount = 1;
            newSO.FindProperty("_boardCount").intValue = boardCount;

            newSO.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"  BoardGridView 생성 (직접 매핑): {boardWidth}x{boardHeight}, {boardCount}보드, {boardTiles.Length}개 타일");
        }

        private static void MigrateStage(InGameStage stage, Dictionary<InGameTileView, BoardTileView> migrationMap)
        {
            GameObject go = stage.gameObject;

            if (go.TryGetComponent<BoardGridView>(out _))
            {
                Debug.LogWarning($"  이미 BoardGridView 존재: {go.name} — 스킵");
                return;
            }

            var oldSO = new SerializedObject(stage);
            var gridSizeProp = oldSO.FindProperty("_gridSize");
            int boardWidth = gridSizeProp.FindPropertyRelative("x").intValue;
            int boardHeight = gridSizeProp.FindPropertyRelative("y").intValue;
            var oldTilesProp = oldSO.FindProperty("_tileViews");

            int tileCount = oldTilesProp.arraySize;
            var newTiles = new BoardTileView[tileCount];
            int mappedCount = 0;

            for (int i = 0; i < tileCount; i++)
            {
                var oldRef = oldTilesProp.GetArrayElementAtIndex(i).objectReferenceValue as InGameTileView;
                if (oldRef != null && migrationMap.TryGetValue(oldRef, out var newRef))
                {
                    newTiles[i] = newRef;
                    mappedCount++;
                }
            }

            var gridView = go.AddComponent<BoardGridView>();
            var newSO = new SerializedObject(gridView);

            var tilesProp = newSO.FindProperty("_tiles");
            tilesProp.arraySize = tileCount;
            for (int i = 0; i < tileCount; i++)
            {
                tilesProp.GetArrayElementAtIndex(i).objectReferenceValue = newTiles[i];
            }

            newSO.FindProperty("_boardWidth").intValue = boardWidth;
            newSO.FindProperty("_boardHeight").intValue = boardHeight;

            int combatHeight = boardHeight * 2;
            int tilesPerBoard = boardWidth * combatHeight;
            int boardCount = tilesPerBoard > 0 ? tileCount / tilesPerBoard : 1;
            if (boardCount < 1) boardCount = 1;
            newSO.FindProperty("_boardCount").intValue = boardCount;

            newSO.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"  BoardGridView 생성: {boardWidth}x{boardHeight}, {boardCount}보드, {mappedCount}/{tileCount} 타일 매핑됨");
        }
    }
}
#endif
