using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    public class BuffStackData
    {
        public ObfuscatorInt sourceCodeId;
        public ObfuscatorDouble value;
        public ObfuscatorFloat elapsedTime;
        public ObfuscatorFloat duration;
        public IEffectCodeSource source;

        public void SetData(int sourceCodeId, float duration, double value, IEffectCodeSource source)
        {
            this.sourceCodeId = sourceCodeId;
            this.value = value;
            this.duration = duration;
            this.source = source;
            this.elapsedTime = 0;
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

