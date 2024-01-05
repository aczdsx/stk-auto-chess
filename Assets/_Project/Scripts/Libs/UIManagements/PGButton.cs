using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CookApps.TeamBattle.UIManagements
{

    public enum DefaultClickSoundType
    {
        None = -1,
        Basic,
        Custom_0,
        Custom_1,
        Custom_2,
    }

    /// <summary>
    /// 유니티가 제공하는 버튼을 상속받은 버튼 클래스
    /// </summary>
    public class PGButton : Button
    {
        [SerializeField] private bool isBlockDrag = false;
        [SerializeField] private bool useDefaultClickSound = true;
        [SerializeField] private DefaultClickSoundType defaultClickSoundType;
        public static event Action<DefaultClickSoundType> OnPlayDefaultClickSound;

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (!SelectableBlockerManager.Instance.IsAllowSelectable(name))
                return;

            SelectableBlockerManager.Instance.OnClicked(gameObject.name);
            if (useDefaultClickSound)
                OnPlayDefaultClickSound?.Invoke(defaultClickSoundType);
            base.OnPointerClick(eventData);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            if (!SelectableBlockerManager.Instance.IsAllowSelectable(name))
                return;

            SelectableBlockerManager.Instance.OnClicked(gameObject.name);
            if (useDefaultClickSound)
                OnPlayDefaultClickSound?.Invoke(defaultClickSoundType);
            base.OnSubmit(eventData);
        }
    }
}
