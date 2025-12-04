# Windows 10에서 git 명령어 실행 시 자동으로 SSH 키를 사용할 수 있도록 설정
# 관리자 권한으로 실행됨
Get-Service ssh-agent
Set-Service -Name ssh-agent -StartupType Automatic
Start-Sleep -Seconds 5
Start-Service ssh-agent
