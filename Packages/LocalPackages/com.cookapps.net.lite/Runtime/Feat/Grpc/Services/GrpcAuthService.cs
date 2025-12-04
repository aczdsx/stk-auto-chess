/*
 * Copyright (c) CookApps.
 */

#if ENABLE_AUTO_AUTHENTICATE
using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.NetLite.Feat.DB;
#endif

using System;
using System.Threading;
using System.Threading.Tasks;
using Tech.Hive.V1;

namespace CookApps.NetLite.Feat.Grpc
{
    public interface IGrpcAuthServiceSession
    {
        string SessionId { get; }
    }

    [GrpcService(typeof(AuthService.AuthServiceClient))]
    public partial class GrpcAuthService : IGrpcAuthServiceSession, IDisposable
    {
        private AuthServiceRefresh _authServiceRefresh;
        private string _sessionId = string.Empty;

        string IGrpcAuthServiceSession.SessionId => _sessionId;

#if ENABLE_AUTO_AUTHENTICATE
        private InternalLiteDB _internalLiteDB;
        private List<DBPlatform> _dbPlatforms;
        public GrpcAuthService(InternalLiteDB internalLiteDB)
        {
            _internalLiteDB = internalLiteDB;
            _dbPlatforms = _internalLiteDB.Table<DBPlatform>()?.ToList() ?? new List<DBPlatform>();
        }
#endif

        /// 서버 인증
        public async Task<AuthenticateResponse> AuthenticateAsync(AuthPlatform authPlatform, string authId, CancellationToken cancellationToken = default)
        {
            var req = new AuthenticateRequest();
            req.AuthPlatformList.Add(new AuthData
            {
                Platform = authPlatform,
                AuthId = authId,
            });
            AuthenticateResponse resp = null; // await ExecuteAsync(ServiceClient.AuthenticateAsync, req, cancellationToken:cancellationToken);

            // if (resp.IsSuccess)
            {
                _sessionId = resp.Data.SessionId; // 세션 아이디 저장
                _authServiceRefresh ??= new AuthServiceRefresh(this);
                _authServiceRefresh.Start();
#if ENABLE_AUTO_AUTHENTICATE
                SaveToDb(authPlatform, authId);
#endif
            }

            return resp;
        }

        /// 로그인된 소셜 Id들을 반환합니다.
        public async Task<ListResponse> ListAsync(CancellationToken cancellationToken = default)
        {
            var req = new ListRequest();
            ListResponse resp = null; // await ExecuteAsync(ServiceClient.ListAsync, req, cancellationToken:cancellationToken);
            return resp;
        }

        /// 세션을 새로고침하여 세션 만료를 방지합니다.
        internal async Task<RefreshResponse> RefreshAsync(CancellationToken cancellationToken = default)
        {
            var req = new RefreshRequest();
            RefreshResponse resp = null; // await ExecuteAsync(ServiceClient.RefreshAsync, req, cancellationToken:cancellationToken);
            return resp;
        }

        /// 계정 복구 요청을 서버에 보냅니다.
        public async Task<RestoreResponse> RestoreAsync(AuthPlatform authPlatform, string authId, CancellationToken cancellationToken = default)
        {
            var req = new RestoreRequest();
            RestoreResponse resp = null; // await ExecuteAsync(ServiceClient.RestoreAsync, req, cancellationToken:cancellationToken);
            return resp;
        }

        /// 계정 복구 기간을 두지 않고 즉시 삭제를 요청 합니다.
        public async Task<AuthDeleteResponse> DeleteAsync(CancellationToken cancellationToken = default)
        {
            var req = new AuthDeleteRequest();
            AuthDeleteResponse resp = null; // await ExecuteAsync(ServiceClient.DeleteAsync, req, cancellationToken: cancellationToken);
            // if (resp.IsSuccess)
            {
                _sessionId = string.Empty; // 세션 아이디 초기화
            }
            return resp;
        }

        /// 서버에서 계정 정보를 삭제하고 회원을 탈퇴합니다.
        public async Task<UnregisterResponse> UnregisterAsync(CancellationToken cancellationToken = default)
        {
            var req = new UnregisterRequest();
            UnregisterResponse resp = null; // await ExecuteAsync(ServiceClient.UnregisterAsync, req, cancellationToken: cancellationToken);
            // if (resp.IsSuccess)
            {
                _sessionId = string.Empty; // 세션 아이디 초기화
            }
            return resp;
        }

        /// 플랫폼 로그인 후 서버에 연동
        /// <param name="authPlatform"></param>
        /// <param name="authId"></param>
        public async Task<CreateResponse> CreateAsync(AuthPlatform authPlatform, string authId, CancellationToken cancellationToken = default)
        {
            var req = new CreateRequest();
            req.Platform = authPlatform;
            req.AuthId = authId;
            CreateResponse resp = null; // await ExecuteAsync(ServiceClient.CreateAsync, req, cancellationToken: cancellationToken);
#if ENABLE_AUTO_AUTHENTICATE
            if (resp.IsSuccess)
            {
                SaveToDb(authPlatform, authId);
            }
#endif
            return resp;
        }

#if ENABLE_AUTO_AUTHENTICATE
        /// 자동으로 인증합니다.
        /// 신규유저/기존유저 상관없이 플랫폼 Id(Android는 GPGS, iOS는 GameCenter로 자동 로그인합니다.
        /// 만약 플랫폼 Id로 로그인을 실패한다면 Device Id로 로그인합니다.
        public async Task<AuthenticateResponse> AuthenticateAsync(CancellationToken cancellationToken = default)
        {
/*
            TODO : tech-platform-auth를 디펜던시로 추가해서 구현 필요.
            1. 로컬DB에 플랫폼 ID로 로그인한게 있다면 곧바로 서버 인증
            2. 로그인한게 없다면 `tech-platform-auth` 패키지를 통해 GPGS혹은 GameCenter로 로그인 시도
            3. GPGS 혹은 GameCenter로 로그인에 성공했다면, 해당 플랫폼 ID로 서버 인증
            4. 만약 3번에서 실패했다면, Device Id로 서버 인증
*/
            var req = new AuthenticateRequest
            {
                AuthPlatformList =
                {
                    _dbPlatforms.Select(dbPlatform => new AuthData
                    {
                        Platform = dbPlatform.AuthPlatform,
                        AuthId = dbPlatform.AuthId
                    })
                }
            };

            AuthenticateResponse resp = await ExecuteAsync(ServiceClient.AuthenticateAsync, req, cancellationToken: cancellationToken);
            return resp;
        }

        private void SaveToDb(AuthPlatform platform, string authId)
        {
            var dbPlatform = new DBPlatform
            {
                AuthPlatform = platform,
                AuthId = authId
            };
            _internalLiteDB.InsertOrReplace(dbPlatform);
            _dbPlatforms.Add(dbPlatform);
        }

        /// <paramref name="authPlatform"/>이 로그인되어있는지
        public bool IsLoggedIn(AuthPlatform authPlatform)
        {
            for (var i = 0; i < _dbPlatforms.Count; i++)
            {
                if (_dbPlatforms[i].AuthPlatform == authPlatform)
                {
                    return true;
                }
            }

            return false;
        }

        /// 하나라도 로그인되어있는지
        public bool IsAnyLoggedIn()
        {
            return _dbPlatforms.Count > 0;
        }
#endif
        public void Dispose()
        {
            _authServiceRefresh?.Stop();
            _authServiceRefresh = null;
        }
    }
}
