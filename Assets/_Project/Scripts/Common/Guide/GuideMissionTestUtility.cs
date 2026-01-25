using CookApps.AutoBattler;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using Tech.Hive.V1;

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
    public static int 아트레시아ID = 3401;
    public static int 코어기사공격력ID = 1101;
    public static int 바벨범위기준ID = 10000;
    public static int 스테이지범위기준ID = 20000;
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
        await HandleElpisFacilityUpgradeOnIterate();
        await HandleEnterChapter(0);
        await HandleCommandCenter(false, false, false);
        await HandleClearStage(GetStageId(), false);
        await UpgradeUnit();
        await HandleCoreUpgrade();
    }

    public static int GetStageId()
    {
#if _SJHONG_TEST_
        return (int)LocalDataManager.Instance.GetLastPlayStageId();
#else
        return (int)ServerDataManager.Instance.Battle.GetLatestClearedStageId();
#endif
    }

    public static async UniTask HandleCoreUpgrade()
    {
        if (!isInit) Init();

        // 406 미션: 디멘션 랩 사용 (코어 연구 - 기사 공격력 연구 upgrade_group_id = 1101)
        if (gdb.GuideMissionId == 406 && edb.GetCoreResearchLevel((uint)코어기사공격력ID) > 1) { if (!ClearFlags[406]) await AddActionAndClaim(406); }
    }

    public static async UniTask HandleElpisFacilityUpgradeOnIterate()
    {
        if (!isInit) Init();

        if (gdb.GuideMissionId == 201 && (edb.GetFacility((int)IdMap.ElpisBuild.Nest_1)?.IsJustCompleted == true) || edb.GetFacilityLevel(ElpisFacilityType.FacilityTypeNest) >= 1) { if (!ClearFlags[201]) await AddActionAndClaim(201); }
        if (gdb.GuideMissionId == 404 && (edb.GetFacility((int)IdMap.ElpisBuild.Nest_2)?.IsJustCompleted == true) || edb.GetFacilityLevel(ElpisFacilityType.FacilityTypeNest) >= 2) { if (!ClearFlags[404]) await AddActionAndClaim(404); }
        if (gdb.GuideMissionId == 405 && (edb.GetFacility((int)IdMap.ElpisBuild.DimensionLab)?.IsJustCompleted == true) || edb.GetFacilityLevel(ElpisFacilityType.FacilityTypeDimensionLab) >= 1) { if (!ClearFlags[405]) await AddActionAndClaim(405); }
        if (gdb.GuideMissionId == 407 && (edb.GetFacility((int)IdMap.ElpisBuild.SimulationCenter)?.IsJustCompleted == true) || edb.GetFacilityLevel(ElpisFacilityType.FacilityTypeSimulationCenter) >= 1) { if (!ClearFlags[407]) await AddActionAndClaim(407); }
    }

    public static async UniTask HandleEnterChapter(int stageId)
    {
        if (gdb.GuideMissionId == 501 && stageId >= 30000) { if (!ClearFlags[501]) await AddActionAndClaim(501); }
    }

    public static async UniTask HandleElpisFacilityUpgrade()
    {
        if (!isInit) Init();

        if (gdb.GuideMissionId == 201 && edb.IsBuildedFacilityExists((uint)IdMap.ElpisBuild.Nest_1)) { if (!ClearFlags[201]) await AddActionAndClaim(201); }
        if (gdb.GuideMissionId == 404 && edb.IsBuildedFacilityExists((uint)IdMap.ElpisBuild.Nest_2)) { if (!ClearFlags[404]) await AddActionAndClaim(404); }
        if (gdb.GuideMissionId == 405 && edb.IsBuildedFacilityExists((uint)IdMap.ElpisBuild.DimensionLab)) { if (!ClearFlags[405]) await AddActionAndClaim(405); }
        if (gdb.GuideMissionId == 407 && edb.IsBuildedFacilityExists((uint)IdMap.ElpisBuild.SimulationCenter)) { if (!ClearFlags[407]) await AddActionAndClaim(407); }
    }

    public static async UniTask HandleCommandCenter(bool isEnterCommandCenter, bool isEnterDimension, bool isEnterBattleSim)
    {
        if (!isInit) Init();

        if (gdb.GuideMissionId == 401 && isEnterCommandCenter) { if (!ClearFlags[401]) await AddActionAndClaim(401); }
        if (gdb.GuideMissionId == 403 && edb.GetFacilityLevel(Tech.Hive.V1.ElpisFacilityType.FacilityTypeCommandCenter) > 1) { if (!ClearFlags[403]) await AddActionAndClaim(403); }
    }

    public static async UniTask HandleBable(int bableStateID)
    {
        if (!isInit) Init();
        if(!(바벨범위기준ID <= bableStateID && bableStateID <= 스테이지범위기준ID))
        {
            Debug.LogError($"잘못된 바벨 ID를 가져오고 있음 {bableStateID}");
                    return;
        }

        // 연속 미션 클리어를 위해 while 루프 사용
        bool processed;
        do
        {
            processed = false;
            int currentMissionId = (int)gdb.GuideMissionId;

            // CLEAR_BABEL_TOWER 미션 처리
            if (CLEAR_BABEL_TOWER_GUIDE_ID.Contains(currentMissionId))
            {
                if (GuideMissionTables.TryGetValue(currentMissionId, out var data) &&
                    bableStateID > data.Subkey &&
                    !ClearFlags[currentMissionId])
                {
                    await AddActionAndClaim(currentMissionId);
                    processed = true;
                    continue;
                }
            }
        } while (processed);
    }
    public static async UniTask HandleClearStage(int stageId, bool onceDamUp)
    {
        if (!isInit) Init();


        if(stageId <= 스테이지범위기준ID)
        {
            Debug.LogError($"잘못된 스테이지 ID를 가져오고 있음 {stageId}");
            return;
        }
        // 연속 미션 클리어를 위해 while 루프 사용
        bool processed;
        do
        {
            processed = false;
            int currentMissionId = (int)gdb.GuideMissionId;

            // CLEAR_STAGE_1 미션 처리
            if (CLEAR_STAGE_1_GUIDE_ID.Contains(currentMissionId))
            {
                if (GuideMissionTables.TryGetValue(currentMissionId, out var data) &&
                    stageId > data.Subkey &&
                    !ClearFlags[currentMissionId])
                {
                    await AddActionAndClaim(currentMissionId);
                    processed = true;
                    continue;
                }
            }

            // CLEAR_STAGE_2 미션 처리
            if (CLEAR_STAGE_2_GUIDE_ID.Contains(currentMissionId))
            {
                if (GuideMissionTables.TryGetValue(currentMissionId, out var data) &&
                    stageId > data.Subkey &&
                    !ClearFlags[currentMissionId])
                {
                    await AddActionAndClaim(currentMissionId);
                    processed = true;
                    continue;
                }
            }
        } while (processed);
    }

    public static async UniTask UpgradeUnit()
    {
        if (!isInit) Init();
        var charModel = ServerDataManager.Instance.Character.GetCharacter(아트레시아ID);
        if (gdb.GuideMissionId == 202 && charModel.Level > 1) await AddActionAndClaim(202);
        if (gdb.GuideMissionId == 305 && charModel.ExceedLevel > 0) await AddActionAndClaim(305);
        if (gdb.GuideMissionId == 402 && charModel.TranscendLevel > 3) await AddActionAndClaim(402);
    }
}
