using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookApps.TeamBattle.UI
{
    #region Horizontal
    internal abstract class HorizontalTableViewCalcImpl : ITableViewCalcImpl
    {
        public Vector2 CalcContentSizeAndFillGroups(Vector2 viewRectSize, Vector2 spacing, List<TableView.CellGroup> groups, Vector2[] itemSizes)
        {
            // var contentSize = Vector2.zero;
            var contentSize = new Vector2(0, viewRectSize.y);
            Vector2 position = Vector2.zero;
            var currGroup = new TableView.CellGroup
            {
                rect = new Rect(position, itemSizes[0]),
                itemSizes = new List<Vector2> {itemSizes[0]},
                startIdx = 0,
                endIdx = 0,
            };

            float maxHeight = currGroup.rect.height;
            for (var i = 1; i < itemSizes.Length; i++)
            {
                Vector2 itemSize = itemSizes[i];
                float postSize = currGroup.rect.size.y + itemSize.y + spacing.y;
                float maxSize = viewRectSize.y;
                if (postSize > maxSize)
                {
                    AddPosition(ref position, currGroup.rect.width, spacing.x);
                    groups.Add(currGroup);
                    contentSize.x = currGroup.rect.size.x + contentSize.x + spacing.x;
                    // contentSize.y = Mathf.Max(currGroup.rect.size.y, contentSize.y);
                    maxHeight = Mathf.Max(maxHeight, itemSize.y);
                    currGroup = new TableView.CellGroup
                    {
                        rect = new Rect(position, itemSize),
                        itemSizes = new List<Vector2> {itemSize},
                        startIdx = i,
                        endIdx = i,
                    };
                }
                else
                {
                    Rect rect = currGroup.rect;
                    rect.width = Mathf.Max(rect.width, itemSize.x);
                    rect.height += itemSize.y + spacing.y;
                    maxHeight = Mathf.Max(maxHeight, rect.height);
                    currGroup.itemSizes.Add(itemSize);
                    currGroup.rect = rect;
                    currGroup.endIdx = i;
                }
            }

            groups.Add(currGroup);
            for (var i = 0; i < groups.Count; i++)
            {
                TableView.CellGroup group = groups[i];
                group.rect.height = maxHeight;
                groups[i] = group;
            }

            contentSize.x = currGroup.rect.size.x + contentSize.x;
            // contentSize.y = Mathf.Max(currGroup.rect.size.y, contentSize.y);
            return contentSize;
        }

        protected abstract void AddPosition(ref Vector2 position, float width, float spacing);

        public Vector2 GetFocusTargetPosition(Rect viewRectRect, Vector2 contentRectSize, float posRate, TableView.CellGroup group)
        {
            Vector2 targetPos = Vector2.zero;
            if (viewRectRect.width > contentRectSize.x)
            {
                return targetPos;
            }

            targetPos.x = -group.rect.min.x;
            float posDiff = posRate * (viewRectRect.width - group.rect.size.x);
            AddPositionToTargetPosition(ref targetPos, posDiff, viewRectRect, contentRectSize);

            return targetPos;
        }

        protected abstract void AddPositionToTargetPosition(ref Vector2 targetPos, float posDiff, Rect viewRectRect, Vector2 contentRectSize);

        public abstract bool IsContainGroup(TableView.CellGroup group, Vector2 startPos, Vector2 endPos, Vector2 spacing);
        public abstract void CalcContentStartEndPosition(ref Vector2 startPos, ref Vector2 endPos, Vector4 viewportMargin, Vector2 viewRectSize);
        public abstract (Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) GetItemAnchorAndPivot();
        public abstract Rect GetItemRect(TableView.CellGroup group, int groupIdx, Vector2 spacing);
        public abstract void ArrangeItems(ref TableView.CellGroup group, Vector2 spacing);

        public (bool, Vector2) ReCalcGroupSize(ref TableView.CellGroup group, int itemIdx, Vector2 itemSize)
        {
            Vector2 size = group.rect.size;
            int groupIdx = itemIdx - group.startIdx;
            group.itemSizes[groupIdx] = itemSize;
            var biggestSize = 0f;
            for (var i = 0; i < group.itemSizes.Count; i++)
            {
                if (group.itemSizes[i].x > biggestSize)
                {
                    biggestSize = group.itemSizes[i].x;
                }
            }

            if (Mathf.Approximately(size.x, biggestSize))
            {
                return (false, Vector2.zero);
            }

            float prevSize = size.x;
            size.x = biggestSize;
            group.rect.size = size;
            return (true, new Vector2(biggestSize - prevSize, 0f));
        }

        public abstract void ShiftPosition(ref TableView.CellGroup group, Vector2 shiftPos);
        public abstract Rect GetRangeGroupRect(List<TableView.CellGroup> groups, int startIdx, int endIdx);
    }

    internal abstract class HorizontalLeftTableViewCalcImpl : HorizontalTableViewCalcImpl
    {
        protected override void AddPosition(ref Vector2 position, float width, float spacing)
        {
            position.x += width + spacing;
        }

        protected override void AddPositionToTargetPosition(ref Vector2 targetPos, float posDiff, Rect viewRectRect, Vector2 contentRectSize)
        {
            targetPos.x += posDiff;
            targetPos.x = Mathf.Clamp(targetPos.x, viewRectRect.width - contentRectSize.x, 0f);
        }

        public override bool IsContainGroup(TableView.CellGroup group, Vector2 startPos, Vector2 endPos, Vector2 spacing)
        {
            float minPos = group.rect.min.x - spacing.x;
            float maxPos = group.rect.min.x + (group.rect.size.x + spacing.x);
            return (startPos.x < minPos && minPos < endPos.x) || (startPos.x < maxPos && maxPos < endPos.x);
        }

        public override void CalcContentStartEndPosition(ref Vector2 startPos, ref Vector2 endPos, Vector4 viewportMargin, Vector2 viewRectSize)
        {
            startPos.x = -startPos.x - viewportMargin.x;
            endPos.x = startPos.x + viewRectSize.x;
        }

        public override void ShiftPosition(ref TableView.CellGroup group, Vector2 shiftPos)
        {
            group.rect.position += shiftPos;
            if (group.isShown)
            {
                foreach (RectTransform groupItem in group.items)
                {
                    groupItem.localPosition += new Vector3(shiftPos.x, shiftPos.y);
                }
            }
        }

        public override Rect GetRangeGroupRect(List<TableView.CellGroup> groups, int startIdx, int endIdx)
        {
            Rect ret = groups[startIdx].rect;
            for (int i = startIdx + 1; i <= endIdx; i++)
            {
                Rect groupRect = groups[i].rect;
                ret.size = new Vector2(
                    groupRect.position.x - ret.position.x + groupRect.size.x,
                    Mathf.Max(ret.size.y, groupRect.size.y)
                );
            }

            return ret;
        }
    }

    internal sealed class HorizontalLeftTableViewWithVerticalTopCalcImpl : HorizontalLeftTableViewCalcImpl
    {
        public override (Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) GetItemAnchorAndPivot()
        {
            return (
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0.5f, 1f)
            );
        }

        public override Rect GetItemRect(TableView.CellGroup group, int groupIdx, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.x += group.rect.size.x * 0.5f;
            refPoint.y = 0;
            Vector2 itemSize = Vector2.zero;

            for (var i = 0; i < group.itemSizes.Count; i++)
            {
                if (i == groupIdx)
                {
                    break;
                }

                itemSize = group.itemSizes[i];
                refPoint.y -= itemSize.y + spacing.y;
            }

            (_, _, Vector2 pivot) = GetItemAnchorAndPivot();
            var actualPosition = new Vector2(refPoint.x - (itemSize.x * pivot.x), refPoint.y - (itemSize.y * pivot.y));
            return new Rect(actualPosition, itemSize);
        }

        public override void ArrangeItems(ref TableView.CellGroup group, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.x += group.rect.size.x * 0.5f;
            refPoint.y = 0;

            for (var i = 0; i < group.items.Length; i++)
            {
                group.items[i].anchoredPosition = refPoint;
                Vector2 itemSize = group.items[i].rect.size;
                refPoint.y -= itemSize.y + spacing.y;
            }
        }
    }

    internal sealed class HorizontalLeftTableViewWithVerticalCenterCalcImpl : HorizontalLeftTableViewCalcImpl
    {
        public override (Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) GetItemAnchorAndPivot()
        {
            return (
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f)
            );
        }

        public override Rect GetItemRect(TableView.CellGroup group, int groupIdx, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.x += group.rect.size.x * 0.5f;
            refPoint.y = group.rect.size.y * 0.5f;
            Vector2 itemSize = Vector2.zero;

            for (var i = 0; i < group.itemSizes.Count; i++)
            {
                itemSize = group.itemSizes[i];
                refPoint.y -= itemSize.y * 0.5f;
                if (i == groupIdx)
                {
                    break;
                }

                refPoint.y -= (itemSize.y * 0.5f) + spacing.y;
            }

            (_, _, Vector2 pivot) = GetItemAnchorAndPivot();
            var actualPosition = new Vector2(refPoint.x - (itemSize.x * pivot.x), refPoint.y - (itemSize.y * pivot.y));
            return new Rect(actualPosition, itemSize);
        }

        public override void ArrangeItems(ref TableView.CellGroup group, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.x += group.rect.size.x * 0.5f;
            refPoint.y = group.rect.size.y * 0.5f;

            for (var i = 0; i < group.items.Length; i++)
            {
                Vector2 itemSize = group.items[i].rect.size;
                refPoint.y -= itemSize.y * 0.5f;
                group.items[i].anchoredPosition = refPoint;
                refPoint.y -= (itemSize.y * 0.5f) + spacing.y;
            }
        }
    }

    internal sealed class HorizontalLeftTableViewWithVerticalBottomCalcImpl : HorizontalLeftTableViewCalcImpl
    {
        public override (Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) GetItemAnchorAndPivot()
        {
            return (
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0.5f, 0f)
            );
        }

        public override Rect GetItemRect(TableView.CellGroup group, int groupIdx, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.x += group.rect.size.x * 0.5f;
            refPoint.y = 0;
            Vector2 itemSize = Vector2.zero;

            for (var i = 0; i < group.itemSizes.Count; i++)
            {
                if (i == groupIdx)
                {
                    break;
                }

                itemSize = group.itemSizes[i];
                refPoint.y += itemSize.y + spacing.y;
            }

            (_, _, Vector2 pivot) = GetItemAnchorAndPivot();
            var actualPosition = new Vector2(refPoint.x - (itemSize.x * pivot.x), refPoint.y - (itemSize.y * pivot.y));
            return new Rect(actualPosition, itemSize);
        }

        public override void ArrangeItems(ref TableView.CellGroup group, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.x += group.rect.size.x * 0.5f;
            refPoint.y = 0;

            for (var i = 0; i < group.items.Length; i++)
            {
                group.items[i].anchoredPosition = refPoint;
                Vector2 itemSize = group.items[i].rect.size;
                refPoint.y += itemSize.y + spacing.y;
            }
        }
    }

    internal abstract class HorizontalRightTableViewCalcImpl : HorizontalTableViewCalcImpl
    {
        protected override void AddPosition(ref Vector2 position, float width, float spacing)
        {
            position.x -= width + spacing;
        }

        protected override void AddPositionToTargetPosition(ref Vector2 targetPos, float posDiff, Rect viewRectRect, Vector2 contentRectSize)
        {
            targetPos.x -= posDiff;
            targetPos.x = Mathf.Clamp(targetPos.x, 0f, contentRectSize.x - viewRectRect.width);
        }

        public override bool IsContainGroup(TableView.CellGroup group, Vector2 startPos, Vector2 endPos, Vector2 spacing)
        {
            float minPos = group.rect.min.x + spacing.x;
            float maxPos = group.rect.min.x - (group.rect.size.x + spacing.x);
            return (endPos.x < minPos && minPos < startPos.x) || (endPos.x < maxPos && maxPos < startPos.x);
        }

        public override void CalcContentStartEndPosition(ref Vector2 startPos, ref Vector2 endPos, Vector4 viewportMargin, Vector2 viewRectSize)
        {
            startPos.x = -startPos.x + viewportMargin.y;
            endPos.x = startPos.x - viewRectSize.x;
        }

        public override void ShiftPosition(ref TableView.CellGroup group, Vector2 shiftPos)
        {
            group.rect.position -= shiftPos;
            if (group.isShown)
            {
                foreach (RectTransform groupItem in group.items)
                {
                    groupItem.localPosition -= new Vector3(shiftPos.x, shiftPos.y);
                }
            }
        }

        public override Rect GetRangeGroupRect(List<TableView.CellGroup> groups, int startIdx, int endIdx)
        {
            Rect ret = groups[startIdx].rect;
            for (int i = startIdx + 1; i <= endIdx; i++)
            {
                Rect groupRect = groups[i].rect;
                ret.size = new Vector2(
                    ret.position.x - groupRect.position.x + groupRect.size.x,
                    Mathf.Max(ret.size.y, groupRect.size.y)
                );
            }

            return ret;
        }
    }

    internal sealed class HorizontalRightTableViewWithVerticalTopCalcImpl : HorizontalRightTableViewCalcImpl
    {
        public override (Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) GetItemAnchorAndPivot()
        {
            return (
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f)
            );
        }

        public override Rect GetItemRect(TableView.CellGroup group, int groupIdx, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.x -= group.rect.size.x * 0.5f;
            refPoint.y = 0;
            Vector2 itemSize = Vector2.zero;

            for (var i = 0; i < group.itemSizes.Count; i++)
            {
                if (i == groupIdx)
                {
                    break;
                }

                itemSize = group.itemSizes[i];
                refPoint.y -= itemSize.y + spacing.y;
            }

            (_, _, Vector2 pivot) = GetItemAnchorAndPivot();
            var actualPosition = new Vector2(refPoint.x - (itemSize.x * pivot.x), refPoint.y - (itemSize.y * pivot.y));
            return new Rect(actualPosition, itemSize);
        }

        public override void ArrangeItems(ref TableView.CellGroup group, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.x -= group.rect.size.x * 0.5f;
            refPoint.y = 0;

            for (var i = 0; i < group.items.Length; i++)
            {
                group.items[i].anchoredPosition = refPoint;
                Vector2 itemSize = group.items[i].rect.size;
                refPoint.y -= itemSize.y + spacing.y;
            }
        }
    }

    internal sealed class HorizontalRightTableViewWithVerticalCenterCalcImpl : HorizontalRightTableViewCalcImpl
    {
        public override (Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) GetItemAnchorAndPivot()
        {
            return (
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f)
            );
        }

        public override Rect GetItemRect(TableView.CellGroup group, int groupIdx, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.x -= group.rect.size.x * 0.5f;
            refPoint.y = group.rect.size.y * 0.5f;
            Vector2 itemSize = Vector2.zero;

            for (var i = 0; i < group.itemSizes.Count; i++)
            {
                itemSize = group.itemSizes[i];
                refPoint.y -= itemSize.y * 0.5f;
                if (i == groupIdx)
                {
                    break;
                }

                refPoint.y -= (itemSize.y * 0.5f) + spacing.y;
            }

            (_, _, Vector2 pivot) = GetItemAnchorAndPivot();
            var actualPosition = new Vector2(refPoint.x - (itemSize.x * pivot.x), refPoint.y - (itemSize.y * pivot.y));
            return new Rect(actualPosition, itemSize);
        }

        public override void ArrangeItems(ref TableView.CellGroup group, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.x -= group.rect.size.x * 0.5f;
            refPoint.y = group.rect.size.y * 0.5f;

            for (var i = 0; i < group.items.Length; i++)
            {
                Vector2 itemSize = group.items[i].rect.size;
                refPoint.y -= itemSize.y * 0.5f;
                group.items[i].anchoredPosition = refPoint;
                refPoint.y -= (itemSize.y * 0.5f) + spacing.y;
            }
        }
    }

    internal sealed class HorizontalRightTableViewWithVerticalBottomCalcImpl : HorizontalRightTableViewCalcImpl
    {
        public override (Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) GetItemAnchorAndPivot()
        {
            return (
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0f)
            );
        }

        public override Rect GetItemRect(TableView.CellGroup group, int groupIdx, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.x -= group.rect.size.x * 0.5f;
            refPoint.y = 0;
            Vector2 itemSize = Vector2.zero;

            for (var i = 0; i < group.itemSizes.Count; i++)
            {
                if (i == groupIdx)
                {
                    break;
                }

                itemSize = group.itemSizes[i];
                refPoint.y += itemSize.y + spacing.y;
            }

            (_, _, Vector2 pivot) = GetItemAnchorAndPivot();
            var actualPosition = new Vector2(refPoint.x - (itemSize.x * pivot.x), refPoint.y - (itemSize.y * pivot.y));
            return new Rect(actualPosition, itemSize);
        }

        public override void ArrangeItems(ref TableView.CellGroup group, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.x -= group.rect.size.x * 0.5f;
            refPoint.y = 0;

            for (var i = 0; i < group.items.Length; i++)
            {
                group.items[i].anchoredPosition = refPoint;
                Vector2 itemSize = group.items[i].rect.size;
                refPoint.y += itemSize.y + spacing.y;
            }
        }
    }
    #endregion

    #region Vertical
    internal abstract class VerticalTableViewCalcImpl : ITableViewCalcImpl
    {
        public Vector2 CalcContentSizeAndFillGroups(Vector2 viewRectSize, Vector2 spacing, List<TableView.CellGroup> groups, Vector2[] itemSizes)
        {
            // var contentSize = Vector2.zero;
            var contentSize = new Vector2(viewRectSize.x, 0f);
            Vector2 position = Vector2.zero;
            var currGroup = new TableView.CellGroup
            {
                rect = new Rect(position, itemSizes[0]),
                itemSizes = new List<Vector2> {itemSizes[0]},
                startIdx = 0,
                endIdx = 0,
            };

            float maxWidth = currGroup.rect.width;
            for (var i = 1; i < itemSizes.Length; i++)
            {
                Vector2 itemSize = itemSizes[i];
                float postSize = currGroup.rect.size.x + itemSize.x + spacing.x;
                float maxSize = viewRectSize.x;
                if (postSize > maxSize)
                {
                    AddPosition(ref position, currGroup.rect.height, spacing.y);
                    groups.Add(currGroup);
                    // contentSize.x = Mathf.Max(currGroup.rect.size.x, contentSize.x);
                    contentSize.y = currGroup.rect.size.y + spacing.y + contentSize.y;
                    maxWidth = Mathf.Max(itemSize.x, maxWidth);
                    currGroup = new TableView.CellGroup
                    {
                        rect = new Rect(position, itemSize),
                        itemSizes = new List<Vector2> {itemSize},
                        startIdx = i,
                        endIdx = i,
                    };
                }
                else
                {
                    Rect rect = currGroup.rect;
                    rect.width += itemSize.x + spacing.x;
                    maxWidth = Mathf.Max(rect.width, maxWidth);
                    rect.height = Mathf.Max(rect.height, itemSize.y);
                    currGroup.itemSizes.Add(itemSize);
                    currGroup.rect = rect;
                    currGroup.endIdx = i;
                }
            }

            groups.Add(currGroup);
            for (var i = 0; i < groups.Count; i++)
            {
                TableView.CellGroup group = groups[i];
                group.rect.width = maxWidth;
                groups[i] = group;
            }

            // contentSize.x = Mathf.Max(currGroup.rect.size.x, contentSize.x);
            contentSize.y = currGroup.rect.size.y + contentSize.y;
            return contentSize;
        }

        protected abstract void AddPosition(ref Vector2 position, float height, float spacing);

        public Vector2 GetFocusTargetPosition(Rect viewRectRect, Vector2 contentRectSize, float posRate, TableView.CellGroup group)
        {
            Vector2 targetPos = Vector2.zero;
            if (viewRectRect.height > contentRectSize.y)
            {
                return targetPos;
            }

            targetPos.y = -group.rect.min.y;
            float posDiff = posRate * (viewRectRect.height - group.rect.size.y);
            AddPositionToTargetPosition(ref targetPos, posDiff, viewRectRect, contentRectSize);
            return targetPos;
        }

        protected abstract void AddPositionToTargetPosition(ref Vector2 targetPos, float posDiff, Rect viewRectRect, Vector2 contentRectSize);

        public abstract bool IsContainGroup(TableView.CellGroup group, Vector2 startPos, Vector2 endPos, Vector2 spacing);
        public abstract void CalcContentStartEndPosition(ref Vector2 startPos, ref Vector2 endPos, Vector4 viewportMargin, Vector2 viewRectSize);
        public abstract (Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) GetItemAnchorAndPivot();
        public abstract Rect GetItemRect(TableView.CellGroup group, int groupIdx, Vector2 spacing);
        public abstract void ArrangeItems(ref TableView.CellGroup group, Vector2 spacing);

        public (bool, Vector2) ReCalcGroupSize(ref TableView.CellGroup group, int itemIdx, Vector2 itemSize)
        {
            Vector2 size = group.rect.size;
            int groupIdx = itemIdx - group.startIdx;
            group.itemSizes[groupIdx] = itemSize;
            var biggestSize = 0f;
            for (var i = 0; i < group.itemSizes.Count; i++)
            {
                if (group.itemSizes[i].y > biggestSize)
                {
                    biggestSize = group.itemSizes[i].y;
                }
            }

            if (Mathf.Approximately(size.y, biggestSize))
            {
                return (false, Vector2.zero);
            }

            float prevSize = size.y;
            size.y = biggestSize;
            group.rect.size = size;
            return (true, new Vector2(0f, biggestSize - prevSize));
        }

        public abstract void ShiftPosition(ref TableView.CellGroup group, Vector2 shiftPos);
        public abstract Rect GetRangeGroupRect(List<TableView.CellGroup> groups, int startIdx, int endIdx);
    }

    internal abstract class VerticalTopTableViewCalcImpl : VerticalTableViewCalcImpl
    {
        protected override void AddPosition(ref Vector2 position, float height, float spacing)
        {
            position.y -= height + spacing;
        }

        protected override void AddPositionToTargetPosition(ref Vector2 targetPos, float posDiff, Rect viewRectRect, Vector2 contentRectSize)
        {
            targetPos.y -= posDiff;
            targetPos.y = Mathf.Clamp(targetPos.y, 0f, contentRectSize.y - viewRectRect.height);
        }

        public override bool IsContainGroup(TableView.CellGroup group, Vector2 startPos, Vector2 endPos, Vector2 spacing)
        {
            float minPos = group.rect.min.y + spacing.y;
            float maxPos = group.rect.min.y - (group.rect.size.y + spacing.y);
            return (endPos.y < minPos && minPos < startPos.y) || (endPos.y < maxPos && maxPos < startPos.y);
        }

        public override void CalcContentStartEndPosition(ref Vector2 startPos, ref Vector2 endPos, Vector4 viewportMargin, Vector2 viewRectSize)
        {
            startPos.y = -startPos.y + viewportMargin.z;
            endPos.y = startPos.y - viewRectSize.y;
        }

        public override void ShiftPosition(ref TableView.CellGroup group, Vector2 shiftPos)
        {
            group.rect.position -= shiftPos;
            if (group.isShown)
            {
                foreach (RectTransform groupItem in group.items)
                {
                    groupItem.localPosition -= new Vector3(shiftPos.x, shiftPos.y);
                }
            }
        }

        public override Rect GetRangeGroupRect(List<TableView.CellGroup> groups, int startIdx, int endIdx)
        {
            Rect ret = groups[startIdx].rect;
            for (int i = startIdx + 1; i <= endIdx; i++)
            {
                Rect groupRect = groups[i].rect;
                ret.size = new Vector2(
                    Mathf.Max(ret.size.x, groupRect.size.x),
                    ret.position.y - groupRect.position.y + groupRect.size.y
                );
            }

            return ret;
        }
    }

    internal sealed class VerticalTopTableViewWithHorizontalLeftCalcImpl : VerticalTopTableViewCalcImpl
    {
        public override (Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) GetItemAnchorAndPivot()
        {
            return (
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 0.5f)
            );
        }

        public override Rect GetItemRect(TableView.CellGroup group, int groupIdx, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.y -= group.rect.size.y * 0.5f;
            refPoint.x = 0f;
            Vector2 itemSize = Vector2.zero;

            for (var i = 0; i < group.itemSizes.Count; i++)
            {
                if (i == groupIdx)
                {
                    break;
                }

                itemSize = group.itemSizes[i];
                refPoint.x += itemSize.x + spacing.x;
            }

            (_, _, Vector2 pivot) = GetItemAnchorAndPivot();
            var actualPosition = new Vector2(refPoint.x - (itemSize.x * pivot.x), refPoint.y - (itemSize.y * pivot.y));
            return new Rect(actualPosition, itemSize);
        }

        public override void ArrangeItems(ref TableView.CellGroup group, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.y -= group.rect.size.y * 0.5f;
            refPoint.x = 0f;

            for (var i = 0; i < group.items.Length; i++)
            {
                group.items[i].anchoredPosition = refPoint;
                Vector2 itemSize = group.items[i].rect.size;
                refPoint.x += itemSize.x + spacing.x;
            }
        }
    }

    internal sealed class VerticalTopTableViewWithHorizontalCenterCalcImpl : VerticalTopTableViewCalcImpl
    {
        public override (Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) GetItemAnchorAndPivot()
        {
            return (
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 0.5f)
            );
        }

        public override Rect GetItemRect(TableView.CellGroup group, int groupIdx, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.y -= group.rect.size.y * 0.5f;
            refPoint.x = -group.rect.size.x * 0.5f;
            Vector2 itemSize = Vector2.zero;

            for (var i = 0; i < group.itemSizes.Count; i++)
            {
                itemSize = group.itemSizes[i];
                refPoint.x += itemSize.x * 0.5f;
                if (i == groupIdx)
                {
                    break;
                }

                refPoint.x += (itemSize.x * 0.5f) + spacing.x;
            }

            (_, _, Vector2 pivot) = GetItemAnchorAndPivot();
            var actualPosition = new Vector2(refPoint.x - (itemSize.x * pivot.x), refPoint.y - (itemSize.y * pivot.y));
            return new Rect(actualPosition, itemSize);
        }

        public override void ArrangeItems(ref TableView.CellGroup group, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.y -= group.rect.size.y * 0.5f;
            refPoint.x = -group.rect.size.x * 0.5f;

            for (var i = 0; i < group.items.Length; i++)
            {
                Vector2 itemSize = group.items[i].rect.size;
                refPoint.x += itemSize.x * 0.5f;
                group.items[i].anchoredPosition = refPoint;
                refPoint.x += (itemSize.x * 0.5f) + spacing.x;
            }
        }
    }

    internal sealed class VerticalTopTableViewWithHorizontalRightCalcImpl : VerticalTopTableViewCalcImpl
    {
        public override (Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) GetItemAnchorAndPivot()
        {
            return (
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0.5f)
            );
        }

        public override Rect GetItemRect(TableView.CellGroup group, int groupIdx, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.y -= group.rect.size.y * 0.5f;
            refPoint.x = 0;
            Vector2 itemSize = Vector2.zero;

            for (var i = 0; i < group.itemSizes.Count; i++)
            {
                if (i == groupIdx)
                {
                    break;
                }

                itemSize = group.itemSizes[i];
                refPoint.x -= itemSize.x + spacing.x;
            }

            (_, _, Vector2 pivot) = GetItemAnchorAndPivot();
            var actualPosition = new Vector2(refPoint.x - (itemSize.x * pivot.x), refPoint.y - (itemSize.y * pivot.y));
            return new Rect(actualPosition, itemSize);
        }

        public override void ArrangeItems(ref TableView.CellGroup group, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.y -= group.rect.size.y * 0.5f;
            refPoint.x = 0;

            for (var i = 0; i < group.items.Length; i++)
            {
                group.items[i].anchoredPosition = refPoint;
                Vector2 itemSize = group.items[i].rect.size;
                refPoint.x -= itemSize.x + spacing.x;
            }
        }
    }

    internal abstract class VerticalBottomTableViewCalcImpl : VerticalTableViewCalcImpl
    {
        protected override void AddPosition(ref Vector2 position, float height, float spacing)
        {
            position.y += height + spacing;
        }

        protected override void AddPositionToTargetPosition(ref Vector2 targetPos, float posDiff, Rect viewRectRect, Vector2 contentRectSize)
        {
            targetPos.y += posDiff;
            targetPos.y = Mathf.Clamp(targetPos.y, viewRectRect.height - contentRectSize.y, 0f);
        }

        public override bool IsContainGroup(TableView.CellGroup group, Vector2 startPos, Vector2 endPos, Vector2 spacing)
        {
            float minPos = group.rect.min.y - spacing.y;
            float maxPos = group.rect.min.y + (group.rect.size.y + spacing.y);
            return (startPos.y < minPos && minPos < endPos.y) || (startPos.y < maxPos && maxPos < endPos.y);
        }

        public override void CalcContentStartEndPosition(ref Vector2 startPos, ref Vector2 endPos, Vector4 viewportMargin, Vector2 viewRectSize)
        {
            startPos.y = -startPos.y - viewportMargin.w;
            endPos.y = startPos.y + viewRectSize.y;
        }

        public override void ShiftPosition(ref TableView.CellGroup group, Vector2 shiftPos)
        {
            group.rect.position += shiftPos;
            if (group.isShown)
            {
                foreach (RectTransform groupItem in group.items)
                {
                    groupItem.localPosition += new Vector3(shiftPos.x, shiftPos.y);
                }
            }
        }

        public override Rect GetRangeGroupRect(List<TableView.CellGroup> groups, int startIdx, int endIdx)
        {
            Rect ret = groups[startIdx].rect;
            for (int i = startIdx + 1; i <= endIdx; i++)
            {
                Rect groupRect = groups[i].rect;
                ret.size = new Vector2(
                    Mathf.Max(ret.size.x, groupRect.size.x),
                    groupRect.position.y - ret.position.y + groupRect.size.y
                );
            }

            return ret;
        }
    }

    internal sealed class VerticalBottomTableViewWithHorizontalLeftCalcImpl : VerticalBottomTableViewCalcImpl
    {
        public override (Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) GetItemAnchorAndPivot()
        {
            return (
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0.5f)
            );
        }

        public override Rect GetItemRect(TableView.CellGroup group, int groupIdx, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.y += group.rect.size.y * 0.5f;
            refPoint.x = 0f;
            Vector2 itemSize = Vector2.zero;

            for (var i = 0; i < group.itemSizes.Count; i++)
            {
                if (i == groupIdx)
                {
                    break;
                }

                itemSize = group.itemSizes[i];
                refPoint.x += itemSize.x + spacing.x;
            }

            (_, _, Vector2 pivot) = GetItemAnchorAndPivot();
            var actualPosition = new Vector2(refPoint.x - (itemSize.x * pivot.x), refPoint.y - (itemSize.y * pivot.y));
            return new Rect(actualPosition, itemSize);
        }

        public override void ArrangeItems(ref TableView.CellGroup group, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.y += group.rect.size.y * 0.5f;
            refPoint.x = 0f;

            for (var i = 0; i < group.items.Length; i++)
            {
                group.items[i].anchoredPosition = refPoint;
                Vector2 itemSize = group.items[i].rect.size;
                refPoint.x += itemSize.x + spacing.x;
            }
        }
    }

    internal sealed class VerticalBottomTableViewWithHorizontalCenterCalcImpl : VerticalBottomTableViewCalcImpl
    {
        public override (Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) GetItemAnchorAndPivot()
        {
            return (
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0.5f)
            );
        }

        public override Rect GetItemRect(TableView.CellGroup group, int groupIdx, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.y += group.rect.size.y * 0.5f;
            refPoint.x = group.rect.size.x * 0.5f;
            Vector2 itemSize = Vector2.zero;

            for (var i = 0; i < group.itemSizes.Count; i++)
            {
                itemSize = group.itemSizes[i];
                refPoint.x -= itemSize.x * 0.5f;
                if (i == groupIdx)
                {
                    break;
                }

                refPoint.x -= (itemSize.x * 0.5f) + spacing.x;
            }

            (_, _, Vector2 pivot) = GetItemAnchorAndPivot();
            var actualPosition = new Vector2(refPoint.x - (itemSize.x * pivot.x), refPoint.y - (itemSize.y * pivot.y));
            return new Rect(actualPosition, itemSize);
        }

        public override void ArrangeItems(ref TableView.CellGroup group, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.y += group.rect.size.y * 0.5f;
            refPoint.x = group.rect.size.x * 0.5f;

            for (var i = 0; i < group.items.Length; i++)
            {
                Vector2 itemSize = group.items[i].rect.size;
                refPoint.x -= itemSize.x * 0.5f;
                group.items[i].anchoredPosition = refPoint;
                refPoint.x -= (itemSize.x * 0.5f) + spacing.x;
            }
        }
    }

    internal sealed class VerticalBottomTableViewWithHorizontalRightCalcImpl : VerticalBottomTableViewCalcImpl
    {
        public override (Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) GetItemAnchorAndPivot()
        {
            return (
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0.5f)
            );
        }

        public override Rect GetItemRect(TableView.CellGroup group, int groupIdx, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.y += group.rect.size.y * 0.5f;
            refPoint.x = 0;
            Vector2 itemSize = Vector2.zero;

            for (var i = 0; i < group.itemSizes.Count; i++)
            {
                if (i == groupIdx)
                {
                    break;
                }

                itemSize = group.itemSizes[i];
                refPoint.x -= itemSize.x + spacing.x;
            }

            (_, _, Vector2 pivot) = GetItemAnchorAndPivot();
            var actualPosition = new Vector2(refPoint.x - (itemSize.x * pivot.x), refPoint.y - (itemSize.y * pivot.y));
            return new Rect(actualPosition, itemSize);
        }

        public override void ArrangeItems(ref TableView.CellGroup group, Vector2 spacing)
        {
            Vector2 refPoint = group.rect.min;
            refPoint.y += group.rect.size.y * 0.5f;
            refPoint.x = 0;

            for (var i = 0; i < group.items.Length; i++)
            {
                group.items[i].anchoredPosition = refPoint;
                Vector2 itemSize = group.items[i].rect.size;
                refPoint.x -= itemSize.x + spacing.x;
            }
        }
    }
    #endregion

    internal interface ITableViewCalcImpl
    {
        Vector2 CalcContentSizeAndFillGroups(Vector2 viewRectSize, Vector2 spacing, List<TableView.CellGroup> groups, Vector2[] itemSizes);
        Vector2 GetFocusTargetPosition(Rect viewRectRect, Vector2 rectSize, float posRate, TableView.CellGroup group);
        bool IsContainGroup(TableView.CellGroup group, Vector2 startPos, Vector2 endPos, Vector2 spacing);
        void CalcContentStartEndPosition(ref Vector2 startPos, ref Vector2 endPos, Vector4 viewportMargin, Vector2 viewRectSize);
        (Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot) GetItemAnchorAndPivot();
        Rect GetItemRect(TableView.CellGroup group, int groupIdx, Vector2 spacing);
        void ArrangeItems(ref TableView.CellGroup group, Vector2 spacing);
        (bool, Vector2) ReCalcGroupSize(ref TableView.CellGroup group, int itemIdx, Vector2 itemSize);
        void ShiftPosition(ref TableView.CellGroup group, Vector2 shiftPos);
        Rect GetRangeGroupRect(List<TableView.CellGroup> groups, int startIdx, int endIdx);
    }

    internal static class TableViewCalcImplFactory
    {
        internal static ITableViewCalcImpl Create(bool vertical, bool horizontal, TableView.ItemHorizontalAlign horizontalAlign, TableView.ItemVerticalAlign verticalAlign)
        {
            if (vertical && verticalAlign == TableView.ItemVerticalAlign.Top)
            {
                return horizontalAlign switch
                {
                    TableView.ItemHorizontalAlign.Left => new VerticalTopTableViewWithHorizontalLeftCalcImpl(),
                    TableView.ItemHorizontalAlign.Center => new VerticalTopTableViewWithHorizontalCenterCalcImpl(),
                    TableView.ItemHorizontalAlign.Right => new VerticalTopTableViewWithHorizontalRightCalcImpl(),
                    _ => throw new ArgumentException(),
                };
            }

            if (vertical && verticalAlign == TableView.ItemVerticalAlign.Bottom)
            {
                return horizontalAlign switch
                {
                    TableView.ItemHorizontalAlign.Left => new VerticalBottomTableViewWithHorizontalLeftCalcImpl(),
                    TableView.ItemHorizontalAlign.Center => new VerticalBottomTableViewWithHorizontalCenterCalcImpl(),
                    TableView.ItemHorizontalAlign.Right => new VerticalBottomTableViewWithHorizontalRightCalcImpl(),
                    _ => throw new ArgumentException(),
                };
            }

            if (horizontal && horizontalAlign == TableView.ItemHorizontalAlign.Left)
            {
                return verticalAlign switch
                {
                    TableView.ItemVerticalAlign.Top => new HorizontalLeftTableViewWithVerticalTopCalcImpl(),
                    TableView.ItemVerticalAlign.Center => new HorizontalLeftTableViewWithVerticalCenterCalcImpl(),
                    TableView.ItemVerticalAlign.Bottom => new HorizontalLeftTableViewWithVerticalBottomCalcImpl(),
                    _ => throw new ArgumentException(),
                };
            }

            if (horizontal && horizontalAlign == TableView.ItemHorizontalAlign.Right)
            {
                return verticalAlign switch
                {
                    TableView.ItemVerticalAlign.Top => new HorizontalRightTableViewWithVerticalTopCalcImpl(),
                    TableView.ItemVerticalAlign.Center => new HorizontalRightTableViewWithVerticalCenterCalcImpl(),
                    TableView.ItemVerticalAlign.Bottom => new HorizontalRightTableViewWithVerticalBottomCalcImpl(),
                    _ => throw new ArgumentException(),
                };
            }

            throw new ArgumentException();
        }
    }
}
