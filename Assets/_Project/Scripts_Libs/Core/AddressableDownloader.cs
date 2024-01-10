using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.TeamBattle
{
    public static class AddressableDownloader
    {
        private static List<string> downloadedKeys = new ();

        public static async UniTask<long> GetTotalDownloadSize(IEnumerable<string> remoteLabels)
        {
            await Addressables.InitializeAsync();

            long totalSize = 0;

            long[] sizes;
            {
                var tasks = new List<UniTask<long>>();
                foreach (var key in remoteLabels)
                {
                    tasks.Add(Addressables.GetDownloadSizeAsync(key).ToUniTask());

                }

                sizes = await UniTask.WhenAll(tasks);
                totalSize = sizes.Sum();
            }

            return totalSize;
        }

        public static async UniTask<long[]> GetEachDownloadSize(IList<string> remoteLabels)
        {
            await Addressables.InitializeAsync();

            long[] sizes;
            {
                var tasks = new List<UniTask<long>>();
                foreach (var key in remoteLabels)
                {
                    tasks.Add(Addressables.GetDownloadSizeAsync(key).ToUniTask());

                }

                sizes = await UniTask.WhenAll(tasks);
            }

            return sizes;
        }

        public static async UniTask DownloadAllAsync(IList<string> remoteLabels, Action<string, long> onDownloadCompleteEachLabel)
        {
            long[] sizes = await GetEachDownloadSize(remoteLabels);
            {
                for (int i = 0; i < remoteLabels.Count; i++)
                {
                    bool isSuccess = false;

                    if (sizes[i] == 0)
                    {
                        isSuccess = true;
                    }
                    else
                    {
                        var handle = Addressables.DownloadDependenciesAsync(remoteLabels[i]);
                        await handle;
                        isSuccess = handle.Status == AsyncOperationStatus.Succeeded;
                        Addressables.Release(handle);
                    }

                    var locs = await Addressables.LoadResourceLocationsAsync(remoteLabels[i]);
                    foreach (var loc in locs)
                        downloadedKeys.Add(loc.InternalId);

                    onDownloadCompleteEachLabel?.Invoke(remoteLabels[i], isSuccess ? sizes[i] : -1);
                }
            }

            downloadedKeys = downloadedKeys.Distinct().ToList();
        }

        public static async UniTask DownloadAllInBackgroundAsync(IList<string> remoteLabels)
        {
            long[] sizes = await GetEachDownloadSize(remoteLabels);
            {
                var handles = new List<(string label, AsyncOperationHandle handle)>();
                for (int i = 0; i < remoteLabels.Count; i++)
                {
                    if (sizes[i] == 0)
                    {
                        var locs = await Addressables.LoadResourceLocationsAsync(remoteLabels[i]);
                        foreach (var loc in locs)
                        {
                            downloadedKeys.Add(loc.InternalId);
                        }
                    }
                    else
                    {
                        handles.Add((remoteLabels[i], Addressables.DownloadDependenciesAsync(remoteLabels[i], false)));
                    }
                }

                for (var i = 0; i < handles.Count; i++)
                {
                    await handles[i].handle;
                    Addressables.Release(handles[i].handle);
                    var locs = await Addressables.LoadResourceLocationsAsync(handles[i].label);
                    foreach (var loc in locs)
                    {
                        downloadedKeys.Add(loc.InternalId);
                    }
                }
            }

            downloadedKeys = downloadedKeys.Distinct().ToList();
        }

        public static async UniTask ClearDownloadCacheAsync()
        {
            foreach (var tmp in Addressables.ResourceLocators)
            {
                var async = Addressables.ClearDependencyCacheAsync(tmp.Keys, false);
                await async;
                Addressables.Release(async);
            }

            Caching.ClearCache();
        }

        public static bool IsDownloaded(string key)
        {
            for (var i = 0; i < downloadedKeys.Count; i++)
            {
                if (downloadedKeys[i].Contains(key))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
