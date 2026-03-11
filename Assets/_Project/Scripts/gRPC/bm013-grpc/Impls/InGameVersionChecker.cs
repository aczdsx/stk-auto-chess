/*
 * Copyright (c) CookApps.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tech.Hive.V1;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// 인게임중 게임 버전(점검, 강업등) 및 스펙 체크
    public static class InGameVersionChecker
    {
        private static readonly WaitForSecondsRealtime Delay = new(60 * 5); // 5분마다 체크
        public static IEnumerator CoroutineLoop()
        {
            while (true)
            {
                yield return Delay;
                CheckVersion();
            }
            // ReSharper disable once IteratorNeverReturns
        }

        /// 인게임중 게임 버전(점검, 강업등) 및 스펙 체크
        private static async void CheckVersion()
        {
            try
            {
                var netManager = NetManager.Instance;
                CheckVersionResponse checkVersionResponse = await netManager.Lobby.CheckVersionAsync();

                // 게임의 버전 체크중 오류가 발생해도 게임진행은 유지
                if (!checkVersionResponse.IsSuccess)
                    return;

                // 점검중이라면 게임진행 불가
                if (checkVersionResponse.Data.IsUnderMaintenance)
                    throw new UnderMaintenanceException();

                // 강제업데이트 상태라면 게임진행 불가
                if (checkVersionResponse.Data.UpdateStatus == CheckVersionUpdateStatus.UpdateStatusForce)
                    throw new UpdateStatusForceException();

                // 스펙 체크 및 다운로드
                uint gameVersion = checkVersionResponse.Data.SpecVersion;
                uint languageVersion = checkVersionResponse.Data.LanguageVersion;
                uint etcVersion = checkVersionResponse.Data.EtcSpecVersion;
                SpecCheckAndDownload(gameVersion, languageVersion, etcVersion);
            }
            catch (Exception e)
            {
                // UnderMaintenanceException, UpdateStatusForceException
                // 게임중 점검이나 강제업데이트 상태 처리
                Debug.LogError(e);
            }
        }

        private static async void SpecCheckAndDownload(uint gameVersion, uint languageVersion, uint etcVersion)
        {
            List<Task> tasks = UnityEngine.Pool.ListPool<Task>.Get();
            try
            {
                var netManager = NetManager.Instance;

                // 현재 캐시된 버전과 다르면 다운로드
                if(netManager.Spec.GetCachedSpecVersion(SpecType.Game) != gameVersion)
                    tasks.Add(netManager.Spec.GetSpecJsonAsync(SpecType.Game, gameVersion));
                if(netManager.Spec.GetCachedSpecVersion(SpecType.Language) != languageVersion)
                    tasks.Add(netManager.Spec.GetSpecJsonAsync(SpecType.Language, languageVersion));
                if(netManager.Spec.GetCachedSpecVersion(SpecType.EtcSpec) != etcVersion)
                    tasks.Add(netManager.Spec.GetSpecJsonAsync(SpecType.EtcSpec, etcVersion));

                if (tasks.Count == 0)
                    return;

                // 최신버전이 있으면 다운로드 병렬 처리
                await Task.WhenAll(tasks);
            }
            catch (Exception)
            {
                // 인게임중에는 스펙을 못받아도 게임진행은 유지
            }
            finally
            {
                UnityEngine.Pool.ListPool<Task>.Release(tasks);
            }
        }
    }
}
