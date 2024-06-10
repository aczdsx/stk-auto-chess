using System.Collections;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CharacterDetailSkillLayer : CachedMonoBehaviour
    {
        private SpecCharacter _specCharacterData;
        private UserCharacter _userCharacterData;

        public void InitLayer(int prefabID)
        {
            _specCharacterData = SpecDataManager.Instance.GetCharacterData(prefabID);
            _userCharacterData = UserDataManager.Instance.GetUserCharacter(prefabID);


        }
    }
}
