using CookApps.AutoBattler;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public static class GuideMissionTestUtility  {
    public static Dictionary<int, (GuideMissionType MissionType, int TargetCount, int Subkey)> GuideMissionTables;
    public static Dictionary<int, bool> ClearFlags;
    public static int 아트레시아ID = 3401;
    private static bool isInit = false;

    // 동시성 제어: 현재 처리 중인 미션 ID 추적
    private static readonly HashSet<int> _processingGids = new HashSet<int>();

    public static GuideMissionDataBridge gdb = new GuideMissionDataBridge();
    public static ElpisDataBridge edb =  new ElpisDataBridge();

    public static void Init()
    {
        if(isInit) return;
        GuideMissionTables = null;
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
        isInit = true;
    }

    public static async UniTask AddActionAndClaim(int gid)
    {
        // 동시성 제어: 이미 처리 중이거나 완료된 미션은 스킵
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

    public static async UniTask HandleDimension()
    {
    
        // var attackDim = edb.GetCurrentDimensionCoreLabs().Select(E => E.dimension_type == DimensionType.STELLA)
        // if(gdb.GuideMissionId == 406 && ) ElpisDimensionLab
    }

    public static async UniTask HandleElpisFacilityUpgrade()
    {
        if(!isInit) Init();
    
        if(gdb.GuideMissionId == 201 && edb.GetFacility((int)IdMap.ElpisBuild.Nest_1).IsJustCompleted) {if(!ClearFlags[201]) await AddActionAndClaim(201);}
        if(gdb.GuideMissionId == 404 && edb.GetFacility((int)IdMap.ElpisBuild.Nest_2).IsJustCompleted) {if(!ClearFlags[404]) await AddActionAndClaim(404);}
    }

    public static async UniTask HandleCommandCenter(bool isEnterCommandCenter, bool OnClickExpensionButton, bool isEnterDimension, bool isEnterBattleSim) {
        if(!isInit) Init();
    
        if(gdb.GuideMissionId == 401 && isEnterCommandCenter) {if(!ClearFlags[401]) await AddActionAndClaim(401);}
        if(gdb.GuideMissionId == 403 && OnClickExpensionButton) {if(!ClearFlags[403]) await AddActionAndClaim(403);}
        if(gdb.GuideMissionId == 405 && isEnterDimension) {if(!ClearFlags[405]) await AddActionAndClaim(405);}
        if(gdb.GuideMissionId == 407 && isEnterBattleSim) {if(!ClearFlags[407]) await AddActionAndClaim(407);}
    }


    public static async UniTask HandleClearStage(int stageId, bool onceDamUp) {
        if(!isInit) Init();
    
        if(gdb.GuideMissionId == 301 && stageId == GuideMissionTables[301].Subkey) {if(!ClearFlags[301]) await AddActionAndClaim(301);} // 테스트 완료
        if(gdb.GuideMissionId == 302 && stageId == GuideMissionTables[302].Subkey) {if(!ClearFlags[302]) await AddActionAndClaim(302);} // 테스트 완료
        if(gdb.GuideMissionId == 303 && stageId == GuideMissionTables[303].Subkey) {if(!ClearFlags[303]) await AddActionAndClaim(303);} // 테스트 완료
        if(gdb.GuideMissionId == 304 && stageId == GuideMissionTables[304].Subkey) {if(!ClearFlags[304]) await AddActionAndClaim(304);} // 테스트 완료
        if(gdb.GuideMissionId == 306 && stageId == GuideMissionTables[306].Subkey) {if(!ClearFlags[306]) await AddActionAndClaim(306);} // 테스트 완료
        if(gdb.GuideMissionId == 307 && stageId == GuideMissionTables[307].Subkey) {if(!ClearFlags[307]) await AddActionAndClaim(307);} // 테스트 완료
        if(gdb.GuideMissionId == 308 && stageId == GuideMissionTables[308].Subkey) {if(!ClearFlags[308]) await AddActionAndClaim(308);} // 테스트 완료
        if(gdb.GuideMissionId == 309 && stageId == GuideMissionTables[309].Subkey) {if(!ClearFlags[309]) await AddActionAndClaim(309);} // 테스트 완료
        if(gdb.GuideMissionId == 310 && stageId == GuideMissionTables[310].Subkey) {if(!ClearFlags[310]) await AddActionAndClaim(310);} // 테스트 완료
        if(gdb.GuideMissionId == 502 && stageId == GuideMissionTables[502].Subkey) {if(!ClearFlags[502]) await AddActionAndClaim(502);}
        if(gdb.GuideMissionId == 503 && stageId == GuideMissionTables[503].Subkey) {if(!ClearFlags[503]) await AddActionAndClaim(503);}
        if(gdb.GuideMissionId == 504 && stageId == GuideMissionTables[504].Subkey) {if(!ClearFlags[504]) await AddActionAndClaim(504);}
        if(gdb.GuideMissionId == 507 && stageId == GuideMissionTables[507].Subkey) {if(!ClearFlags[507]) await AddActionAndClaim(507);}
        if(gdb.GuideMissionId == 508 && stageId == GuideMissionTables[508].Subkey) {if(!ClearFlags[508]) await AddActionAndClaim(508);}
        if(gdb.GuideMissionId == 509 && stageId == GuideMissionTables[509].Subkey) {if(!ClearFlags[509]) await AddActionAndClaim(509);}
        
        if(gdb.GuideMissionId == 505 && stageId == GuideMissionTables[505].Subkey) {if(!ClearFlags[505]) await AddActionAndClaim(505);}
        if(gdb.GuideMissionId == 506 && stageId == GuideMissionTables[506].Subkey) {if(!ClearFlags[506]) await AddActionAndClaim(506);}
        if(gdb.GuideMissionId == 601 && stageId == GuideMissionTables[601].Subkey) {if(!ClearFlags[601]) await AddActionAndClaim(601);}
    }

    public static async UniTask UpgradeUnit()
    {
        if(!isInit) Init();
        var charModel = ServerDataManager.Instance.Character.GetCharacter(아트레시아ID);
        if(gdb.GuideMissionId == 202 && charModel.Level > 1) await AddActionAndClaim(202);
        if(gdb.GuideMissionId == 305 && charModel.ExceedLevel > 0) await AddActionAndClaim(305);
        if(gdb.GuideMissionId == 402 && charModel.TranscendLevel > 3) await AddActionAndClaim(402);
    }
}
