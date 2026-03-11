using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoBattler
{
    public partial class TitleMain
    {
        // 테스트 모드: true로 설정하면 가라 데이터로 다운로드 UI 테스트
        private const bool TestDownloadMode = false;
        private const long TestDownloadSize = 150 * 1024 * 1024; // 150MB 가라 데이터
        private const float TestDownloadDuration = 30f; // 5초 동안 다운로드 시뮬레이션

        /// <summary>
        /// Addressables 다운로드 사이즈 체크 및 다운로드 진행
        /// </summary>
        private async UniTask CheckAndDownloadAddressablesAsync()
        {
            if (TestDownloadMode)
            {
                await TestDownloadAsync();
                return;
            }

            // 카탈로그 업데이트 체크
            var catalogsToUpdate = await Addressables.CheckForCatalogUpdates().WaitUntilDone();
            if (catalogsToUpdate != null && catalogsToUpdate.Count > 0)
            {
                await Addressables.UpdateCatalogs(catalogsToUpdate).WaitUntilDone();
            }

            // 전체 다운로드 사이즈 체크
            var downloadKeys = await GetAllDownloadKeysAsync();
            if (downloadKeys.Count == 0)
            {
                CADebug.Log("[TitleMain] No addressables to download");
                return;
            }

            long totalDownloadSize = 0;
            for (int i = 0; i < downloadKeys.Count; i++)
            {
                totalDownloadSize += downloadKeys[i].Size;
            }

            CADebug.Log($"[TitleMain] Total download size: {DownloadConfirmPopup.FormatFileSize(totalDownloadSize)} ({downloadKeys.Count} keys)");

            // 다운로드 확인 팝업 표시
            bool userConfirmed = await ShowDownloadConfirmPopupAsync(totalDownloadSize);
            if (!userConfirmed)
            {
                CADebug.Log("[TitleMain] User cancelled download");
                return;
            }

            // 다운로드 진행
            await DownloadAddressablesAsync(downloadKeys, totalDownloadSize);
        }

        /// <summary>
        /// 가라 데이터로 다운로드 UI 테스트
        /// </summary>
        private async UniTask TestDownloadAsync()
        {
            CADebug.Log($"[TitleMain] TEST MODE - 가라 다운로드 시작: {DownloadConfirmPopup.FormatFileSize(TestDownloadSize)}");

            // 다운로드 확인 팝업 표시
            bool userConfirmed = await ShowDownloadConfirmPopupAsync(TestDownloadSize);
            if (!userConfirmed)
            {
                CADebug.Log("[TitleMain] TEST MODE - User cancelled download");
                return;
            }

            // 가라 다운로드 진행
            var tcs = new UniTaskCompletionSource<bool>();
            bool isCancelled = false;

            var popupData = new DownloadProgressPopupData(
                videoAssetReference: downloadVideoAssetReference,
                totalDownloadSizeBytes: TestDownloadSize,
                onComplete: () => tcs.TrySetResult(true),
                onCancel: () =>
                {
                    isCancelled = true;
                    tcs.TrySetResult(false);
                }
            );

            var progressPopup = await SceneUILayerManager.Instance.PushUILayerAsync<DownloadProgressPopup>(popupData);

            // 가라 다운로드 시뮬레이션
            float elapsed = 0f;
            while (elapsed < TestDownloadDuration && !isCancelled)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / TestDownloadDuration);
                long downloadedBytes = (long)(TestDownloadSize * progress);

                progressPopup.UpdateProgress(progress, downloadedBytes);
                await UniTask.Yield();
            }

            if (!isCancelled)
            {
                progressPopup.UpdateProgress(1f, TestDownloadSize);
                await UniTask.Delay(500);
                progressPopup.OnDownloadComplete();
            }

            await tcs.Task;
            CADebug.Log("[TitleMain] TEST MODE - 가라 다운로드 완료");
        }

        private const string PreloadLabel = "preload";

        /// <summary>
        /// preload 라벨이 붙은 에셋의 다운로드 키 수집
        /// </summary>
        private async UniTask<List<(object Key, long Size)>> GetAllDownloadKeysAsync()
        {
            var result = new List<(object Key, long Size)>();

            // preload 라벨의 다운로드 사이즈 체크
            var sizeHandle = Addressables.GetDownloadSizeAsync(PreloadLabel);
            long size = await sizeHandle.WaitUntilDone();

            if (size > 0)
            {
                result.Add((PreloadLabel, size));
            }

            return result;
        }

        /// <summary>
        /// 다운로드 확인 팝업 표시
        /// </summary>
        private async UniTask<bool> ShowDownloadConfirmPopupAsync(long downloadSize)
        {
            var tcs = new UniTaskCompletionSource<bool>();

            var popupData = new DownloadConfirmPopupData(
                downloadSizeBytes: downloadSize,
                onConfirm: () => tcs.TrySetResult(true),
                onCancel: () => tcs.TrySetResult(false)
            );

            await SceneUILayerManager.Instance.PushUILayerAsync<DownloadConfirmPopup>(popupData);

            return await tcs.Task;
        }

        /// <summary>
        /// Addressables 다운로드 실행
        /// </summary>
        private async UniTask DownloadAddressablesAsync(List<(object Key, long Size)> downloadKeys, long totalDownloadSize)
        {
            var tcs = new UniTaskCompletionSource<bool>();
            bool isCancelled = false;

            var popupData = new DownloadProgressPopupData(
                videoAssetReference: downloadVideoAssetReference,
                totalDownloadSizeBytes: totalDownloadSize,
                onComplete: () => tcs.TrySetResult(true),
                onCancel: () =>
                {
                    isCancelled = true;
                    tcs.TrySetResult(false);
                }
            );

            var progressPopup = await SceneUILayerManager.Instance.PushUILayerAsync<DownloadProgressPopup>(popupData);

            // 각 키별로 다운로드
            long downloadedBytes = 0;
            for (int i = 0; i < downloadKeys.Count; i++)
            {
                if (isCancelled)
                    break;

                var (key, expectedSize) = downloadKeys[i];
                var downloadHandle = Addressables.DownloadDependenciesAsync(key, false);

                // 다운로드 진행률 업데이트
                while (!downloadHandle.IsDone && !isCancelled)
                {
                    var status = downloadHandle.GetDownloadStatus();
                    long currentDownloaded = downloadedBytes + status.DownloadedBytes;
                    float progress = (float)currentDownloaded / totalDownloadSize;

                    progressPopup.UpdateProgress(progress, currentDownloaded);

                    await UniTask.Yield();
                }

                if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    downloadedBytes += expectedSize;
                }
                else if (!isCancelled)
                {
                    CADebug.LogError($"[TitleMain] Failed to download: {key}");
                    progressPopup.OnDownloadFailed($"Failed to download: {key}");
                    Addressables.Release(downloadHandle);
                    return;
                }

                Addressables.Release(downloadHandle);
            }

            if (!isCancelled)
            {
                progressPopup.UpdateProgress(1f, totalDownloadSize);
                await UniTask.Delay(500);
                progressPopup.OnDownloadComplete();
            }

            await tcs.Task;
        }
    }
}
