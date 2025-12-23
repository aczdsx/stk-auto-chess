using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.TeamBattle
{
    public class Badge : CachedMonoBehaviour
    {
        [Serializable]
        public class BadgeInfo
        {
            public BadgeType type;
            // public bool needShowCount;
            public string path;
        }

        [Serializable]
        private class BadgeSpriteInfo
        {
            public BadgeType type;
            public Sprite sprite;
        }

        [SerializeField] private List<BadgeSpriteInfo> sprites;
        [SerializeField] private Image image;
        // [SerializeField] private TextMeshProUGUI text;

        [SerializeField] private BadgeType defaultType = BadgeType.None;
        [SerializeField] private List<BadgeInfo> paths = new ();
        private bool hasBadge = false;
        private BadgeType currentType = BadgeType.None;
        // private bool currentNeedShowCount = false;
        // private int currentBadgeCount = 0;
        private string currentPath = string.Empty;
        private bool isLoading = false;

        protected void Awake()
        {
            Init();
        }

        public void Init()
        {
            UpdateBadge();
            BadgeManager.Instance.AddToManagedBadgeList(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (BadgeManager.Instance != null)
            {
                BadgeManager.Instance.RemoveFromManagedBadgeList(this);
            }
        }

        public void Clear()
        {
            defaultType = BadgeType.None;
            paths.Clear();
            UpdateBadge();
        }

        public void AddBadgePath(BadgeType type, string path)
        {
            paths.Add(new () { type = type, path = path });
            UpdateBadge();
        }

        public void UpdateBadge()
        {
            if (!this)
                return;
            
            if (!gameObject)
                return;
            
            if (paths.Count == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            hasBadge = false;
            var targetType = BadgeType.None;
            var targetPath = string.Empty;
            // var targetNeedShowCount = false;
            // var badgeCount = 0;
            for (int i = 0; i < paths.Count; i++)
            {
                var type = paths[i].type == BadgeType.None ? defaultType : paths[i].type;
                if (type == BadgeType.None)
                    continue;

                if (BadgeManager.Instance.HasBadge(type, paths[i].path))
                {
                    targetType = paths[i].type == BadgeType.None ? defaultType : paths[i].type;
                    // targetNeedShowCount = paths[i].needShowCount;
                    // if (targetNeedShowCount)
                    //     badgeCount = BadgeManager.Instance.GetBadgeCount(type, paths[i].path);
                    targetPath = paths[i].path;
                    hasBadge = true;
                    break;
                }
            }

            gameObject.SetActive(hasBadge);

            if (!hasBadge)
                return;

            if ((currentType, /*currentNeedShowCount, currentBadgeCount,*/ currentPath) == (targetType, /*targetNeedShowCount, badgeCount,*/ targetPath))
                return;

            (currentType, /*currentNeedShowCount, currentBadgeCount,*/ currentPath) = (targetType, /*targetNeedShowCount, badgeCount,*/ targetPath);
            SetBadge();
        }

        private void SetBadge()
        {
            if (currentType == BadgeType.None)
                return;

            foreach (var sprite in sprites)
            {
                if (sprite.type != currentType)
                    continue;
                    
                image.sprite = sprite.sprite;
            }
            
            // image.SetNativeSize();
            // text.gameObject.SetActive(currentNeedShowCount);
            // if (currentNeedShowCount)
            // {
            //     text.text = BadgeManager.Instance.GetBadgeCount(currentType, currentPath).ToString();
            // }
        }
    }
}
