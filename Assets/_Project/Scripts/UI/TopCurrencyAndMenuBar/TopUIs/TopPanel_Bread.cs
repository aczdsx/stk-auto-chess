using System;
using Cysharp.Text;

public class TopPanel_Bread : TopPanelBase
{
    public override TopPanelType PanelType => TopPanelType.CloseButton;

    private void OnEnable()
    {
        UserDataManager.OnBreadChanged += OnBreadChanged;
    }

    private void OnDisable()
    {
        UserDataManager.OnBreadChanged -= OnBreadChanged;
    }

    private void OnBreadChanged(int bread)
    {
        currencyText.SetText(bread);
    }
}
