using System.Collections.Generic;
using Com.Cookapps.Sampleteambattle;
using CookApps.gRPC.Universal;
using Google.Protobuf;

namespace CookApps.SampleTeamBattle
{
    public static class UserDataDefault
    {
        private static Dictionary<string, string> _userDataDict = new ();

        public static string Get(DataCategory dataCategory)
        {
            string result;
            if (_userDataDict.TryGetValue(dataCategory.ToCategoryString(), out result) == false)
            {
                IMessage message = CreateInstance(dataCategory);
                result = MessageUtility.ToBase64String(message);
                _userDataDict.Add(dataCategory.ToCategoryString(), result);
            }

            return result;
        }

        internal static IMessage CreateInstance(DataCategory dataCategory)
        {
            IMessage message = null;
            switch (dataCategory)
            {
                case DataCategory.UserData:
                    message = new UserData
                    {
                        Uid = 0,
                        Level = 1,
                        Exp = 0,
                    };
                    break;

                case DataCategory.UserWallet:
                    message = new UserWallet
                    {
                        Coin = 0,
                        Gem = 0,
                        Bread = 100,
                        ETicket = 0,
                        KTicket = 0,
                    };
                    break;

                case DataCategory.UserStage:
                    message = new UserStage
                    {
                        LastStageId = 1,
                        CurrentStageId = 1,
                    };
                    break;
            }

            return message;
        }
    }
}
