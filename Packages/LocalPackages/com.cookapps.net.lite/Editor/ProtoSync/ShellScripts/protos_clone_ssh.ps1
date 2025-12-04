param (
    [string]$BRANCH,
    [string]$PROJ_NAME,
    [string]$TARGET_DIR,
    [string]$CLONE_DIR
)

Write-Host "BRANCH: $BRANCH"
Write-Host "PROJ_NAME: $PROJ_NAME"
Write-Host "TARGET_DIR: $TARGET_DIR"
Write-Host "CLONE_DIR: $CLONE_DIR"

# TARGET_DIR로 이동
if (!(Test-Path -Path $TARGET_DIR)) {
    Write-Host "Error: The target directory does not exist."
    exit 2
}
Set-Location -Path $TARGET_DIR

# 엉뚱한 repository 삭제 방지
if ((Test-Path -Path "$($CLONE_DIR)\.git") -or (Test-Path -Path ".git")) {
    Write-Host "Error: There is already a git repository in the target or clone directory."
    exit 3
}

# ssh-agent 시작
Start-Service ssh-agent

# ssh-add -l 명령 실행 후 메시지 기억
$ssh_add_result = ssh-add -l 2>&1

if ($ssh_add_result -like "The agent has no identities.") {
#    # 등록되어 있는 SSH 키가 없는 경우 SSH 키 추가
#    Write-Host "The agent has no identities."
#
#    # SSH 키 파일 경로 정의
#    $ed25519_key = "$HOME\.ssh\id_ed25519"
#    $rsa_key = "$HOME\.ssh\id_rsa"
#
#    # id_ed25519 파일이 있는지 확인
#    if (Test-Path $ed25519_key) {
#        Write-Host "Adding id_ed25519 key..."
#        ssh-add $ed25519_key
#        Write-Host "Adding key Done."
#    }
#    # id_ed25519 파일이 없고, id_rsa 파일이 있는지 확인
#    elseif (Test-Path $rsa_key) {
#        Write-Host "id_ed25519 not found. Adding id_rsa key..."
#        ssh-add $rsa_key
#        Write-Host "Adding key Done."
#    }
#    else {
#        Write-Host "Error: No SSH key (id_ed25519 or id_rsa) found in the $HOME\.ssh directory."
#        exit 5
#    }
    Write-Host "Error: No SSH key (id_ed25519 or id_rsa) found in the $HOME\.ssh directory."
    exit 5
}
elseif ($ssh_add_result) {
    # 등록되어 있는 SSH 키가 있는 경우 다음 단계로 바로 넘어감
    Write-Host $ssh_add_result
}
else {
    # ssh-agent 관련 문제 발생 시 종료
    Write-Host "Error: Failed to check the identities in the ssh-agent."
    exit 8
}

# hive-grpc-IDL 디렉토리 삭제
try {
    Remove-Item -Recurse -Force "$CLONE_DIR" -ErrorAction Stop
} catch {
    if ($_.Exception -is [System.IO.IOException]) {
        Write-Host "Error: IOException occurred while removing directory: $($_.Exception.Message)"
        exit 9
    } else {
        throw
    }
}

# 레포지토리 URL 정의
$REPO = "git@github.com:cookapps-devops/hive-grpc-IDL.git"

#Write-Host "$(ssh -T git@github.com)"

# ssh-agent를 통해 확보한 SSH 키를 사용하기 위해 git config 설정
git config --global core.sshCommand "C:\\Windows\\System32\\OpenSSH\\ssh.exe"

# Clone with depth
git clone --no-checkout --depth 1 --branch $BRANCH --single-branch $REPO $CLONE_DIR

# Navigate into the repository
if (! (Test-Path -Path "$CLONE_DIR")) {
    Write-Host "Error: Failed to clone the repository."
    git config --global --unset-all core.sshCommand
    exit 6
}
Set-Location -Path "$CLONE_DIR"

# Initialize sparse checkout
git sparse-checkout init --cone
git sparse-checkout set $PROJ_NAME
git checkout $BRANCH

# git config 설정 초기화
git config --global --unset-all core.sshCommand

# remove .git
Remove-Item -Recurse -Force ".git"

if (!(Test-Path -Path "$PROJ_NAME")) {
    Write-Host "Error: Project directory not found in the repository."
    exit 7
}

exit 0
