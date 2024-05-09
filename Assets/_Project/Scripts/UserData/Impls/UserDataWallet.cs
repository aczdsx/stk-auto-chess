using System;
using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Universal;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserWallet userWallet;

        public static event Action<int> OnEnergyChanged;
        public static event Action<int> OnCoinChanged;
        public static event Action<int> OnJewelChanged;

        [Initialize(DataCategory.UserWallet)]
        private void Initialize_Wallet(string data)
        {
            if (data == null)
            {
                userWallet = new UserWallet
                {
                    Coin = 0,
                    // Jewel = 0,
                    // Energy = 100,
                };
                return;
            }

            userWallet = MessageUtility.FromBase64String<UserWallet>(data);
        }

        [ClearFunc]
        private void Clear_Wallet()
        {
            userWallet = null;
        }

        public void AddEnergy(int amount)
        {
            // userWallet.Energy += amount;
            // OnEnergyChanged?.Invoke(userWallet.Energy);
        }
    }
}
