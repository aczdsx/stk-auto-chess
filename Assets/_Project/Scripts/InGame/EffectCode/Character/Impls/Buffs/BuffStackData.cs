using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    public class BuffStackData
    {
        public enum BuffShowPosition
        {
            DEFAULT = 0,//기본 위쪽 위치
            SIDE = 1,// 옆 위치
        }
        public ObfuscatorInt sourceCodeId;
        public ObfuscatorDouble value;
        public ObfuscatorFloat elapsedTime;
        public ObfuscatorFloat duration;
        public IEffectCodeSource source;
        public bool isShowValue = false;
        public BuffShowPosition showPosition = BuffShowPosition.DEFAULT;

        public void SetData(int sourceCodeId, float duration, double value, IEffectCodeSource source,
        bool isShowValue = false, BuffShowPosition showPosition = BuffShowPosition.DEFAULT)
        {
            this.sourceCodeId = sourceCodeId;
            this.value = value;
            this.duration = duration;
            this.source = source;
            this.elapsedTime = 0;
            this.isShowValue = isShowValue;
            this.showPosition = showPosition;
        }

        public bool AddDeltaTime(float dt)
        {
            this.elapsedTime += dt;
            if (elapsedTime > duration)
            {
                return true;
            }

            return false;
        }
    }
}

