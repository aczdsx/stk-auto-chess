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

            var openList = new PriorityQueue<InGameTile, int>();
            var closedList = new HashSet<InGameTile>();
            var gCosts = new Dictionary<InGameTile, int>();
            var fCosts = new Dictionary<InGameTile, int>();
            var cameFrom = new Dictionary<InGameTile, InGameTile>();

            openList.Enqueue(src, 0);
            gCosts[src] = 0;
            fCosts[src] = GetManhattanDistance(src, dest);

            while (openList.Count > 0)
            {
                var current = openList.Dequeue();

                if (current == dest)
                {
                    return ReconstructPath(cameFrom, current, src);
                }

                closedList.Add(current);

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (closedList.Contains(neighbor) || neighbor.OccupiedCharacter != null)
                    {
                        continue;
                    }

                    int tentativeGCost = gCosts[current] + GetManhattanDistance(current, neighbor);

                    if (!gCosts.ContainsKey(neighbor) || tentativeGCost < gCosts[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gCosts[neighbor] = tentativeGCost;
                        int fCost = tentativeGCost + GetManhattanDistance(neighbor, dest);
                        fCosts[neighbor] = fCost;

                        if (!openList.Contains(neighbor))
                        {
                            openList.Enqueue(neighbor, fCost);
                        }
                    }
                }
            }

            return FindBestTile(closedList, gCosts, src, dest);
        }

        private InGameTile ReconstructPath(Dictionary<InGameTile, InGameTile> cameFrom, InGameTile current, InGameTile start)
        {
            var path = new List<InGameTile>();
            while (current != start)
            {
                path.Add(current);
                current = cameFrom[current];
            }

            path.Reverse();
            return path.Count > 1 ? path[1] : path.FirstOrDefault();
        }

        private InGameTile FindBestTile(HashSet<InGameTile> closedList, Dictionary<InGameTile, int> gCosts, InGameTile src, InGameTile dest)
        {
            InGameTile bestTile = null;
            int bestCost = int.MaxValue;

            foreach (var tile in closedList)
            {
                if (tile == src)
                    continue;

                int cost = gCosts[tile] + GetManhattanDistance(tile, dest);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestTile = tile;
                }
            }

            return bestTile;
        }

        private IEnumerable<InGameTile> GetNeighbors(InGameTile tile)
        {
            // var directions = new int2[]
            // {
            //     new int2(-1, 0), new int2(1, 0), new int2(0, -1), new int2(0, 1),
            //     new int2(-1, -1), new int2(-1, 1), new int2(1, -1), new int2(1, 1)
            // };
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

    public class PriorityQueue<TElement, TPriority>
    {
        private readonly SortedDictionary<TPriority, Queue<TElement>> _dictionary = new();

        public int Count { get; private set; }

        public void Enqueue(TElement element, TPriority priority)
        {
            if (!_dictionary.TryGetValue(priority, out var elements))
            {
                elements = new Queue<TElement>();
                _dictionary[priority] = elements;
            }

            elements.Enqueue(element);
            Count++;
        }

        public TElement Dequeue()
        {
            var firstPair = _dictionary.First();
            var element = firstPair.Value.Dequeue();
            if (firstPair.Value.Count == 0)
            {
                _dictionary.Remove(firstPair.Key);
            }
            Count--;
            return element;
        }

        public bool Contains(TElement element)
        {
            return _dictionary.Values.Any(queue => queue.Contains(element));
        }
    }
}
