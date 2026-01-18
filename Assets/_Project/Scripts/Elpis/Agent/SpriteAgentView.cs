using UnityEngine;

namespace Elpis.Agent
{
    public enum Direction
    {
        Front,
        Back,
        Left,
        Right
    }

    public class SpriteAgentView : MonoBehaviour
    {
        [SerializeField]
        private Animator animator;
        [SerializeField]
        private SpriteRenderer[] spriteRendererList;

        private static readonly int BackIdle = Animator.StringToHash("Back_IDLE");
        private static readonly int FrontIdle = Animator.StringToHash("Front_IDLE");
        private static readonly int BackMove = Animator.StringToHash("Back_MOVE");
        private static readonly int FrontMove = Animator.StringToHash("Front_MOVE");

        private bool CachedFlipX { get; set; }
        private bool CachedFront { get; set; }
        private int CachedAnimationHash { get; set; }
        
        private void Awake()
        {
            if (animator != null)
            {
                if (spriteRendererList == null || spriteRendererList.Length == 0)
                {
                    spriteRendererList = animator.transform.GetComponents<SpriteRenderer>();
                }
            }
        }

        public void LookAt(Vector3 dir, bool withoutChangingAnimation = false)
        {
            var prevCachedFlipX = CachedFlipX;
            var prevCachedFront = CachedFront;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle += 45;
            if (angle < 0)
                angle += 360;
            CachedFront = angle is <= 0 or >= 180;
            CachedFlipX = angle is >= 90 and < 270;

            if (prevCachedFlipX != CachedFlipX)
                SetFlipOrNot();

            if (!withoutChangingAnimation && prevCachedFront != CachedFront)
                ChangeAnimation();
        }

        public void PlayIdleAnimation()
        {
            var hash = CachedFront ? FrontIdle : BackIdle;
            if (CachedAnimationHash == hash)
                return;
            CachedAnimationHash = hash;
            animator.Play(hash);
        }

        public void PlayMoveAnimation()
        {
            var hash = CachedFront ? FrontMove : BackMove;
            if (CachedAnimationHash == hash)
                return;
            CachedAnimationHash = hash;
            animator.Play(hash);
        }

        public void ChangeAnimation()
        {
            if (CachedAnimationHash == FrontIdle || CachedAnimationHash == BackIdle)
            {
                PlayIdleAnimation();
            }
            else if (CachedAnimationHash == FrontMove || CachedAnimationHash == BackMove)
            {
                PlayMoveAnimation();
            }
             
        }

        private void SetFlipOrNot()
        {
            for (var i = 0; i < spriteRendererList.Length; i++)
            {
                spriteRendererList[i].flipX = CachedFlipX;
            }
        }
    }
}