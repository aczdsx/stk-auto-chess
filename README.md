# CookApps 유니티 프로젝트 탬플릿
유니티로 게임을 제작할 때 Assets폴더 하위에 커스텀한 폴더들을 생성하고 게임 리소스들을 배치합니다. 하지만 작업자의 성향에 따라 폴더 구조가 상이하게 되어 있습니다. 이런 상황은 작업자가 서로 다른 프로젝트를 열어보았을 때 쉽게 파악할 수 없고, 필요한 에셋을 찾기 어려울 수 밖에 없습니다.

그리고 유니티에는 수많은 설정값들이 있습니다. 이중에는 프로젝트에는 필요없지만 활성화 되어있는 설정도 있고, 최적화에 좋은 설정들이 비활성화 되어 있는 것도 있습니다. 

이러한 이유로 이 탬플릿이 제작되었습니다. 

히스토리가 궁금하다면 [컨플루언스](https://cookapps.atlassian.net/wiki/spaces/TST/pages/25195352863)를 참고해주세요.

> Unity 2021.3.0f1으로 제작되었지만 그 이상의 버전에서 사용 가능합니다.

# 사용방법
![Image](https://user-images.githubusercontent.com/95894326/187809539-ad4fad65-d0ec-40f9-912a-c15969002912.png)  
github에서 레파지토리를 생성할 때 template으로 `tech-unity-project-template`을 선택하기만 하면 됩니다. 


# 이 탬플릿에는 아래의 것들을 미리 세팅해둡니다.

### 프로젝트 폴더 구조
![Image](https://user-images.githubusercontent.com/95894326/187809584-a368704d-d6dc-4fe3-8e5b-cd013dbffa80.png)  
_Project폴더 하위에 필요한 폴더를 생성했습니다. 이 구조는 각 스튜디오의 프로젝트들을 참고하여 공통된 폴더를 기반으로 제작되었습니다.

Sounds폴더의 SFX파일들은 아래 내용을 참고해주세요.
* `SFX_over_200kb`폴더에는 200kb 이상의 파일을 넣어주세요.
* `SFX_under_200kb`폴더에는 200kb 미만의 파일을 넣어주세요.

자세한 내용은 [오디오 설정하기](https://cookapps.atlassian.net/wiki/spaces/TST/pages/25218222531) 페이지를 참고해주세요. 

---

### preset 파일
![Image](https://user-images.githubusercontent.com/95894326/187809572-dc1212e7-41de-44c0-a3ce-ad26ab467207.png)  
preset은 에셋을 임포트할 때의 기본 설정을 명세해놓은 파일입니다. preset파일의 폴더 위치에 따라 임포트한 에셋이 어떤 preset을 따를지를 결정합니다.

이 기능은 기술지원팀의 `CookApps Postprocessor` 패키지를 사용합니다. 

자세한 내용은 [매뉴얼](http://docs.tech.cookapps.com/com.cookapps.postprocessor/)을 확인해주세요.

---

### Unity Project Settings
![Image](https://user-images.githubusercontent.com/95894326/187809579-510586fb-fe0d-47ae-bc0d-223ee731890a.png)  
유니티에서 기본적으로 설정되면 좋을 Project Settings를 설정해둡니다.

이 기능은 기술지원팀의 `CookApps Unity Settings` 패키지를 사용합니다.

자세한 내용은 [매뉴얼](http://docs.tech.cookapps.com/com.cookapps.unitysettings/)을 확인해주세요.
