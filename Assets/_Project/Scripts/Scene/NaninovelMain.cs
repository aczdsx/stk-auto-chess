using System;
using System.Linq;
using CookApps.TeamBattle.UIManagements;
using Naninovel;
using UnityEngine;

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
        private string _currentScriptName; // 현재 실행 중인 스크립트 이름
        private bool _isSkipTransition = false; // SKIP 버튼으로 전환 중인지 여부

        public static NaninovelMain GetNaninovelMain()
        {
            return SceneUILayerManager.Instance.GetUILayer<NaninovelMain>();
        }

        protected override void Awake()
        {
            base.Awake();
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

            // 파라미터 처리 - (스크립트 이름, 종료 액션) 튜플
            _onEndAction = null;
            _currentScriptName = null;

            var (scriptName, onEndAction) = ((string, Action))param;
            _onEndAction = onEndAction;
            _currentScriptName = scriptName;
            PreEnterAsync().Forget();
        }

        private async UniTask PreEnterAsync()
        {
            // 스크립트 실행
            if (!string.IsNullOrEmpty(_currentScriptName))
            {
                await InitializeNaninovelAsync(_currentScriptName);
            }
            else
            {
                Debug.LogWarning("NaninovelMain: 실행할 스크립트가 없습니다.");
                _onEndAction?.Invoke();
                _onEndAction = null;
            }

            await SceneTransition.FadeOutAsync();
        }

        /// <summary>
        /// 스크립트 종료 시 실행할 액션 설정
        /// </summary>
        public void SetEndAction(Action action)
        {
            _onEndAction = action;
        }

        /// <summary>
        /// 스크립트 종료 시 호출 - 트리거 매니저에서 다음 스크립트 검색 후 실행 (비동기 버전)
        /// @end 커맨드 완료 전에 다음 스크립트 재생을 await하여 Naninovel 엔진이 종료되지 않도록 함
        /// </summary>
        public async UniTask ExecuteEndActionAsync()
        {
            // 현재 스크립트 실행 완료 기록
            if (!string.IsNullOrEmpty(_currentScriptName))
            {
                NaninovelTriggerManager.Instance.MarkTriggerExecuted(_currentScriptName);
            }

            // 트리거 매니저에서 다음 스크립트 검색 (NANINOVEL_END 트리거)
            var nextScript = NaninovelTriggerManager.Instance.GetTriggerOnNaninovelEnd(_currentScriptName);

            if (!string.IsNullOrEmpty(nextScript))
            {
                Debug.Log($"NaninovelMain: 다음 스크립트 실행 - {nextScript}");
                _currentScriptName = nextScript;

                // 씬 전환 애니메이션과 함께 다음 스크립트 실행 (await로 @end 완료 지연)
                await PlayNextScriptWithTransitionAsync(nextScript);
                return;
            }

            // 다음 스크립트 없음 - 씬 전환 애니메이션과 함께 종료 액션 실행
            Debug.Log("NaninovelMain: 모든 스크립트 완료, 종료 액션 실행");
            _currentScriptName = null;
            await EndWithTransitionInternalAsync();
        }

        /// <summary>
        /// 씬 전환 애니메이션과 함께 종료 액션 실행 (다른 씬으로 이동)
        /// FadeOut은 이동하는 씬에서 처리됨
        /// </summary>
        private async UniTask EndWithTransitionInternalAsync()
        {
            Debug.Log($"EndWithTransitionInternalAsync 시작 - _onEndAction null 여부: {_onEndAction == null}");

            if (_onEndAction == null)
            {
                Debug.LogWarning("EndWithTransitionInternalAsync: _onEndAction이 null입니다");
                return;
            }

            // NaninovelEndCommand에서 이미 FadeIn 처리된 경우 스킵
            if (!SceneTransition.IsFadeProcessing)
            {
                Debug.Log("EndWithTransitionInternalAsync: SceneTransition 생성 중...");
                SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);

                Debug.Log("EndWithTransitionInternalAsync: FadeIn 시작...");
                await SceneTransition.FadeInAsync();
            }
            Debug.Log("EndWithTransitionInternalAsync: FadeIn 완료, 종료 액션 실행...");

            // 종료 액션 실행 (다른 씬으로 전환)
            // FadeOut은 다른 씬에서 처리됨
            _onEndAction?.Invoke();
            _onEndAction = null;
            Debug.Log("EndWithTransitionInternalAsync: 종료 액션 실행 완료");
        }

        /// <summary>
        /// 씬 전환 애니메이션과 함께 다음 스크립트 실행 (비동기 버전)
        /// </summary>
        private async UniTask PlayNextScriptWithTransitionAsync(string nextScript)
        {
            // NaninovelEndCommand에서 이미 FadeIn 처리된 경우 스킵
            if (!SceneTransition.IsFadeProcessing)
            {
                SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
                await SceneTransition.FadeInAsync();
            }

            // 이전 스크립트의 spawn 객체 정리
            var spawnManager = Engine.GetService<ISpawnManager>();
            if (spawnManager != null && spawnManager.Spawned.Count > 0)
            {
                // Spawned 컬렉션 복사 후 순회 (순회 중 컬렉션 변경 방지)
                var spawnedPaths = spawnManager.Spawned.Select(s => s.Path).ToList();
                foreach (var path in spawnedPaths)
                {
                    spawnManager.DestroySpawned(path);
                }
                Debug.Log($"NaninovelMain: 이전 스크립트의 spawn 객체 {spawnedPaths.Count}개 정리 완료");
            }

            // 연쇄 재생 전 UI 상태 복원
            RestoreUIStateForChainedScript();

            // 스크립트 로드 및 재생
            await PlayScript(nextScript);

            // 페이드 아웃 (화면 복원)
            await SceneTransition.FadeOutAsync();

            // SKIP으로 전환된 경우에만 첫 대사 입력 대기 상태를 자동으로 넘김
            if (_isSkipTransition && _scriptPlayer != null && _scriptPlayer.WaitingForInput)
            {
                Debug.Log("NaninovelMain: SKIP 전환 후 첫 입력 자동 트리거");
                var inputManager = Engine.GetService<IInputManager>();
                inputManager?.GetContinue()?.Activate(1f);
            }
        }

        /// <summary>
        /// 연쇄 재생 전 UI 및 입력 상태 복원 (이전 스크립트에서 숨긴 UI/비활성화된 입력 복원)
        /// </summary>
        private void RestoreUIStateForChainedScript()
        {
            try
            {
                // UI 표시 (@showUI와 동일)
                var uiManager = Engine.GetService<IUIManager>();
                uiManager?.SetUIVisibleWithToggle(true);

                // TextPrinter 표시 (@showPrinter와 동일)
                var printerManager = Engine.GetService<ITextPrinterManager>();
                if (printerManager != null && !string.IsNullOrEmpty(printerManager.DefaultPrinterId))
                {
                    var printer = printerManager.GetActor(printerManager.DefaultPrinterId);
                    printer?.ChangeVisibility(true, new(0.3f)).Forget();
                }

                // 입력 시스템 활성화 (이전 스크립트에서 비활성화된 경우 복원)
                var inputManager = Engine.GetService<IInputManager>();
                if (inputManager != null)
                {
                    Debug.Log($"NaninovelMain: 입력 상태 확인 - ProcessInput: {inputManager.ProcessInput}");
                    if (!inputManager.ProcessInput)
                    {
                        inputManager.ProcessInput = true;
                        Debug.Log("NaninovelMain: 입력 시스템 활성화됨");
                    }
                }

                // 스크립트 플레이어 상태 확인
                if (_scriptPlayer != null)
                {
                    Debug.Log($"NaninovelMain: ScriptPlayer 상태 - Playing: {_scriptPlayer.Playing}, WaitingForInput: {_scriptPlayer.WaitingForInput}");
                }

                Debug.Log("NaninovelMain: 연쇄 재생을 위한 UI 상태 복원 완료");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"NaninovelMain: UI 상태 복원 중 오류 - {ex.Message}");
            }
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

                // 플레이어 닉네임을 Naninovel 변수로 설정
                SetupPlayerVariable();

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

                // 스크립트 재생 후 입력 상태 확인 및 활성화
                var inputManager = Engine.GetService<IInputManager>();
                if (inputManager != null)
                {
                    Debug.Log($"Naninovel PlayScript 후 입력 상태 - ProcessInput: {inputManager.ProcessInput}");
                    if (!inputManager.ProcessInput)
                    {
                        inputManager.ProcessInput = true;
                        Debug.Log("Naninovel PlayScript: 입력 시스템 강제 활성화");
                    }
                }

                Debug.Log($"Naninovel PlayScript 후 ScriptPlayer 상태 - WaitingForInput: {_scriptPlayer.WaitingForInput}");
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

            var path = /*"Scripts/" + */scriptName;

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

        /// <summary>
        /// 스킵 버튼용 - 현재 스크립트를 즉시 종료하고 @end 로직 실행
        /// </summary>
        public async UniTaskVoid SkipToEndAsync()
        {
            if (_scriptPlayer == null) return;
            if (SceneTransition.IsFadeProcessing) return; // 전환 중이면 무시

            Debug.Log("NaninovelMain: 스킵으로 즉시 종료");

            // SKIP 전환 플래그 설정
            _isSkipTransition = true;

            // 현재 스크립트 중지
            if (_scriptPlayer.Playing)
            {
                _scriptPlayer.Stop();
            }

            // @end와 동일한 종료 로직 실행
            SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
            await SceneTransition.FadeInAsync();
            await ExecuteEndActionAsync();

            // SKIP 전환 플래그 초기화
            _isSkipTransition = false;
        }

        /// <summary>
        /// 플레이어 닉네임을 Naninovel 변수로 설정
        /// .nani 스크립트에서 {player} 형식으로 사용 가능
        /// </summary>
        private void SetupPlayerVariable()
        {
            var variableManager = Engine.GetService<ICustomVariableManager>();
            if (variableManager == null) return;

            var nickname = ServerDataManager.Instance?.PlayerData?.Nickname;
            if (!string.IsNullOrEmpty(nickname))
            {
                variableManager.SetVariableValue("player", new CustomVariableValue(nickname));
                Debug.Log($"NaninovelMain: player 변수 설정 - {nickname}");
            }
        }

        /// <summary>
        /// 플레이어 닉네임 변수 업데이트 (외부에서 호출 가능)
        /// </summary>
        public void UpdatePlayerVariable(string nickname)
        {
            var variableManager = Engine.GetService<ICustomVariableManager>();
            if (variableManager == null) return;

            variableManager.SetVariableValue("player", new CustomVariableValue(nickname ?? ""));
            Debug.Log($"NaninovelMain: player 변수 업데이트 - {nickname}");
        }
    }
}
