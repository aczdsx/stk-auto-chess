using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Unity.Mathematics;
using UnityEngine;

public class InGameStage : MonoBehaviour
{
    public int2 GridSize => _gridSize;
    public InGameTileView[] TileViews => _tileViews;

    [SerializeField] private int2 _gridSize; // 임시. Spec에서 받아오거나 다른 방법으로 변경 필요
    [SerializeField] private InGameTileView[] _tileViews;
}
