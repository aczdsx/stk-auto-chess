using System;
using Com.Cookapps.Sampleteambattle;

namespace CookApps.SampleTeamBattle
{
    public interface IUserData
    {
        DataCategory DataCategory { get; }
        int Priority { get; } // 낮을수록 먼저 초기화됨

        void Initialize(string data);
    }
}
