using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CookApps.NetLite.Utils;
using Grpc.Core;
using Tech.Hive.V1;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    /// 최초 진입하는 타이틀 씬
    public class TitleScene : MonoBehaviour
    {
        [SerializeField] private Text _infoText;
        [SerializeField] private Button _buttonReadyToInGame;
        [SerializeField] private GameObject _goInGameScene;

        private void Awake()
        {
            // NetManager 초기화
            NetManager.Instance.Startup();
        }

        private void OnDestroy()
        {
            // 모든 게임 종료시 NetManager 종료 처리 (여기서는 필요없음)
            // NetManager.Instance.Shutdown();
        }

        private void OnEnable()
        {
            UserData.Instance.Reset();
            SetReadyToInGame(false);
        }

        /// 여기서부터 시작 (실제 인게임에 들어가기 위한 기능)
        public async void OnClickLaunch()
        {
            try
            {
                SetInfoText("Launch...");
                Task checkVersionAndSpec = CheckVersionAndSpecDownloadAsync();
                Task authenticateUser = AuthenticateUserDataAsync();
                // 인증, 유저 데이터 동기화, 버전 체크, 스펙 다운로드 병렬로 진행
                await Task.WhenAll(authenticateUser, checkVersionAndSpec);

                // 기타 처리...

                SetInfoText("Launch Complete!");
                SetReadyToInGame(true);
            }
            catch(RpcException e) // Rpc 오류
            {
                SetInfoText(e.Message);
                SetReadyToInGame(false);
            }
            catch(UpdateStatusForceException e) // 강제 업데이트 필요
            {
                SetInfoText(e.Message);
                SetReadyToInGame(false);
            }
            catch(SpecDownloadFailedException e) // 스펙 다운로드 실패
            {
                SetInfoText(e.Message);
                SetReadyToInGame(false);
            }
            catch(Exception e) // 그 외 오류
            {
                SetInfoText(e.Message);
                SetReadyToInGame(false);
            }
        }

        /// 인게임 씬으로 이동
        public void OnClickReadyToInGame()
        {
            // 게임 씬으로 이동
            _goInGameScene.SetActive(true);
            gameObject.SetActive(false);
        }

        /// 인증과 서버의 데이터 동기화 처리
        private async Task AuthenticateUserDataAsync()
        {
            Debug.Log("인증 및 유저 데이터 동기화 시작");
            // Platform Auth 패키지를 사용하여 플렛폼에 맞게 authId 획득
            // Platform 정보 및 AuthId는 암호화해서 로컬에 저장 이후 재사용 가능
            string authId = await AuthPlatformUtil.GetAuthId(AuthPlatform.Google);

            var netManager = NetManager.Instance;
            AuthenticateResponse authenticateResponse = await netManager.Auth.AuthenticateAsync(AuthPlatform.Google, authId);
            // 오류 발생 시 예외 던지자 (상위 메서드에서 처리)
            authenticateResponse.ThrowIfError();

            // userId, playerId 세팅
            UserData.Instance.UserId = authenticateResponse.Data.Uid;
            UserData.Instance.PlayerId = authenticateResponse.Data.PlayerId;

            // 신규 유저라면 동기화할 데이터가 없으므로 종료
            if (authenticateResponse.Data.IsNewUser)
                return;

            // 디바이스 변경되지 않았다면 로컬 저장된 데이터를 이용 가능하니 종료 가능 (게임에 따라 서버 데이터를 우선하면 계속 진행)
            // if (!authenticateResponse.Data.IsDeviceIdChanged)
            //     return;

            // 서버에서 유저 데이터 동기화 수신
            PlayerDataListResponse playerDataResponse = await netManager.PlayerData.ListAsync(UserData.GetSyncCategoryNames());
            playerDataResponse.ThrowIfError();

            // 동기화된 유저 데이터로 갱신
            Dictionary<string, string> categoryData = playerDataResponse.Data.ItemList.ToDictionary(x => x.Category, x => x.GetData());

            // 기존 로컬에 저장된 데이터와 병합 처리 필요(게임에 따라 다름)
            // 여기서는 서버 데이터를 우선으로 처리
            // 동기화된 데이터로 로컬 유저 데이터 갱신
            UserData.Instance.SetSyncData(categoryData);

            Debug.Log("인증 및 유저 데이터 동기화 완료");
        }

        /// 버전 체크 및 스펙 다운로드 처리
        private async Task CheckVersionAndSpecDownloadAsync()
        {
            Debug.Log("버전 및 스펙 체크 시작");
            var netManager = NetManager.Instance;
            // CheckVersionAsync 호출 (재시도 포함)
            CheckVersionResponse checkVersionResponse = await GrpcCallHelper.CallWithRetry(() => netManager.Lobby.CheckVersionAsync());
            // 오류 발생 시 예외 던지자
            checkVersionResponse.ThrowIfError();

            // 강제 업데이트 필요 시 예외 던지자
            if (checkVersionResponse.Data.UpdateStatus == CheckVersionUpdateStatus.UpdateStatusForce)
                throw new UpdateStatusForceException();

            // 점검중이면 예외 던지자
            if (checkVersionResponse.Data.IsUnderMaintenance)
                throw new UnderMaintenanceException();

            // 스펙 최신버전 아직 캐싱 안됬으면 다운로드
            var specGame = netManager.Spec.GetSpecDataAsync(SpecType.Game, checkVersionResponse.Data.SpecVersion);
            var specEtc = netManager.Spec.GetSpecDataAsync(SpecType.EtcSpec, checkVersionResponse.Data.EtcSpecVersion);
            var specLanguage = netManager.Spec.GetSpecDataAsync(SpecType.Language, checkVersionResponse.Data.LanguageVersion);
            // 모든 스펙 병령로 다운로드 진행
            await Task.WhenAll(specGame, specEtc, specLanguage);

            if (string.IsNullOrEmpty(specGame.Result))
                throw new SpecDownloadFailedException(SpecType.Game, checkVersionResponse.Data.SpecVersion);
            if (string.IsNullOrEmpty(specEtc.Result))
                throw new SpecDownloadFailedException(SpecType.EtcSpec, checkVersionResponse.Data.EtcSpecVersion);
            if (string.IsNullOrEmpty(specLanguage.Result))
                throw new SpecDownloadFailedException(SpecType.Language, checkVersionResponse.Data.LanguageVersion);

            // spec 처리 (여기까지 왔다면 최신 스펙 다운로드 완료)
            uint specVersion = netManager.Spec.GetCachedSpecVersion(SpecType.Game);
            string specData = await netManager.Spec.GetCachedSpecDataAsync(SpecType.Game);
            // spec load.....
            // 현재 사용되는 게임 스펙 버전 세팅 (서버에서도 해당 버전의 정보를 사용)
            netManager.Spec.CurrentGameSpecVersion = specVersion;

            Debug.Log("버전 및 스펙 체크 완료");
        }

        private void SetReadyToInGame(bool isReady)
        {
            if (_buttonReadyToInGame != null)
            {
                _buttonReadyToInGame.interactable = isReady;
            }
        }

        private void SetInfoText(string message)
        {
            if (_infoText != null)
            {
                _infoText.text = message;
            }
        }
    }
}
