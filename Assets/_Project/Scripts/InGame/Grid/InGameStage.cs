using CookApps.AutoBattler;
using Unity.Mathematics;
using UnityEngine;

public class InGameStage : MonoBehaviour
{
    public int2 GridSize => _gridSize;
    public InGameTileView[] TileViews => _tileViews;
    
    [SerializeField] private int2 _gridSize; // 임시. Spec에서 받아오거나 다른 방법으로 변경 필요
    [SerializeField] private InGameTileView[] _tileViews;
    
    void SetStageTiles()
    {
        
    }
    
    public void ChangeTile()
    {
        float tileX = 1.0f;
        float tileY = 1.0f;

        for (int i = 0; i < _gridSize.x; i++)
        {
            for (int j = 0; j < _gridSize.y; j++)
            {
                int index = i * _gridSize.y + j; // 2차원 그리드를 1차원 배열 인덱스로 변환합니다.
                if (index < _tileViews.Length)
                {
                    // 타일의 위치를 계산하고, 이를 이용해 transform을 업데이트합니다.
                    _tileViews[index].transform.position = new Vector3(i * tileX, 0, j * tileY);
                }
            }
        }
    }
    
    public void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 150, 100), "Spread Tiles"))
        {
            ChangeTile();
        }
    }
}
