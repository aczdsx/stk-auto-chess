using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

public class InGameStage : MonoBehaviour
{
    public int2 GridSize => _gridSize;
    public InGameTileView[] TileViews => _tileViews;

    [SerializeField] private int2 _gridSize; // 임시. Spec에서 받아오거나 다른 방법으로 변경 필요
    [SerializeField] private InGameTileView[] _tileViews;

    public void ChangeBoardColor(List<InGameTileView> tileViews, Color color)
    {
        foreach (var tileView in tileViews)
        {
            tileView.ChangeColor(color);
        }
    }

    public async UniTask GraduallyChangeBoardColor(Color targetColor, float duration, bool isOnlyPlayerBoard = false)
    {
        List<InGameTileView> tileViews = new List<InGameTileView>();
        if (isOnlyPlayerBoard)
            tileViews.AddRange(_tileViews.Where(tileView => tileView.AllianceType == AllianceType.Player));
        else
            tileViews.AddRange(_tileViews);
        
        float elapsedTime = 0f;
        Color[] initialColors = new Color[tileViews.Count];

        for (int i = 0; i < tileViews.Count; i++)
        {
            initialColors[i] = tileViews[i].GetColor();
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            for (int i = 0; i < tileViews.Count; i++)
            {
                Color newColor = Color.Lerp(initialColors[i], targetColor, t);
                tileViews[i].ChangeColor(newColor);
            }

            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        ChangeBoardColor(tileViews, targetColor);
    }
}
