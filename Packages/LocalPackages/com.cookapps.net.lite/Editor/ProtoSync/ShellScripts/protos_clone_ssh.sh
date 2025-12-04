#!/bin/bash

# 변수 초기화
BRANCH=$1
PROJ_NAME=$2
TARGET_DIR=$3
CLONE_DIR=$4

echo "BRANCH: $BRANCH"
echo "PROJ_NAME: $PROJ_NAME"
echo "TARGET_DIR: $TARGET_DIR"
echo "CLONE_DIR: $CLONE_DIR"

# TARGET_DIR로 이동
cd "$TARGET_DIR" || { echo "Error: The target directory does not exist."; exit 2; }

# 엉뚱한 repository 삭제 방지
if [[ -d "$CLONE_DIR/.git" || -d ".git" ]]; then
    echo "Error: There is already a git repository in the target or clone directory."
    exit 3
fi

# ssh-agent 시작
eval "$(ssh-agent -s)"

# ssh-add -l 명령 실행 후 메시지 기억
ssh_add_result=$(ssh-add -l 2>&1)

if [[ "$ssh_add_result" == "The agent has no identities." ]]; then
    # 등록되어 있는 SSH 키가 없는 경우 SSH 키 추가
    echo "No SSH keys found in the ssh-agent."

    # SSH 키 파일 경로 정의
    ed25519_key="$HOME/.ssh/id_ed25519"
    rsa_key="$HOME/.ssh/id_rsa"

    # id_ed25519 파일이 있는지 확인
    if [[ -f "$ed25519_key" ]]; then
        echo "Adding id_ed25519 key..."
        ssh-add "$ed25519_key"
        Write-Host "Adding key Done."
    # id_ed25519 파일이 없고, id_rsa 파일이 있는지 확인
    elif [[ -f "$rsa_key" ]]; then
        echo "id_ed25519 not found. Adding id_rsa key..."
        ssh-add "$rsa_key"
        Write-Host "Adding key Done."
    else
        echo "Error: No SSH key (id_ed25519 or id_rsa) found in the $HOME/.ssh directory."
        exit 5
    fi
elif [[ -n "$ssh_add_result" ]]; then
    # 등록되어 있는 SSH 키가 있는 경우 다음 단계로 바로 넘어감
    echo "$ssh_add_result"
else
    # ssh-agent 관련 문제 발생 시 종료
    echo "Error: Failed to check the identities in the ssh-agent."
    exit 8
fi

# hive-grpc-IDL 디렉토리 삭제
if ! rm -rf "$CLONE_DIR" 2>/dev/null; then
    echo "Error: IOException occurred while removing directory: $CLONE_DIR"
    exit 9
fi

# 레포지토리 URL 정의
REPO="git@github.com:cookapps-devops/hive-grpc-IDL.git"

#echo "$(ssh -T git@github.com)"

# Clone with depth
git clone --no-checkout --depth 1 --branch "$BRANCH" --single-branch $REPO "$CLONE_DIR"

# Navigate into the repository
if [[ ! -d "$CLONE_DIR" ]]; then
    echo "Error: Failed to clone the repository."
    exit 6
fi
cd "$CLONE_DIR" || exit 6

# Initialize sparse checkout
git sparse-checkout init --cone

git sparse-checkout set "$PROJ_NAME"

git checkout "$BRANCH"

# remove .git
rm -rf .git

if [[ ! -d "$PROJ_NAME" ]]; then
    echo "Error: Project directory not found in the repository."
    exit 7
fi

exit 0
