using CookApps.AutoBattler;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Tech.Hive.V1;
using System;

// ! GUIDE_TODO
/// <summary>
/// 하드코딩된 유틸리티 ID이므로 가이드 미션의 클리어 조건을 받아 올 수 있지 않을까?
/// 
/// </summary>
[Obsolete]
public static class GuideMissionConstants
{
    [Obsolete] public const int 아트레시아ID = 3401;
    [Obsolete] public const int 코어기사공격력ID = 1101;
    [Obsolete] public const int 바벨범위기준ID = 10000;
    [Obsolete] public const int 스테이지범위기준ID = 20000;
    [Obsolete] public const int 챕터2기준ID = 30000;
    [Obsolete] public const int 커맨드센터들어간가이드미션ID = 401;
}

/// <summary>
/// 하드코딩된 유틸리티 ID이므로 앞으로 서버한테 의존하도록 합시다.
/// </summary>
[Obsolete]
public static class GuideMissionTestUtility
{
    public static Dictionary<int, (GuideMissionType MissionType, int TargetCount, int Subkey)> GuideMissionTables;
    public static Dictionary<int, bool> ClearFlags;
    public static List<int> SUMMON_CHARACTER_GUIDE_ID = new();
    public static List<int> INSTALL_BUILDINGS_NEST_GUIDE_ID = new();
    public static List<int> INSTALL_BUILDINGS_DIMENSION_GUIDE_ID = new();
    public static List<int> CHARACTER_LEVEL_UP = new();
    public static List<int> CLEAR_STAGE_1_GUIDE_ID = new();
    public static List<int> CLEAR_STAGE_2_GUIDE_ID = new();
    public static List<int> USE_BUILDING_COMMAND_CENTER_GUIDE_ID = new();
    public static List<int> USE_BUILDING_DIMENSION_GUIDE_ID = new();
    public static List<int> CLEAR_BABEL_TOWER_GUIDE_ID = new();
    private static bool isInit = false;

    private static readonly HashSet<int> _processingGids = new HashSet<int>();

    public static GuideMissionDataBridge gdb;
    public static ElpisDataBridge edb;

    public static void Init()
    {
        if (isInit) return;
        GuideMissionTables = null;
        gdb = new GuideMissionDataBridge();
        edb = new ElpisDataBridge();
        GuideMissionTables = new Dictionary<int, (GuideMissionType MissionType, int TargetCount, int Subkey)>
        {
            { 101, (GuideMissionType.SUMMON_CHARCTER, 1,0) },
            { 201, (GuideMissionType.INSTALL_BUILDING, 1,0) },
            { 202, (GuideMissionType.CLEAR_TUTORIAL, 1,0) },
            { 301, (GuideMissionType.CLEAR_STAGE, 99,20001) },
            { 302, (GuideMissionType.CLEAR_STAGE, 99,20002) },
            { 303, (GuideMissionType.CLEAR_STAGE, 99,20003) },
            { 304, (GuideMissionType.CLEAR_STAGE, 99,20004) },
            { 305, (GuideMissionType.CLEAR_TUTORIAL, 1,0) },
            { 306, (GuideMissionType.CLEAR_STAGE, 99,20005) },
            { 307, (GuideMissionType.CLEAR_STAGE, 99,20006) },
            { 308, (GuideMissionType.CLEAR_STAGE, 99,20007) },
            { 309, (GuideMissionType.CLEAR_STAGE, 99,20008) },
            { 310, (GuideMissionType.CLEAR_STAGE, 99,20009) },
            { 401, (GuideMissionType.USE_BUILDING, 1,0) },
            { 402, (GuideMissionType.CLEAR_TUTORIAL, 1,0) },
            { 403, (GuideMissionType.CLEAR_TUTORIAL, 1,0) },
            { 404, (GuideMissionType.CLEAR_TUTORIAL, 1,0) },
            { 405, (GuideMissionType.CLEAR_TUTORIAL, 1,0) },
            { 406, (GuideMissionType.CLEAR_TUTORIAL, 1,0) },
            { 407, (GuideMissionType.CLEAR_TUTORIAL, 1,0) },
            { 501, (GuideMissionType.ENTER_CHAPTER, 1,0) },
            { 502, (GuideMissionType.CLEAR_STAGE, 99,30001) },
            { 503, (GuideMissionType.CLEAR_STAGE, 99,30002) },
            { 504, (GuideMissionType.CLEAR_STAGE, 99,30005) },
            { 505, (GuideMissionType.CLEAR_BABEL, 1,10001) },
            { 506, (GuideMissionType.CLEAR_BABEL, 1,10004) },
            { 507, (GuideMissionType.CLEAR_STAGE, 99,30006) },
            { 508, (GuideMissionType.CLEAR_STAGE, 99,30009) },
            { 509, (GuideMissionType.CLEAR_STAGE, 99,30012) },
            { 601, (GuideMissionType.CLEAR_BABEL, 1,10013) },
        };
        ClearFlags = new Dictionary<int, bool>
        {
            { 101, false },
            { 201, false },
            { 202, false },
            { 301, false },
            { 302, false },
            { 303, false },
            { 304, false },
            { 305, false },
            { 306, false },
            { 307, false },
            { 308, false },
            { 309, false },
            { 310, false },
            { 401, false },
            { 402, false },
            { 403, false },
            { 404, false },
            { 405, false },
            { 406, false },
            { 407, false },
            { 501, false },
            { 502, false },
            { 503, false },
            { 504, false },
            { 505, false },
            { 506, false },
            { 507, false },
            { 508, false },
            { 509, false },
            { 601, false },
        };
        foreach (var key in new List<int>(ClearFlags.Keys))
        {
            if (gdb.GuideMissionId > key)
                ClearFlags[key] = true;
        }
        SUMMON_CHARACTER_GUIDE_ID = new() { 101 };
        INSTALL_BUILDINGS_NEST_GUIDE_ID = new() { 201, 404 };
        INSTALL_BUILDINGS_DIMENSION_GUIDE_ID = new() { 405 };
        CHARACTER_LEVEL_UP = new() { 202, 305, 402 };
        CLEAR_STAGE_1_GUIDE_ID = new() { 301, 302, 303, 304, 306, 307, 308, 309, 310 };
        CLEAR_STAGE_2_GUIDE_ID = new() { 502, 503, 504, 507, 508, 509 };
        USE_BUILDING_COMMAND_CENTER_GUIDE_ID = new() { 401, 403 };
        USE_BUILDING_DIMENSION_GUIDE_ID = new() { 406 };
        CLEAR_BABEL_TOWER_GUIDE_ID = new() { 505, 506, 601 };
        isInit = true;
    }

