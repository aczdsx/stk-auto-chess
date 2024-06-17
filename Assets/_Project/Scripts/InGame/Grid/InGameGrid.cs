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

        public InGameTile GetRandomEmptyTile(AllianceType allianceType)
        {
            var emptyTiles = _tiles.Where(t => t.View.AllianceType == allianceType && t.OccupiedCharacter == null)
                .ToList();
            if (emptyTiles.Count == 0)
            {
                return null;
            }

            var random = new System.Random();
            int randomIndex = random.Next(emptyTiles.Count);
            return emptyTiles[randomIndex];
        }

        public InGameTile GetRecommandedTile(SpecCharacter spec)
        {
            int midPoint = Width / 2;
            int[] xOrder = Enumerable.Range(0, Width)
                .Select(i => midPoint - i >= 0 ? midPoint - i : i - midPoint)
                .ToArray();
            int yPosition = 0;

            if (spec.character_position_type == CharacterPositionType.TANK ||
                spec.character_position_type == CharacterPositionType.GUARDIAN)
            {
                yPosition = 2;
            }
            else if (spec.character_position_type == CharacterPositionType.WIZARD ||
                     spec.character_position_type == CharacterPositionType.SUPPORTER)
            {
                yPosition = 1;
            }
            else if (spec.character_position_type == CharacterPositionType.RANGER ||
                     spec.character_position_type == CharacterPositionType.ASSASSIN)
            {
                yPosition = 0;
            }

            foreach (int x in xOrder)
            {
                var tile = _tiles.FirstOrDefault(t => t.X == x && t.Y == yPosition && t.OccupiedCharacter == null);
                if (tile != null)
                {
                    return tile;
                }
            }

            return GetRandomEmptyTile(AllianceType.Player);
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

        public void GetTilesInRange(InGameTile pivot, int range, BattleSystem.AttackRangeShape shape,
            List<InGameTile> resTiles)
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

        public InGameTile GetTileByCharacterDirection(CharacterController characterController)
        {
            bool isFront = characterController.GetCharacterView().CachedFront;
            bool isFlip = characterController.GetCharacterView().CachedFlipX;

            int directionX = characterController.CurrentTile.X;
            int directionY = characterController.CurrentTile.Y;

            if (!isFront && isFlip)
            {
                directionX += 1;
            }
            else if (!isFront && !isFlip)
            {
                directionY += 1;
            }
            else if (isFront && isFlip)
            {
                directionY -= 1;
            }
            else
            {
                directionX -= 1;
            }

            return _tiles.FirstOrDefault(t => t.X == directionX && t.Y == directionY);
        }

        public List<InGameTile> GetTileListByCharacterDirection(CharacterController characterController)
        {
            bool isFront = characterController.GetCharacterView().CachedFront;
            bool isFlip = characterController.GetCharacterView().CachedFlipX;

            int directionX = characterController.CurrentTile.X;
            int directionY = characterController.CurrentTile.Y;

            List<InGameTile> tiles = new List<InGameTile>();

            int2 primaryDirection;
            int2[] secondaryDirections;

            if (!isFront && isFlip)
            {
                primaryDirection = new int2(1, 0);
                secondaryDirections = new int2[] {new int2(0, -1), new int2(0, 1)};
            }
            else if (!isFront && !isFlip)
            {
                primaryDirection = new int2(0, 1);
                secondaryDirections = new int2[] {new int2(-1, 0), new int2(1, 0)};
            }
            else if (isFront && isFlip)
            {
                primaryDirection = new int2(0, -1);
                secondaryDirections = new int2[] {new int2(-1, 0), new int2(1, 0)};
            }
            else
            {
                primaryDirection = new int2(-1, 0);
                secondaryDirections = new int2[] {new int2(0, -1), new int2(0, 1)};
            }

            int2 primaryPos = new int2(directionX, directionY) + primaryDirection;
            if (IsValidPosition(primaryPos))
            {
                tiles.Add(GetTile(primaryPos));
            }

            foreach (var offset in secondaryDirections)
            {
                int2 secondaryPos = primaryPos + offset;
                if (IsValidPosition(secondaryPos))
                {
                    tiles.Add(GetTile(secondaryPos));
                }
            }

            return tiles;
        }

        public List<InGameTile> GetTileListByAllianceType(AllianceType type, int count)
        {
            var tiles = _tiles.Where(t => t.OccupiedCharacter != null && t.OccupiedCharacter.AllianceType == type)
                .ToList();
            if (tiles.Count == 0)
            {
                return null;
            }
            else if (tiles.Count < count)
            {
                return tiles;
            }
            else
            {
                return tiles.Take(count).ToList();
            }
        }

        public List<InGameTile> GetTileListByRow(InGameTile tile)
        {
            return _tiles.Where(t => t.Y == tile.Y).ToList();
        }

        public List<InGameTile> GetTileListByNearest(InGameTile tile)
        {
            return _tiles.OrderBy(t => GetManhattanDistance(tile, t)).ToList();
        }

        public InGameTile GetTileForKnockBack(InGameTile attackerTile, InGameTile targetTile, int count)
        {
            int2 direction = new int2(targetTile.X - attackerTile.X, targetTile.Y - attackerTile.Y);

            direction.x = direction.x != 0 ? direction.x / Math.Abs(direction.x) : 0;
            direction.y = direction.y != 0 ? direction.y / Math.Abs(direction.y) : 0;

            InGameTile lastValidTile = targetTile;

            for (int i = 1; i <= count; i++)
            {
                int2 newPos = new int2(targetTile.X + direction.x * i, targetTile.Y + direction.y * i);
                if (IsValidPosition(newPos))
                {
                    InGameTile tile = GetTile(newPos);
                    if (tile.OccupiedCharacter == null)
                    {
                        lastValidTile = tile;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            return lastValidTile;
        }

        public InGameTile GetTileForAssassin(InGameTile targetTile)
        {
            InGameTile nearestEmptyTile = null;
            int minDistance = int.MaxValue;

            foreach (var tile in _tiles)
            {
                if (tile.OccupiedCharacter != null)
                {
                    continue;
                }

                int distance = GetManhattanDistance(targetTile, tile);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestEmptyTile = tile;
                }
            }

            return nearestEmptyTile;
        }
    }
}
