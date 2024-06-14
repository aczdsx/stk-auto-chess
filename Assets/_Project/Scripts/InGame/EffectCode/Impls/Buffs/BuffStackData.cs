using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    public class BuffStackData
    {
        public ObfuscatorInt sourceId;
        public ObfuscatorDouble value;
        public ObfuscatorFloat elapsedTime;
        public ObfuscatorFloat duration;

        public void SetData(int sourceId, float duration, double value)
        {
            this.sourceId = sourceId;
            this.value = value;
            this.duration = duration;
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
