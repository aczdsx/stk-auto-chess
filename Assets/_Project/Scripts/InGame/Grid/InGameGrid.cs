using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using Unity.Mathematics;
using UnityEngine;

namespace CookApps.BattleSystem
{
    public class InGameGrid
    {
        private InGameTile[] tiles;
        public int Width { get; }
        public int Height { get; }

        public InGameGrid(int2 gridSize, InGameTileView[] views)
        {
            Width = gridSize.x;
            Height = gridSize.y;
            tiles = new InGameTile[Width * Height];
            for (var i = 0; i < Width * Height; i++)
            {
                tiles[i] = new InGameTile(i % Width, i / Width, views[i]);
                views[i].ID = i;
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

        public int GetManhattanDistance(InGameTile from, InGameTile dest)
        {
            return Mathf.Abs(from.X - dest.X) + Mathf.Abs(from.Y - dest.Y);
        }

        public bool IsInRange(InGameTile from, InGameTile dest, int range, AttackRangeShape shape)
        {
            range = 1; //[TODO] 임시 코드 나중에 삭제
            var distance = GetManhattanDistance(from, dest);
            return distance <= range;
        }


        public void GetTilesInRange(InGameTile pivot, int range, AttackRangeShape shape, List<InGameTile> resTiles)
        {
            for (var i = 0; i < Width * Height; i++)
            {
                InGameTile tile = GetTile(i);
                var distance = GetManhattanDistance(pivot, tile);
                var allowDistance = range * 2 - (AttackRangeShape.Rectangle - shape);
                if (distance <= allowDistance)
                {
                    resTiles.Add(tile);
                }
            }
        }

        public InGameTile GetNextMovableTile(InGameTile src, InGameTile dest)
        {
            Debug.Log($"GetNextMovableTile : ({src.X}, {src.Y}) -> ({dest.X}, {dest.Y})");

            var priorityQueue = new PriorityQueue<InGameTile, int>();
            var closedList = new HashSet<InGameTile>();

            foreach (var tile in tiles)
            {
                tile.G = int.MaxValue;
                tile.H = int.MaxValue;
                tile.cameFrom = null;
            }

            src.G = 0;
            src.H = GetManhattanDistance(src, dest);
            priorityQueue.Enqueue(src, src.H);

            while (priorityQueue.Count > 0)
            {
                var current = priorityQueue.Dequeue();

                if (current == dest)
                {
                    while (current.cameFrom != src)
                    {
                        current = current.cameFrom;
                    }
                    return current;
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
                        neighbor.H = tentativeGCost + GetManhattanDistance(neighbor, dest);

                        if (!priorityQueue.Contains(neighbor))
                        {
                            priorityQueue.Enqueue(neighbor, neighbor.H);
                        }
                    }
                }
            }

            return FindBestTile(closedList, src);
        }

        private InGameTile FindBestTile(HashSet<InGameTile> closedList, InGameTile src)
        {
            InGameTile bestTile = null;
            int bestFScore = int.MaxValue;

            foreach (var tile in closedList)
            {
                if (tile == src) continue;

                if (tile.H < bestFScore)
                {
                    bestFScore = tile.H;
                    bestTile = tile;
                }
            }

            return bestTile ?? src;
        }


        private IEnumerable<InGameTile> GetNeighbors(InGameTile tile)
        {
            var directions = new int2[]
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
