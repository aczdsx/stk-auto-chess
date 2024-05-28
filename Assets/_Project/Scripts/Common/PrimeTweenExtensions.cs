using System.Diagnostics.CodeAnalysis;
using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;

namespace CookApps.AutoBattler
{
    public static class PrimeTweenExtensions
    {
        // y 방향으로 점프
        public static Sequence Jump([NotNull] Transform target, Vector3 endValue, float duration, float height, int numJumps = 1) {
            Assert.IsTrue(height > 0f);
            Assert.IsTrue(numJumps >= 1, nameof(numJumps) + "should be >= 1.");
            var jumpsSequence = Sequence.Create();
            var iniPosY = target.position.y;
            var deltaJump = (endValue.y - iniPosY) / numJumps;
            var jumpDuration = duration / (numJumps * 2);
            for (int i = 0; i < numJumps; i++) {
                var from = iniPosY + i * deltaJump;
                var to = iniPosY + (i + 1) * deltaJump;
                jumpsSequence.Chain(Tween.PositionY(target, Mathf.Max(from, to) + height, jumpDuration, Ease.OutQuad))
                    .Chain(Tween.PositionY(target, to, jumpDuration, Ease.InQuad));
            }
            var result = Sequence.Create()
                .Group(jumpsSequence);
            if (!Mathf.Approximately(target.position.x, endValue.x)) {
                result.Group(Tween.PositionX(target, endValue.x, duration, Ease.Linear));
            }
            if (!Mathf.Approximately(target.position.z, endValue.z)) {
                result.Group(Tween.PositionZ(target, endValue.z, duration, Ease.Linear));
            }

            return result;
        }

        // MoveTo 메서드 추가
        public static Sequence MoveTo([NotNull] Transform target, Vector3 endValue, float duration, Ease ease = Ease.Linear)
        {
            Assert.IsTrue(duration > 0f, nameof(duration) + " should be > 0.");

            var moveSequence = Sequence.Create();

            if (!Mathf.Approximately(target.position.x, endValue.x))
            {
                moveSequence.Group(Tween.PositionX(target, endValue.x, duration, ease));
            }
            if (!Mathf.Approximately(target.position.y, endValue.y))
            {
                moveSequence.Group(Tween.PositionY(target, endValue.y, duration, ease));
            }
            if (!Mathf.Approximately(target.position.z, endValue.z))
            {
                moveSequence.Group(Tween.PositionZ(target, endValue.z, duration, ease));
            }

            return moveSequence;
        }
    }
}
