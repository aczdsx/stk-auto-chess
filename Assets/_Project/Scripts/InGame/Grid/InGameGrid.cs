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
        private const int RecentVisitLimit = 5;
        private const int HighVisitPenalty = 1000; // High penalty for revisiting a tile

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

        public bool IsInRange(InGameTile from, InGameTile to, int range, AttackRangeShape shape)
        {
            return GetManhattanDistance(from, to) <= range;
        }

        public void GetTilesInRange(InGameTile pivot, int range, AttackRangeShape shape, List<InGameTile> resTiles)
        {
            foreach (var tile in _tiles)
            {
                if (GetManhattanDistance(pivot, tile) <= range * 2 - (AttackRangeShape.Rectangle - shape))
                {
                    resTiles.Add(tile);
                }
            }
        }

        public InGameTile GetNextMovableTile(InGameTile src, InGameTile dest)
        {
            Debug.Log($"GetNextMovableTile: ({src.X}, {src.Y}) -> ({dest.X}, {dest.Y})");

            var priorityQueue = new PriorityQueue<InGameTile, int>();
            var closedList = new HashSet<InGameTile>();
            var openList = new Dictionary<InGameTile, int>();

            ResetTiles();

            src.G = 0;
            src.H = CalculateHeuristic(src, dest);
            priorityQueue.Enqueue(src, src.H);
            openList[src] = src.H;

            src.OccupiedCharacter.RecentlyVisitedTiles.Enqueue(src);
            if (src.OccupiedCharacter.RecentlyVisitedTiles.Count > RecentVisitLimit)
            {
                src.OccupiedCharacter.RecentlyVisitedTiles.Dequeue();
            }

            while (priorityQueue.Count > 0)
            {
                var current = priorityQueue.Dequeue();
                openList.Remove(current);

                if (current == dest)
                {
                    return TracePathToSource(src, current);
                }

                closedList.Add(current);

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (closedList.Contains(neighbor) || neighbor.OccupiedCharacter != null)
                    {
                        continue;
                    }

                    int tentativeGCost = current.G + GetManhattanDistance(current, neighbor);

                    if (tentativeGCost < neighbor.G)
                    {
                        neighbor.cameFrom = current;
                        neighbor.G = tentativeGCost;
                        neighbor.H = tentativeGCost + CalculateHeuristic(neighbor, dest);

                        if (!openList.ContainsKey(neighbor))
                        {
                            priorityQueue.Enqueue(neighbor, neighbor.H);
                            openList[neighbor] = neighbor.H;
                        }
                    }
                }
            }

            return FindBestTile(closedList, src);
        }

        private void ResetTiles()
        {
            foreach (var tile in _tiles)
            {
                tile.G = int.MaxValue;
                tile.H = int.MaxValue;
                tile.cameFrom = null;
            }
        }

        private int CalculateHeuristic(InGameTile tile, InGameTile dest)
        {
            int heuristic = GetManhattanDistance(tile, dest);
            if (tile.OccupiedCharacter != null)
            {
                if (tile.OccupiedCharacter.RecentlyVisitedTiles.Contains(tile))
                {
                    heuristic += HighVisitPenalty;
                }
            }
            return heuristic;
        }

        private InGameTile TracePathToSource(InGameTile src, InGameTile current)
        {
            while (current.cameFrom != src)
            {
                current = current.cameFrom;
            }
            return current;
        }

        private InGameTile FindBestTile(HashSet<InGameTile> closedList, InGameTile src)
        {
            return closedList.Where(tile => tile != src && !src.OccupiedCharacter.RecentlyVisitedTiles.Contains(tile))
                .OrderBy(tile => tile.H)
                .FirstOrDefault() ?? src;
        }

        private IEnumerable<InGameTile> GetNeighbors(InGameTile tile)
        {
            var directions = new[]
            {
                new int2(-1, 0), new int2(1, 0), new int2(0, -1), new int2(0, 1),
            };

            foreach (var dir in directions)
            {
                int2 newPos = new int2(tile.X + dir.x, tile.Y + dir.y);
                if (newPos.x >= 0 && newPos.x < Width && newPos.y >= 0 && newPos.y < Height)
                {
                    yield return GetTile(newPos);
                }
            }
        }
    }
}
