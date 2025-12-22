using System;
using UnityEngine;
using Naninovel;
using Naninovel.Async;
using CookApps.TeamBattle.UIManagements;

namespace CookApps.AutoBattler
{
    public class NaninovelMain : UILayer
    {
        [SerializeField] private string defaultScriptName;
        [SerializeField] private bool hideTitleMenuOnInit = true;

        public static float eventStartTime = 0f;
        public static bool isSkip = false;
        public static bool isAuto = false;

        private IScriptPlayer _scriptPlayer;
        private ILocalizationManager _localizationManager;
        private bool _isInitialized = false;
        private Action _onEndAction;

        public static NaninovelMain GetNaninovelMain()
        {
            return SceneUILayerManager.Instance.GetUILayer<NaninovelMain>();
        }

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnBackButton(ref bool offPrevUI)
        {
            // Naninovel은 백 버튼으로 종료하지 않음
            // 필요시 커스텀 로직 추가
        }

        protected override void OnPreEnter(object param)
        {
            Debug.Log("NaninovelMain param: " + param);
            base.OnPreEnter(param);

            // 파라미터 처리
            string scriptName = defaultScriptName;
            _onEndAction = null;

            (scriptName, _onEndAction) = ((string, Action))param;

            InitializeNaninovelAsync(scriptName).Forget();
        }

        /// <summary>
        /// 스크립트 종료 시 실행할 액션 설정
        /// </summary>
        public void SetEndAction(Action action)
        {
            _onEndAction = action;
        }

        /// <summary>
        /// 스크립트 종료 시 실행할 액션 실행
        /// </summary>
        public void ExecuteEndAction()
        {
            _onEndAction?.Invoke();
            _onEndAction = null; // 실행 후 정리
        }

        private async UniTaskVoid InitializeNaninovelAsync(string scriptName)
        {
            try
            {
                // Naninovel 엔진 초기화
                await InitializeEngine();

                // 로컬라이제이션 설정
                await SetupLocalization();

                // 스크립트 재생
                if (!string.IsNullOrEmpty(scriptName))
                {
                    await PlayScript(scriptName);
                }
                else
                {
                    Debug.LogWarning("NaninovelMain: 스크립트 이름이 지정되지 않았습니다.");
                }

                eventStartTime = Time.realtimeSinceStartup;
            }
            catch (Exception ex)
            {
                Debug.LogError($"NaninovelMain 초기화 실패: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async UniTask InitializeEngine()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                await RuntimeInitializer.Initialize();

                if (!Engine.Initialized)
                {
                    throw new Exception("Naninovel 엔진이 초기화되지 않았습니다!");
                }

                _scriptPlayer = Engine.GetService<IScriptPlayer>();
                _localizationManager = Engine.GetService<ILocalizationManager>();
                _isInitialized = true;

                // TitleMenu UI 숨기기 (옵션에 따라)
                if (hideTitleMenuOnInit)
                {
                    var uiManager = Engine.GetService<IUIManager>();
                    var titleUI = uiManager?.GetUI<Naninovel.UI.ITitleUI>();
                    if (titleUI != null)
                    {
                        titleUI.Hide();
                        Debug.Log("NaninovelMain: TitleMenu UI 숨김");
                    }
                }

                Debug.Log("Naninovel 엔진 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Naninovel 엔진 초기화 실패: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private async UniTask SetupLocalization()
        {
            if (!_isInitialized || _localizationManager == null)
            {
                Debug.LogError("Naninovel 엔진이 초기화되지 않았습니다!");
                return;
            }

            // TODO: UserDataManager에서 언어 코드 가져오기
            // var lanCode = LanguageManager.Instance.GetCurrentLanguageCode();
            // var locale = ConvertLanguageCodeToLocale(lanCode);

            var locale = "ko"; // 기본값: 한국어

            Debug.Log($"Naninovel 로컬라이제이션 설정: {locale}");
            await _localizationManager.SelectLocale(locale);
        }

        private async UniTask PlayScript(string scriptName)
        {
            if (!_isInitialized || _scriptPlayer == null)
            {
                Debug.LogError("Naninovel 엔진이 초기화되지 않았습니다!");
                return;
            }

            // 스크립트 경로 정규화
            var scriptPath = NormalizeScriptPath(scriptName);

            if (string.IsNullOrEmpty(scriptPath))
            {
                Debug.LogError("NaninovelMain: 스크립트 경로가 비어있습니다!");
                return;
            }

            Debug.Log($"Naninovel 스크립트 재생 시작: {scriptPath}");

            try
            {
                await _scriptPlayer.LoadAndPlay(scriptPath);
                Debug.Log($"Naninovel 스크립트 재생 중: {_scriptPlayer.Playing}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Naninovel 스크립트 재생 실패: {ex.Message}\n스크립트 경로: {scriptPath}\n원본 이름: {scriptName}");
                Debug.LogError(ex.ToString());
            }
        }

        private string NormalizeScriptPath(string scriptName)
        {
            if (string.IsNullOrEmpty(scriptName))
            {
                return string.Empty;
            }

            var path = scriptName;

            // "Scripts/" 접두사 제거
            // if (path.StartsWith("Scripts/"))
            // {
            //     path = path.Substring(7);
            // }

            // 확장자 제거 (.nani)
            if (path.EndsWith(".nani"))
            {
                path = path.Substring(0, path.Length - 5);
            }

            return path;
        }

        // TODO: 언어 코드를 Naninovel locale로 변환하는 헬퍼 메서드
        // private string ConvertLanguageCodeToLocale(string languageCode)
        // {
        //     return languageCode switch
        //     {
        //         "ko" => "ko",
        //         "ja" => "ja",
        //         "zh-TW" => "zh-TW",
        //         "zh-CN" => "zh-CN",
        //         "en" => "en",
        //         _ => "ko"
        //     };
        // }

        /// <summary>
        /// 외부에서 스크립트를 재생하고 싶을 때 사용
        /// </summary>
        public async UniTask PlayScriptAsync(string scriptName)
        {
            if (!_isInitialized)
            {
                await InitializeEngine();
                await SetupLocalization();
            }

            await PlayScript(scriptName);
        }

        /// <summary>
        /// Naninovel 스크립트를 중지
        /// </summary>
        public void StopScript()
        {
            if (_scriptPlayer != null && _scriptPlayer.Playing)
            {
                _scriptPlayer.Stop();
            }
        }
    }
}

