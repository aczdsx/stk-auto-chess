using System.Collections.Generic;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// ScriptableObject 데이터를 초기화 시점에 로드하여 접근할 수 있는 Singleton 클래스
    /// </summary>
    public class SoDataProvider : SingletonMonoBehaviour<SoDataProvider>
    {
        private Dictionary<System.Type, ScriptableObject> _soDataCache = new Dictionary<System.Type, ScriptableObject>();
        private bool _isInitialized = false;

        /// <summary>
        /// 초기화 완료 여부
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// ScriptableObject를 초기화 시점에 로드
        /// </summary>
        /// <typeparam name="T">ScriptableObject 타입</typeparam>
        /// <param name="address">Addressables 주소</param>
        public async UniTask LoadSoData<T>(string address) where T : ScriptableObject
        {
            var type = typeof(T);
            
            // 이미 로드된 경우 스킵
            if (_soDataCache.ContainsKey(type))
            {
                Debug.LogWarning($"[SoDataProvider] {type.Name} is already loaded. Skipping.");
                return;
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<T>(address);
                var so = await handle;
                
                if (so != null)
                {
                    _soDataCache[type] = so;
                    Debug.Log($"[SoDataProvider] {type.Name} loaded successfully from address: {address}");
                }
                else
                {
                    Debug.LogError($"[SoDataProvider] Failed to load {type.Name} from address: {address}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SoDataProvider] Error loading {type.Name} from address: {address}. Exception: {e.Message}");
            }
        }

        /// <summary>
        /// 여러 ScriptableObject를 한 번에 로드
        /// </summary>
        /// <param name="loadInfos">로드할 SO 정보 리스트 (타입, 주소)</param>
        public async UniTask LoadSoDataBatch(List<(System.Type type, string address)> loadInfos)
        {
            var tasks = new List<UniTask>();
            
            foreach (var (type, address) in loadInfos)
            {
                // 이미 로드된 경우 스킵
                if (_soDataCache.ContainsKey(type))
                {
                    continue;
                }

                try
                {
                    var handle = Addressables.LoadAssetAsync<ScriptableObject>(address);
                    var so = await handle;
                    
                    if (so != null && type.IsAssignableFrom(so.GetType()))
                    {
                        _soDataCache[type] = so;
                        Debug.Log($"[SoDataProvider] {type.Name} loaded successfully from address: {address}");
                    }
                    else
                    {
                        Debug.LogError($"[SoDataProvider] Failed to load {type.Name} from address: {address}. Type mismatch or null.");
                        if (so != null)
                        {
                            Addressables.Release(so);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SoDataProvider] Error loading {type.Name} from address: {address}. Exception: {e.Message}");
                }
            }

            _isInitialized = true;
        }

        /// <summary>
        /// 로드된 ScriptableObject 가져오기
        /// </summary>
        /// <typeparam name="T">ScriptableObject 타입</typeparam>
        /// <returns>로드된 ScriptableObject 인스턴스, 없으면 null</returns>
        public T Get<T>() where T : ScriptableObject
        {
            var type = typeof(T);
            
            if (_soDataCache.TryGetValue(type, out var so))
            {
                return so as T;
            }

            Debug.LogWarning($"[SoDataProvider] {type.Name} is not loaded. Call LoadSoData<{type.Name}>() first.");
            return null;
        }

        /// <summary>
        /// 로드된 ScriptableObject 가져오기 (안전한 버전)
        /// </summary>
        /// <typeparam name="T">ScriptableObject 타입</typeparam>
        /// <param name="so">로드된 ScriptableObject 인스턴스</param>
        /// <returns>로드 여부</returns>
        public bool TryGet<T>(out T so) where T : ScriptableObject
        {
            so = Get<T>();
            return so != null;
        }

        /// <summary>
        /// 특정 타입의 ScriptableObject가 로드되었는지 확인
        /// </summary>
        /// <typeparam name="T">ScriptableObject 타입</typeparam>
        /// <returns>로드 여부</returns>
        public bool IsLoaded<T>() where T : ScriptableObject
        {
            return _soDataCache.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 모든 로드된 ScriptableObject 해제
        /// </summary>
        public void UnloadAll()
        {
            foreach (var so in _soDataCache.Values)
            {
                if (so != null)
                {
                    Addressables.Release(so);
                }
            }
            
            _soDataCache.Clear();
            _isInitialized = false;
            Debug.Log("[SoDataProvider] All ScriptableObjects unloaded.");
        }

        /// <summary>
        /// 특정 타입의 ScriptableObject 해제
        /// </summary>
        /// <typeparam name="T">ScriptableObject 타입</typeparam>
        public void Unload<T>() where T : ScriptableObject
        {
            var type = typeof(T);
            
            if (_soDataCache.TryGetValue(type, out var so))
            {
                if (so != null)
                {
                    Addressables.Release(so);
                }
                
                _soDataCache.Remove(type);
                Debug.Log($"[SoDataProvider] {type.Name} unloaded.");
            }
        }

        protected override void OnDestroy()
        {
            UnloadAll();
            base.OnDestroy();
        }
    }
}

