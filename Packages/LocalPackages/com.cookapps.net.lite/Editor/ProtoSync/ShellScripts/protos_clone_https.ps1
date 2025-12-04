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
$REPO = "https://github.com/cookapps-devops/hive-grpc-IDL.git"

# Clone with depth
git clone --no-checkout --depth 1 --branch $BRANCH --single-branch $REPO $CLONE_DIR

# Navigate into the repository
if (! (Test-Path -Path "$CLONE_DIR")) {
    Write-Host "Error: Failed to clone the repository."
    exit 6
}
Set-Location -Path "$CLONE_DIR"

# Initialize sparse checkout
git sparse-checkout init --cone
git sparse-checkout set $PROJ_NAME
git checkout $BRANCH

# remove .git
Remove-Item -Recurse -Force ".git"

if (!(Test-Path -Path "$PROJ_NAME")) {
    Write-Host "Error: Project directory not found in the repository."
    exit 7
}

exit 0
