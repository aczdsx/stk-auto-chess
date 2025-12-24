using System;
using System.Collections.Generic;
using CookApps.TeamBattle.Utility;
using CookApps.CypherPrefs;
using R3;
using UnityEngine.Pool;

namespace CookApps.TeamBattle
{
    internal class BadgePathPref : Preference<int, List<string>>
    {
        public BadgePathPref() : base(PreferenceGetterSetter.Default)
        {
        }

        public override string PreferenceKey => "RD_BadgeSystem_State";
        
        public void AddBadgePath(BadgeType type, string path)
        {
            var list = GetData((int)type) ?? new List<string>();
            
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == path)
                    return;
            }
            list.Add(path);
            SetData((int)type, list);
        }
        
        public void RemoveBadgePath(BadgeType type, string path)
        {
            var list = GetData((int)type);
            if (list == null)
                return;

            bool isRemoved = false;
            for (var i = 0; i < list.Count;)
            {
                if (list[i].StartsWith(path))
                {
                    list.RemoveAt(i);
                    isRemoved = true;
                }
                else
                {
                    i++;
                }
            }
            
            if (isRemoved)
                SetData((int)type, list);
        }
    }
    
    internal class BadgeNode
    {
        public BadgeNode(ulong key, BadgeNode parent, bool withSave)
        {
            this.key = key;
            this.parent = parent;
            this.withSave = withSave;
        }

        public ulong key;
        public BadgeNode parent;
        public bool withSave;
        public string path;
        public Dictionary<ulong, BadgeNode> children = new Dictionary<ulong, BadgeNode>();
    }

    public enum BadgeType
    {
        None,
        RedDot,
        AdDot,
        NewDot,
        Max
    }

    public class BadgeManager : Singleton<BadgeManager>
    {
        private Dictionary<BadgeType, BadgeNode> rootBadgeNodes = new ();
        private List<Badge> managedBadges = new ();
        private Subject<Unit> refreshDelegate = new ();
        private IDisposable disposable;

        private BadgePathPref pref;
        
        public void Initialize()
        {
            pref = new BadgePathPref();
            pref.Load();
            ClearRootBadgeNodes();
            LoadFromPref();
            disposable = refreshDelegate.ThrottleLastFrame(0).Subscribe(this, (_, self) => self.UpdateAllBadges());
        }

        public void Clear()
        {
            managedBadges.Clear();
            pref.Delete();
            pref.Save();
            ClearRootBadgeNodes();
            disposable?.Dispose();
        }

        private void ClearRootBadgeNodes()
        {
            rootBadgeNodes.Clear();
            for (var type = BadgeType.None + 1; type < BadgeType.Max; type++)
            {
                rootBadgeNodes.Add(type, new BadgeNode(0, null, false));
            }
        }

        public void AddToManagedBadgeList(Badge badge)
        {
            if (!managedBadges.Contains(badge))
                managedBadges.Add(badge);
        }

        public void RemoveFromManagedBadgeList(Badge badge)
        {
            managedBadges.Remove(badge);
        }

        public void AddBadge(BadgeType type, string path, bool withSave = false)
        {
            if (type == BadgeType.None)
                return;

            var count = 0;
            for (var i = 0; i < path.Length; i++)
            {
                if (path[i] == '/')
                    count++;
            }

            
            Span<ulong> arr = stackalloc ulong[count + 1];
            int index = 0;
            int arrIndex = 0;
            
            for (var i = 0; i < path.Length; i++)
            {
                if (path[i] != '/')
                    continue;

                if (index == i)
                    return;

                var span = path.AsSpan(index, i - index);
                arr[arrIndex] = span.djb2Hash();
                arrIndex++;
                index = i + 1;
            }

            {
                var span = path.AsSpan(index, path.Length - index);
                arr[arrIndex] = span.djb2Hash();
            }

            var badgeNode = rootBadgeNodes[type];
            for (int i = 0; i < arr.Length; i++)
            {
                if (!badgeNode.children.ContainsKey(arr[i]))
                {
                    badgeNode.children.Add(arr[i], new BadgeNode(arr[i], badgeNode, false));
                }
                badgeNode = badgeNode.children[arr[i]];
            }
            badgeNode.withSave = withSave;
            badgeNode.path = path;

            if (withSave)
            {
                pref.AddBadgePath(type, path);
                pref.Save();
            }
            refreshDelegate.OnNext(Unit.Default);
        }

        public void RemoveBadge(BadgeType type, string path)
        {
            if (type == BadgeType.None)
                return;

            var count = 0;
            for (var i = 0; i < path.Length; i++)
            {
                if (path[i] == '/')
                    count++;
            }

            Span<ulong> arr = stackalloc ulong[count + 1];
            int index = 0;
            int arrIndex = 0;
            
            for (var i = 0; i < path.Length; i++)
            {
                if (path[i] != '/')
                    continue;

                if (index == i)
                    return;

                var span = path.AsSpan(index, i - index);
                arr[arrIndex] = span.djb2Hash();
                arrIndex++;
                index = i + 1;
            }
            
            {
                var span = path.AsSpan(index, path.Length - index);
                arr[arrIndex] = span.djb2Hash();
            }

            var badgeNode = rootBadgeNodes[type];
            for (int i = 0; i < arr.Length; i++)
            {
                if (!badgeNode.children.ContainsKey(arr[i]))
                    return;
                badgeNode = badgeNode.children[arr[i]];
            }

            var hasWithSave = CheckHasWithSave(badgeNode);
            badgeNode.children.Clear();

            while (badgeNode.children.Count == 0)
            {
                if (badgeNode.parent == null)
                    break;
                var key = badgeNode.key;
                badgeNode = badgeNode.parent;
                badgeNode.children.Remove(key);
            }

            if (hasWithSave)
            {
                pref.RemoveBadgePath(type, path);
                pref.Save();
            }
            refreshDelegate.OnNext(Unit.Default);
        }
        
        private bool CheckHasWithSave(BadgeNode node)
        {
            if (node.withSave)
                return true;
            foreach (var child in node.children)
            {
                if (CheckHasWithSave(child.Value))
                    return true;
            }
            return false;
        }

        public bool HasBadge(BadgeType type, string path)
        {
            var count = 0;
            for (var i = 0; i < path.Length; i++)
            {
                if (path[i] == '/')
                    count++;
            }

            Span<ulong> arr = stackalloc ulong[count + 1];
            int index = 0;
            int arrIndex = 0;
            
            for (var i = 0; i < path.Length; i++)
            {
                if (path[i] != '/')
                    continue;

                if (index == i)
                    return false;

                var span = path.AsSpan(index, i - index);
                arr[arrIndex] = span.djb2Hash();
                arrIndex++;
                index = i + 1;
            }

            {
                var span = path.AsSpan(index, path.Length - index);
                arr[arrIndex] = span.djb2Hash();
            }

            var badgeNode = rootBadgeNodes[type];
            for (int i = 0; i < arr.Length; i++)
            {
                if (!badgeNode.children.ContainsKey(arr[i]))
                    return false;
                badgeNode = badgeNode.children[arr[i]];
            }
            return true;
        }

        public int GetBadgeCount(BadgeType type, string path)
        {
            var count = 0;
            for (var i = 0; i < path.Length; i++)
            {
                if (path[i] == '/')
                    count++;
            }

            Span<ulong> arr = stackalloc ulong[count + 1];
            int index = 0;
            int arrIndex = 0;
            
            for (var i = 0; i < path.Length; i++)
            {
                if (path[i] != '/')
                    continue;

                if (index == i)
                    return 0;

                var span = path.AsSpan(index, i - index);
                arr[arrIndex] = span.djb2Hash();
                arrIndex++;
                index = i + 1;
            }

            {
                var span = path.AsSpan(index, path.Length - index);
                arr[arrIndex] = span.djb2Hash();
            }

            var badgeNode = rootBadgeNodes[type];
            for (int i = 0; i < arr.Length; i++)
            {
                if (!badgeNode.children.ContainsKey(arr[i]))
                    return 0;
                badgeNode = badgeNode.children[arr[i]];
            }

            using var _ = ListPool<BadgeNode>.Get(out var resList);
            GetLastNodes(badgeNode, ref resList);
            return resList.Count;
        }

        private void UpdateAllBadges()
        {
            for (int i = managedBadges.Count; --i >=0; )
            {
                if (managedBadges[i] != null)
                {
                    managedBadges[i].UpdateBadge();
                }else
                {
                    managedBadges.RemoveAt(i);
                }  
            }
        }

        #region Save & Load
        private void LoadFromPref()
        {
            foreach (var pair in rootBadgeNodes)
            {
                pair.Value.children.Clear();
            }

            for (int i = (int)BadgeType.None + 1; i < (int)BadgeType.Max; i++)
            {
                var paths = pref.GetData(i);
                if (paths == null)
                    continue;

                foreach (var path in paths)
                {
                    AddBadge((BadgeType)i, path, true);
                }
            }
        }
        #endregion

        private void GetLastNodes(BadgeNode parent, ref List<BadgeNode> resList)
        {
            if (parent.children.Count == 0)
            {
                resList.Add(parent);
                return;
            }

            foreach(var child in parent.children)
            {
                GetLastNodes(child.Value, ref resList);
            }
        }
    }
}
