using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using Unity.Mathematics;
using UnityEngine;

namespace CookApps.BattleSystem
{
    public class InGameGrid
    {
        public int Width { get; }
        public int Height { get; }

        private readonly InGameTile[] _tiles;
        private static readonly int2[] Directions =
        {
            new int2(-1, 0), new int2(1, 0), new int2(0, -1), new int2(0, 1),
        };

        public InGameGrid(int2 gridSize, InGameTileView[] views)
        {
            Width = gridSize.x;
            Height = gridSize.y;
            _tiles = new InGameTile[Width * Height];

            for (var i = 0; i < Width * Height; i++)
            {
                _tiles[i] = new InGameTile(i % Width, i / Width, views[i]);
                views[i].ID = i;
            }
        }

        public InGameTile GetTile(int2 pos)
        {
            return _tiles[pos.x + pos.y * Width];
        }

        public InGameTile GetTile(int index)
        {
            return _tiles[index];
        }

        public InGameTile GetEmptyTile()
        {
            return _tiles.FirstOrDefault(t => t.OccupiedCharacter == null);
        }

        public InGameTile[] GetManhattanDistanceTiles(InGameTile centerTile, int distance)
        {
            return _tiles.Where(tile => GetManhattanDistance(centerTile, tile) <= distance).ToArray();
        }

        public int GetManhattanDistance(InGameTile from, InGameTile to)
        {
            return Mathf.Abs(from.X - to.X) + Mathf.Abs(from.Y - to.Y);
        }

        public bool IsInRange(InGameTile from, InGameTile to, int range)
        {
            return GetManhattanDistance(from, to) <= range;
        }

        public void GetTilesInRange(InGameTile pivot, int range, BattleSystem.AttackRangeShape shape, List<InGameTile> resTiles)
        {
            foreach (var tile in _tiles)
            {
                if (GetManhattanDistance(pivot, tile) <= range * 2 - (BattleSystem.AttackRangeShape.Rectangle - shape))
                {
                    resTiles.Add(tile);
                }
            }
        }

        public InGameTile GetNextMovableTile(InGameTile src, InGameTile dest)
        {
            Debug.Log($"GetNextMovableTile: ({src.X}, {src.Y}) -> ({dest.X}, {dest.Y})");


            InGameTile bestTile = src;
            int shortestDistance = int.MaxValue;

            foreach (var direction in Directions)
            {
                int2 newPos = new int2(src.X + direction.x, src.Y + direction.y);
                if (IsValidPosition(newPos))
                {
                    var neighbor = GetTile(newPos);
                    if (neighbor.OccupiedCharacter == null)
                    {
                        var distance = BFS(neighbor, dest);
                        if (distance < shortestDistance)
                        {
                            shortestDistance = distance;
                            bestTile = neighbor;
                        }
                    }
                }
            }

            // 다 막혀 있어서 갱신이 안됐으면 상하좌우 중 그나마 가장 가까운 타일을 찾는다.
            if (bestTile == src)
            {
                foreach (var direction in Directions)
                {
                    int2 newPos = new int2(src.X + direction.x, src.Y + direction.y);
                    if (IsValidPosition(newPos))
                    {
                        var neighbor = GetTile(newPos);
                        if (neighbor.OccupiedCharacter == null)
                        {
                            var distance = GetManhattanDistance(neighbor, dest);
                            if (distance < shortestDistance)
                            {
                                shortestDistance = distance;
                                bestTile = neighbor;
                            }
                        }
                    }
                }
            }

            return bestTile;
        }

        private int BFS(InGameTile start, InGameTile dest)
        {
            var queue = new Queue<(InGameTile tile, int distance)>();
            var visited = new HashSet<InGameTile>();


            var targetPositions = new List<InGameTile>();
            foreach (var direction in Directions)
            {
                int2 neighborPos = new int2(dest.X + direction.x, dest.Y + direction.y);
                if (IsValidPosition(neighborPos))
                {
                    var neighbor = GetTile(neighborPos);
                    targetPositions.Add(neighbor);
                }
            }

            queue.Enqueue((start, 0));
            visited.Add(start);

            while (queue.Count > 0)
            {
                var (current, distance) = queue.Dequeue();

                if (targetPositions.Contains(current))
                {
                    return distance;
                }

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (!visited.Contains(neighbor) && neighbor.OccupiedCharacter == null)
                    {
                        queue.Enqueue((neighbor, distance + 1));
                        visited.Add(neighbor);
                    }
                }
            }

            return int.MaxValue; // 경로를 찾지 못한 경우
        }

        private IEnumerable<InGameTile> GetNeighbors(InGameTile tile)
        {
            foreach (var dir in Directions)
            {
                int2 newPos = new int2(tile.X + dir.x, tile.Y + dir.y);
                if (IsValidPosition(newPos))
                {
                    yield return GetTile(newPos);
                }
            }
        }

        private bool IsValidPosition(int2 pos)
        {
            return pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;
        }
    }
}
