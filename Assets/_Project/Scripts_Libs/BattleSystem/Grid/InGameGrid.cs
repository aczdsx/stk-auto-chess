using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace CookApps.TeamBattle.BattleSystem
{
    public class InGameGrid
    {
        private InGameTile[] tiles;
        public int Width { get; }
        public int Height { get; }

        public InGameGrid(int2 gridSize)
        {
            Width = gridSize.x;
            Height = gridSize.y;
            tiles = new InGameTile[Width * Height];
            for (var i = 0; i < Width * Height; i++)
            {
                tiles[i] = new InGameTile(i % Width, i / Width);
            }
        }

        public InGameTile GetTile(int2 pos)
        {
            return tiles[pos.x + pos.y * Width];
        }

        public InGameTile GetTile(int index)
        {
            return tiles[index];
        }

        public int GetRange(InGameTile from, InGameTile dest)
        {
            return Mathf.Abs(from.X - dest.X) + Mathf.Abs(from.Y - dest.Y);
        }

        public void GetTilesInRange(InGameTile pivot, int range, List<InGameTile> tiles)
        {
            for (var i = 0; i < Width * Height; i++)
            {
                InGameTile tile = GetTile(i);
                if (GetRange(pivot, tile) <= range)
                {
                    tiles.Add(tile);
                }
            }
        }

        /// <summary>
        /// occupied된 타일을 제외한 다음 이동 가능한 타일을 반환합니다.
        /// scanType == ScanType.Nearest인 경우 src 기준으로 8방향 중에서 dest에 가장 가까운 타일을 반환합니다.
        /// scanType == ScanType.Farthest인 경우 dest 기준으로 8방향 중에서 src에서 가장 먼 타일을 반환합니다.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        /// <param name="scanType"></param>
        /// <returns></returns>
        public InGameTile GetNextMovableTile(InGameTile src, InGameTile dest, ScanType scanType)
        {
            InGameTile nextTile = null;
            int minRange = int.MaxValue;
            for (var i = 0; i < 8; i++)
            {
                int2 pos;
                if (scanType == ScanType.Nearest)
                {
                    pos = new int2(src.X + i % 3 - 1, src.Y + i / 3 - 1);
                }
                else if (scanType == ScanType.Farthest)
                {
                    pos = new int2(dest.X + i % 3 - 1, dest.Y + i / 3 - 1);
                }
                else
                {
                    continue;
                }

                if (pos.x < 0 || pos.x >= Width || pos.y < 0 || pos.y >= Height)
                {
                    continue;
                }

                InGameTile tile = GetTile(pos);
                if (tile.IsOccupied())
                {
                    continue;
                }

                int range = GetRange(tile, dest);
                if (scanType == ScanType.Nearest)
                {
                    if (range < minRange)
                    {
                        nextTile = tile;
                        minRange = range;
                    }
                }
                else
                {
                    if (range > minRange)
                    {
                        nextTile = tile;
                        minRange = range;
                    }
                }
            }

            return nextTile;
        }
    }
}
