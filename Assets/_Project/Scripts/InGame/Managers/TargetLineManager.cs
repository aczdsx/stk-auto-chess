using System;
using System.Collections.Generic;

namespace CookApps.BattleSystem
{
    public class TargetLineManager
    {
        private List<InGameVfxTargetLine> _playerTargetLines = new();
        private List<InGameVfxTargetLine> _enemyTargetLines = new();
        private CharacterController _focusedCharacter;
        private InGameTile _focusedCharacterPreviewTile;

        private readonly Func<bool, List<CharacterController>> _getCharacterList;
        private readonly Func<CharacterController, CharacterController> _getTarget;

        public TargetLineManager(
            Func<bool, List<CharacterController>> getCharacterList,
            Func<CharacterController, CharacterController> getTarget)
        {
            _getCharacterList = getCharacterList;
            _getTarget = getTarget;
        }

        public void DrawPlayerLine(bool isPlayer)
        {
            if (_focusedCharacter != null)
                DrawFocusedTargetLines(isPlayer);
            else
                DrawAllTargetLines(isPlayer);
        }

        private void DrawAllTargetLines(bool isPlayer)
        {
            var characterControllers = _getCharacterList(isPlayer);
            var targetLines = isPlayer ? _playerTargetLines : _enemyTargetLines;

            foreach (var anyCharacter in characterControllers)
            {
                var target = _getTarget(anyCharacter);
                if (target != null)
                    DrawOrReuseLine(anyCharacter, target, isPlayer, targetLines);
            }
        }

        private void DrawFocusedTargetLines(bool isPlayer)
        {
            var characterControllers = _getCharacterList(isPlayer);
            var targetLines = isPlayer ? _playerTargetLines : _enemyTargetLines;

            // 프리뷰 타일 임시 적용
            InGameTile savedTile = null;
            if (_focusedCharacterPreviewTile != null)
            {
                savedTile = _focusedCharacter.CurrentTile;
                _focusedCharacter.CurrentTile = _focusedCharacterPreviewTile;
            }

            foreach (var anyCharacter in characterControllers)
            {
                var target = _getTarget(anyCharacter);
                if (target == null) continue;
                if (anyCharacter != _focusedCharacter && target != _focusedCharacter) continue;

                DrawOrReuseLine(anyCharacter, target, isPlayer, targetLines);
            }

            // 원래 타일 복원
            if (savedTile != null)
                _focusedCharacter.CurrentTile = savedTile;
        }

        private void DrawOrReuseLine(CharacterController source, CharacterController target, bool isPlayer, List<InGameVfxTargetLine> targetLines)
        {
            InGameVfxTargetLine targetLine = null;
            foreach (var line in targetLines)
            {
                if (!line.CachedGo.activeSelf)
                {
                    targetLine = line;
                    break;
                }
            }

            if (targetLine == null)
            {
                targetLine = source.SetLine(target, isPlayer, (tl) => tl.SetActiveObject(false));
                targetLines.Add(targetLine);
            }
            else
            {
                source.ReUseLine(targetLine, target, isPlayer, (tl) => tl.SetActiveObject(false));
            }
        }

        public void SetFocusedCharacter(CharacterController character)
        {
            _focusedCharacter = character;
            _focusedCharacterPreviewTile = null;
            HideAllTargetLines();
        }

        public void SetFocusedCharacterPreviewTile(InGameTile tile)
        {
            _focusedCharacterPreviewTile = tile;
            HideAllTargetLines();
            DrawPlayerLine(true);
            DrawPlayerLine(false);
        }

        public void ClearFocusedCharacter()
        {
            HideAllTargetLines();
            _focusedCharacter = null;
            _focusedCharacterPreviewTile = null;
        }

        private void HideAllTargetLines()
        {
            foreach (var line in _playerTargetLines)
                if (line.CachedGo.activeSelf) line.SetActiveObject(false);
            foreach (var line in _enemyTargetLines)
                if (line.CachedGo.activeSelf) line.SetActiveObject(false);
        }

        public void ClearAll()
        {
            foreach (var line in _playerTargetLines) line.Remove();
            foreach (var line in _enemyTargetLines) line.Remove();
            _playerTargetLines.Clear();
            _enemyTargetLines.Clear();
        }
    }
}
