using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using Unity.Mathematics;
using Unity.VisualScripting;
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
            new int2(0, 1), new int2(0, -1), new int2(1, 0), new int2(-1, 0),
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

        public InGameTile[] GetAllTiles()
        {
            return _tiles;
        }

        public InGameTile GetRandomEmptyTile(AllianceType? allianceType = null)
        {
            var emptyTiles = _tiles.Where(t => (!allianceType.HasValue || t.View.AllianceType == allianceType.Value) && t.OccupiedCharacter == null)
                .ToList();
            if (emptyTiles.Count == 0)
            {
                return null;
            }

            var random = new System.Random();
            int randomIndex = random.Next(emptyTiles.Count);
            return emptyTiles[randomIndex];
        }
        
        public InGameTile GetPriorityEmptyTile(AllianceType? allianceType = null)
        {
            var emptyTiles = _tiles
                .Where(t => (!allianceType.HasValue || t.View.AllianceType == allianceType.Value) && t.OccupiedCharacter == null)
                .OrderByDescending(t => t.Y)
                .ThenByDescending(t => t.X)
                .ToList();

            return emptyTiles.FirstOrDefault();
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

        public int GetManhattanDistance(InGameTile from, InGameTile to)
        {
            return Mathf.Abs(from.X - to.X) + Mathf.Abs(from.Y - to.Y);
        }

        public List<InGameTile> GetTileListByManhattanDistance(InGameTile pivotTile, int distance)
        {
            List<InGameTile> tilesAtDistance = new List<InGameTile>();

            foreach (var tile in _tiles)
            {
                if (GetManhattanDistance(pivotTile, tile) == distance)
                {
                    tilesAtDistance.Add(tile);
                }
            }

            return tilesAtDistance;
        }

        public List<InGameTile> GetTileListByManhattanDistanceInRange(InGameTile pivotTile, int distance)
        {
            List<InGameTile> tilesAtDistance = new List<InGameTile>();

            foreach (var tile in _tiles)
            {
                if (GetManhattanDistance(pivotTile, tile) <= distance)
                {
                    tilesAtDistance.Add(tile);
                }
            }

            return tilesAtDistance;
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
            int manhattanDistance = int.MaxValue;

            // y축 확인 후 x축 확인
            foreach (var direction in Directions)
            {
                int2 newPos = new int2(src.X + direction.x, src.Y + direction.y);
                if (IsValidPosition(newPos))
                {
                    var neighbor = GetTile(newPos);

                    // bool isDying = (neighbor.OccupiedCharacter != null) &&
                    //                 neighbor.OccupiedCharacter.GetCurrentState() is CharacterStateDead;

                    if (neighbor.OccupiedCharacter == null /*|| isDying*/)
                    {
                        var distance = BFS(neighbor, dest); 
                        if (distance <= shortestDistance)
                        {
                            if (distance == shortestDistance)
                            {
                                int tempManhattanDistance = GetManhattanDistance(neighbor, dest);
                                if (manhattanDistance > tempManhattanDistance)
                                {
                                    shortestDistance = distance;
                                    manhattanDistance = tempManhattanDistance;
                                    bestTile = neighbor;
                                }
                            }
                            else
                            {
                                if (distance < shortestDistance)
                                {
                                    int tempManhattanDistance = GetManhattanDistance(neighbor, dest);
                                    shortestDistance = distance;
                                    manhattanDistance = tempManhattanDistance;
                                    bestTile = neighbor;
                                }
                            }
                        }
                    }
                }
            }

            // 다 막혀 있어서 갱신이 안됐으면 상하좌우 중 그나마 가장 가까운 타일을 찾는다.
            // if (bestTile == src)
            // {
            //     foreach (var direction in Directions)
            //     {
            //         int2 newPos = new int2(src.X + direction.x, src.Y + direction.y);
            //         if (IsValidPosition(newPos))
            //         {
            //             var neighbor = GetTile(newPos);
            //             if (neighbor.OccupiedCharacter == null)
            //             {
            //                 var distance = BFSByOnlyWall(neighbor, dest);
            //                 if (distance < shortestDistance)
            //                 {
            //                     shortestDistance = distance;
            //                     bestTile = neighbor;
            //                 }
            //             }
            //         }
            //     }
            // }

            return bestTile;
        }

        public int BFS(InGameTile start, InGameTile dest)
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

        public int BFSByOnlyWall(InGameTile start, InGameTile dest)
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
                    bool isCheckTile = neighbor.OccupiedCharacter != null &&
                                       neighbor.OccupiedCharacter.AllianceType == AllianceType.Wall;
                    if (!visited.Contains(neighbor) && !isCheckTile)
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

        public List<InGameTile> GetTileByCharacterDirection(CharacterController characterController, int count = 1)
        {
            bool isFront = characterController.GetCharacterView().CachedFront;
            bool isFlip = characterController.GetCharacterView().CachedFlipX;

            int directionX = characterController.CurrentTile.X;
            int directionY = characterController.CurrentTile.Y;

            int dx = 0, dy = 0;

            if (!isFront && isFlip)
            {
                dx = 1;
            }
            else if (!isFront && !isFlip)
            {
                dy = 1;
            }
            else if (isFront && isFlip)
            {
                dy = -1;
            }
            else
            {
                dx = -1;
            }

            List<InGameTile> tiles = new List<InGameTile>();
            for (int i = 0; i < count; i++)
            {
                directionX += dx;
                directionY += dy;
                var tile = _tiles.FirstOrDefault(t => t.X == directionX && t.Y == directionY);
                if (tile != null)
                {
                    tiles.Add(tile);
                }
                else
                {
                    break;
                }
            }

            return tiles;
        }

        public List<InGameTile> GetTileListByCharacterDirection(CharacterController characterController, int frontDistance, int areaSize)
        {
            bool isFront = characterController.GetCharacterView().CachedFront;
            bool isFlip = characterController.GetCharacterView().CachedFlipX;

            int directionX = characterController.CurrentTile.X;
            int directionY = characterController.CurrentTile.Y;

            List<InGameTile> tiles = new List<InGameTile>();

            int2 primaryDirection;
            List<int2> secondaryDirections = new();

            bool isX = false;
            if (!isFront && isFlip)
            {
                primaryDirection = new int2(frontDistance, 0);
                isX = true;
            }
            else if (!isFront && !isFlip)
            {
                primaryDirection = new int2(0, frontDistance);
            }
            else if (isFront && isFlip)
            {
                primaryDirection = new int2(0, -frontDistance);
            }
            else
            {
                primaryDirection = new int2(-frontDistance, 0);
                isX = true;
            }

            for (int i = 1; i <= areaSize; i++)
            {
                if (isX)
                {
                    secondaryDirections.Add(new int2(0, -i));
                    secondaryDirections.Add(new int2(0, i));
                }
                else
                {
                    secondaryDirections.Add(new int2(-i, 0));
                    secondaryDirections.Add(new int2(i, 0));
                }
            }

            int2 primaryPos = new int2(directionX, directionY) + primaryDirection;
            if (IsValidPosition(primaryPos))
            {
                tiles.Add(GetTile(primaryPos));
            }

            // Loop through areaSize
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

        public List<InGameTile> GetTileListByShapeX(InGameTile inGameTile)
        {
            return _tiles.Where(t => t.X == inGameTile.X ||  t.Y == inGameTile.Y).ToList();
        }
        
        public List<InGameTile> GetTileListByShapeX(InGameTile inGameTile, int size)
        {
            return _tiles.Where(t => 
                (t.X == inGameTile.X && (t.Y == inGameTile.Y + size || t.Y == inGameTile.Y - size)) ||
                (t.Y == inGameTile.Y && (t.X == inGameTile.X + size || t.X == inGameTile.X - size))
            ).ToList();
        }
        
        public List<InGameTile> GetTileListByShapeXInRange(InGameTile inGameTile, int size)
        {
            return _tiles.Where(t => 
                (t.X == inGameTile.X && Math.Abs(t.Y - inGameTile.Y) <= size) ||
                (t.Y == inGameTile.Y && Math.Abs(t.X - inGameTile.X) <= size)
            ).ToList();
        }
        
        public List<InGameTile> GetTileListByDiagonal(InGameTile ingameTile, int size)
        {
            return _tiles.Where(t =>
                    (t.X == ingameTile.X + size && t.Y == ingameTile.Y + size) || // 우상
                    (t.X == ingameTile.X - size && t.Y == ingameTile.Y + size) || // 좌상
                    (t.X == ingameTile.X + size && t.Y == ingameTile.Y - size) || // 우하
                    (t.X == ingameTile.X - size && t.Y == ingameTile.Y - size)    // 좌하
            ).ToList();
        }

        public List<InGameTile> GetTileListByShapeSquare(InGameTile pivot, int size)
        {
            List<InGameTile> tiles = new List<InGameTile>();
            InGameTile centerTile = pivot;

            for (int x = centerTile.X - size; x <= centerTile.X + size; x++)
            {
                for (int y = centerTile.Y - size; y <= centerTile.Y + size; y++)
                {
                    if (IsValidPosition(new int2(x, y)))
                    {
                        tiles.Add(GetTile(new int2(x, y)));
                    }
                }
            }

            return tiles;
        }

        public List<InGameTile> GetTileListByAllianceType(AllianceType type, int count)
        {
            var tiles = _tiles.Where(t => t.CheckValidTile(type, true)).ToList();
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
        
        public List<InGameTile> GetTileListByColumn(InGameTile tile)
        {
            return _tiles.Where(t => t.X == tile.X).ToList();
        }

        public List<InGameTile> GetTileListByRow(InGameTile tile)
        {
            return _tiles.Where(t => t.Y == tile.Y).ToList();
        }
        
        public List<InGameTile> GetTileListByRow(InGameTile tile, int range)
        {
            int minX = Math.Max(0, tile.X - range);
            int maxX = Math.Min(Width - 1, tile.X + range);

            return _tiles.Where(t => t.Y == tile.Y && t.X >= minX && t.X <= maxX).ToList();
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

        public InGameTile GetTileForAssassin(CharacterController characterController)
        {
            InGameTile farthestEmptyTile = null;

            for (int i = 1; i <= 10; i++)
            {
                int maxDistance = int.MinValue;

                List<InGameTile> InGameTileList = GetTileListByManhattanDistance(characterController.Target.CurrentTile, i);
                foreach (var tile in InGameTileList)
                {
                    if (tile.OccupiedCharacter != null)
                    {
                        continue;
                    }

                    int distance = GetManhattanDistance(characterController.CurrentTile, tile);
                    if (distance > maxDistance ||
                        (distance == maxDistance &&
                         ((characterController.AllianceType == AllianceType.Player && tile.Y > farthestEmptyTile.Y) ||
                          (characterController.AllianceType == AllianceType.Enemy && tile.Y < farthestEmptyTile.Y))))
                    {
                        maxDistance = distance;
                        farthestEmptyTile = tile;
                    }
                }

                if (farthestEmptyTile != null)
                {
                    break;
                }
            }

            return farthestEmptyTile;
        }
    }
}
