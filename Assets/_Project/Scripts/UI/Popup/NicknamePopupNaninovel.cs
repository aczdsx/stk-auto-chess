using System;
using System.Collections.Generic;
using System.Text;
using Naninovel;
using Naninovel.Commands;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CookApps.TeamBattle.UIManagements;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// Naninovel spawn용 닉네임 입력 팝업
    /// UILayerSystem 없이 독립적으로 동작
    ///
    /// 사용법:
    /// @spawn NicknamePopupNaninovel wait:true
    /// @spawn NicknamePopupNaninovel params:true wait:true  ; allowClose=true
    ///
    /// 결과:
    /// - 입력된 닉네임은 Naninovel 변수 {NicknameResult}에 저장됨
    /// - 취소 시 빈 문자열
    /// </summary>
    public class NicknamePopupNaninovel : MonoBehaviour, Spawn.IAwaitable, Spawn.IParameterized
    {
        [Header("Common")]
        [SerializeField] private CAButton _confirmButton;
        [SerializeField] private Canvas _canvas;

        [Space(10)]
        [SerializeField] private TMP_InputField _nicknameInputField;

        [Header("Naninovel Settings")]
        [SerializeField] private bool _allowClose = false; // 첫 닉네임 설정 시 닫기 불가

        private const string NicknameResultVariable = "NicknameResult";

        private UniTaskCompletionSource _spawnCompletionSource;
        private bool _isCompleted;
        private string _resultNickname = "";
        private bool _wasInputEnabled;


        [Header("Tutorial Toast Pop")]
        [SerializeField] private GameObject _tutorialToastObj;
        [SerializeField] private TextMeshProUGUI _tutoiralText;
        [SerializeField] private Animator _tutorialToastAnimator;

        private static readonly int LongShow = Animator.StringToHash("LongAnim");

        /// <summary>
        /// Naninovel 파라미터 설정
        /// params:true -> allowClose = true
        /// </summary>
        public void SetSpawnParameters(IReadOnlyList<string> parameters, bool asap)
        {
            if (parameters == null || parameters.Count == 0) return;

            // 첫 번째 파라미터: allowClose (true/false)
            if (parameters.Count > 0 && bool.TryParse(parameters[0], out var allowClose))
            {
                _allowClose = allowClose;
            }
        }

        private void Awake()
        {
            _confirmButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickConfirmButton()).AddTo(this);
        }

        private void OnEnable()
        {
            _isCompleted = false;
            _spawnCompletionSource = new UniTaskCompletionSource();
            _nicknameInputField.text = "";

            // UICamera를 사용하여 Dialogue_New보다 위에 렌더링되도록 설정
            SetupCanvasCamera();

            // Naninovel 입력 비활성화 (팝업이 입력을 독점하도록)
            DisableNaninovelInput();

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
        }

        private void DisableNaninovelInput()
        {
            var inputManager = Engine.GetService<IInputManager>();
            if (inputManager != null)
            {
                _wasInputEnabled = inputManager.ProcessInput;
                inputManager.ProcessInput = false;
            }
        }

        private void RestoreNaninovelInput()
        {
            RestoreNaninovelInputDelayed().Forget();
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid RestoreNaninovelInputDelayed()
        {
            // 1프레임 대기하여 현재 클릭 이벤트가 Naninovel로 전달되지 않도록 함
            await Cysharp.Threading.Tasks.UniTask.Yield();

            var inputManager = Engine.GetService<IInputManager>();
            if (inputManager != null)
            {
                inputManager.ProcessInput = _wasInputEnabled;
            }
        }

        private void SetupCanvasCamera()
        {
            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
            }

            if (_canvas != null)
            {
                var cameraManager = Engine.GetService<ICameraManager>();
                if (cameraManager?.UICamera != null)
                {
                    _canvas.worldCamera = cameraManager.UICamera;
                    _canvas.sortingOrder = 999; // 다른 UI보다 위에 표시
                }
            }
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void OnDisable()
        {
            // Naninovel 입력 복원
            RestoreNaninovelInput();

            // 완료되지 않은 상태에서 비활성화되면 완료 처리
            if (!_isCompleted)
            {
                _isCompleted = true;
                _spawnCompletionSource?.TrySetResult();
            }
        }

        /// <summary>
        /// Naninovel spawn 완료 대기
        /// 닉네임 입력 완료 또는 닫기까지 대기
        /// </summary>
        public UniTask AwaitSpawn(AsyncToken token = default)
        {
            return _spawnCompletionSource?.Task ?? UniTask.CompletedTask;
        }

        private async void OnClickConfirmButton()
        {
            // 닉네임 유효성 체크
            int minLength = SpecDataManager.Instance.GetGameConfig<int>("min_user_name_length");
            int maxLength = SpecDataManager.Instance.GetGameConfig<int>("max_user_name_length");

            int byteCount = Encoding.UTF8.GetByteCount(_nicknameInputField.text);

            if (byteCount < minLength || byteCount > maxLength)
            {
                ShowToast("ERROR_SERVER_NICKNAME_LENGTH");
                return;
            }

            // 닉네임 변경 API 호출
            var result = await NetManager.Instance.CustomLobby.ChangeNicknameAsync(_nicknameInputField.text);
            if (!result.IsSuccess)
            {
                ShowToast(result.Status?.Message ?? "ERROR_UNKNOWN");
                return;
            }

            // 닉네임 설정 완료 플래그
            ClientProgressData.Get().SetNicknameSet(true);

            // Naninovel player 변수 업데이트
            NaninovelMain.GetNaninovelMain()?.UpdatePlayerVariable(_nicknameInputField.text);

            _resultNickname = _nicknameInputField.text;
            CompleteAndDestroy();
        }

        private void CompleteAndDestroy()
        {
            if (_isCompleted) return;

            _isCompleted = true;

            // Naninovel 변수에 결과 저장
            SaveResultToNaninovelVariable();

            _spawnCompletionSource?.TrySetResult();

            // SpawnManager를 통해 제거
            var spawnManager = Engine.GetService<ISpawnManager>();
            if (spawnManager != null)
            {
                // 현재 오브젝트의 spawn path 찾기
                foreach (var spawned in spawnManager.Spawned)
                {
                    if (spawned.GameObject == gameObject)
                    {
                        spawnManager.DestroySpawned(spawned.Path);
                        return;
                    }
                }
            }

            // fallback: 직접 제거
            Destroy(gameObject);
        }

        private void SaveResultToNaninovelVariable()
        {
            var variableManager = Engine.GetService<ICustomVariableManager>();
            if (variableManager == null) return;

            // NicknameResult 변수에 결과 저장
            variableManager.SetVariableValue(NicknameResultVariable, new CustomVariableValue(_resultNickname));

            // player 변수도 업데이트 (스크립트에서 {player}로 사용)
            if (!string.IsNullOrEmpty(_resultNickname))
            {
                variableManager.SetVariableValue("player", new CustomVariableValue(_resultNickname));
            }

            Debug.Log($"[NicknamePopupNaninovel] 닉네임 결과 저장: {_resultNickname}");
        }

        private void ShowToast(string tokenKey)
        {
            if (_tutorialToastObj == null || _tutoiralText == null) return;

            string message = LanguageManager.Instance.GetDefaultText(tokenKey);
            _tutoiralText.text = message;
            _tutorialToastObj.SetActive(true);

            if (_tutorialToastAnimator != null)
            {
                _tutorialToastAnimator.SetTrigger(LongShow);
            }

            HideToastAfterAnimationAsync().Forget();
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid HideToastAfterAnimationAsync()
        {
            await Cysharp.Threading.Tasks.UniTask.Yield();

            if (_tutorialToastAnimator != null)
            {
                var clipInfo = _tutorialToastAnimator.GetCurrentAnimatorClipInfo(0);
                if (clipInfo.Length > 0)
                {
                    float clipLength = clipInfo[0].clip.length;
                    await Cysharp.Threading.Tasks.UniTask.Delay(TimeSpan.FromSeconds(clipLength));
                }
                else
                {
                    await Cysharp.Threading.Tasks.UniTask.Delay(TimeSpan.FromSeconds(2f));
                }
            }
            else
            {
                await Cysharp.Threading.Tasks.UniTask.Delay(TimeSpan.FromSeconds(2f));
            }

            if (_tutorialToastObj != null)
            {
                _tutorialToastObj.SetActive(false);
            }
        }
    }
}
