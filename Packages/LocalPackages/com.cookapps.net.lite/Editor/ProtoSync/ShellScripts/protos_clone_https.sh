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

# hive-grpc-IDL 디렉토리 삭제
if ! rm -rf "$CLONE_DIR" 2>/dev/null; then
    echo "Error: IOException occurred while removing directory: $CLONE_DIR"
    exit 9
fi

# 레포지토리 URL 정의
REPO="https://github.com/cookapps-devops/hive-grpc-IDL.git"

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
