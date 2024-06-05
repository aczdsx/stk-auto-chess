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

    public void ChangeBoardColor(Color color)
    {
        foreach (var tileView in _tileViews)
        {
            tileView.ChangeColor(color);
        }
    }

    public async UniTask GraduallyChangeBoardColor(Color targetColor, float duration)
    {
        float elapsedTime = 0f;
        Color[] initialColors = new Color[_tileViews.Length];

        for (int i = 0; i < _tileViews.Length; i++)
        {
            initialColors[i] = _tileViews[i].GetColor();
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            for (int i = 0; i < _tileViews.Length; i++)
            {
                Color newColor = Color.Lerp(initialColors[i], targetColor, t);
                _tileViews[i].ChangeColor(newColor);
            }

            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        ChangeBoardColor(targetColor);
    }
}
