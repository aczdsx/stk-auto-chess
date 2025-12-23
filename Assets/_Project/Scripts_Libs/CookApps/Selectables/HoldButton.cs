using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace CookApps.TeamBattle.UIManagements
{
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static float AccelerationFactor = 3.0f;
        public static float BaseActionInterval = 1/5f;
        private bool isBeingHeld = false;
        private float heldTime = 0f;
        private float lastActionTime = 0f;
        [SerializeField] private bool isBlockDrag = false;
        [Header("인자는 실행 배수 입니다.")]
        [SerializeField] private UnityEvent<int> onExecute;

        [SerializeField] private DefaultClickSoundType defaultClickSoundType;
        public static event Action<DefaultClickSoundType> OnPlayDefaultClickSound;

        private void OnDisable()
        {
            isBeingHeld = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isBeingHeld = true;
            heldTime = 0f;

            Execute(1);
            lastActionTime = Time.time;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isBeingHeld = false;
        }

        private void Update()
        {
            if (!isBeingHeld)
                return;

            heldTime += Time.deltaTime;

            var currentActionInterval = BaseActionInterval / (1 + AccelerationFactor * heldTime);

            var deltaTime = Time.time - lastActionTime;
            if (deltaTime > currentActionInterval)
            {
                var count = 0;
                while (deltaTime > currentActionInterval)
                {
                    count++;
                    deltaTime -= currentActionInterval;
                }
                Execute(count);
                lastActionTime = Time.time;
            }
        }

        private void Execute(int count)
        {
            if (!SelectableBlockerManager.Instance.IsAllowSelectable(name))
                return;

            SelectableBlockerManager.Instance.OnClicked(gameObject.name);
            onExecute?.Invoke(count);
            OnPlayDefaultClickSound?.Invoke(defaultClickSoundType);
        }

        private void DoForParents<T>(Action<T> action) where T : IEventSystemHandler
        {
            Transform parent = transform.parent;
            while (parent != null)
            {
                foreach (var component in parent.GetComponents<Component>())
                {
                    if (component is T)
                        action((T)(IEventSystemHandler)component);
                }

                parent = parent.parent;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isBlockDrag)
                return;

            DoForParents<IBeginDragHandler>((parent) => { parent.OnBeginDrag(eventData); });
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isBlockDrag)
                return;

            DoForParents<IDragHandler>((parent) => { parent.OnDrag(eventData); });
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isBlockDrag)
                return;

            DoForParents<IEndDragHandler>((parent) => { parent.OnEndDrag(eventData); });
        }
    }
}