    public static async UniTask AddActionAndClaim(int gid)
    {
        if (ClearFlags[gid] || _processingGids.Contains(gid))
            return;

        _processingGids.Add(gid);
        try
        {
            (GuideMissionType missionType, int count, int subkey) data = GuideMissionTables[gid];
            await NetManager.Instance.GuideMission.UpdateActionAsync((uint)data.count);
            ClearFlags[gid] = true;
            Debug.LogColor($"SJH {data} {gid}", "cyan");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GuideMission] Failed gid={gid}: {e.Message}");
        }
        finally
        {
            _processingGids.Remove(gid);
        }
    }


    public static async UniTask HandleIteratively()
    {
        if (!isInit) Init();
// #if _SJHONG_TEST_
//         List<int> keyBuff = new();
//         foreach (var gid in CLEAR_STAGE_1_GUIDE_ID)
//         {
//             await gdb.AddActionAsync(GuideMissionType.CLEAR_STAGE, 1, GuideMissionTables[gid].Subkey);
//         }
// #endif
        // ! GUIDE_TODO
        // ! 201	2	INSTALL_BUILDING	GUIDE_MISSION_NAME_201	숙소 복구	20002	GUIDE_MISSION_DESC_201	0	1	GOLD	210001	200											
        // ! INSTALL_BUILDING
                var n1 = edb.GetFacility((int)IdMap.ElpisBuild.Nest_1)?.IsJustCompleted == true;
                var n2 = edb.GetFacilityLevel(ElpisFacilityType.FacilityTypeNest) >= 1;
        if (gdb.GuideMissionId == 201 && (n1 || n2))
        {
            await gdb.AddActionAsync(GuideMissionType.INSTALL_BUILDING, 1);
        }
        // ! GUIDE_TODO
        // ! 404	17	CLEAR_TUTORIAL	GUIDE_MISSION_NAME_404	숙소설치 가이드미션	30003	GUIDE_MISSION_DESC_404	0	1	GOLD	210001	200											
        // ! INSTALL_BUILDING
        var nn3 = edb.HasFacility(200102); Debug.Log($"nn3 {nn3}");
        if (gdb.GuideMissionId == 404 && (nn3))
        {
            await NetManager.Instance.GuideMission.UpdateActionAsync(1);
        }


        // ! GUIDE_TODO
        // ! 405	18	CLEAR_TUTORIAL	GUIDE_MISSION_NAME_405	디멘션 큐브 가이드 미션	30004	GUIDE_MISSION_DESC_405	0	1	GOLD	210001	200											
        // ! INSTALL_BUILDING
        var d2 = edb.GetFacilityLevel(ElpisFacilityType.FacilityTypeDimensionLab) >= 1; Debug.Log($"d2 {d2}");
        if (gdb.GuideMissionId == 405 && d2)
        {
            await NetManager.Instance.GuideMission.UpdateActionAsync(1);
        }


        // ! GUIDE_TODO
        // ! 407	20	CLEAR_TUTORIAL	GUIDE_MISSION_NAME_407	전투 시뮬레이션 센터 가이드 미션	30006	GUIDE_MISSION_DESC_407	0	1	GOLD	210001	200											
        // ! INSTALL_BUILDING
        var s2 = edb.GetFacilityLevel(ElpisFacilityType.FacilityTypeSimulationCenter) >= 1; Debug.Log($"s2 : {s2}");
        if (gdb.GuideMissionId == 407 &&s2)
        {
            await NetManager.Instance.GuideMission.UpdateActionAsync(1);
        }
        var commandCenterLevel = edb.GetFacilityLevel(ElpisFacilityType.FacilityTypeCommandCenter);
        if (gdb.GuideMissionId == 403 && (commandCenterLevel > 1)) { if (!ClearFlags[403]) await AddActionAndClaim(403); }
    }

}