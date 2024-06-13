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

        public void InitLayer(int characterID)
        {
            _specCharacterData = SpecDataManager.Instance.GetCharacterData(characterID);
            _userCharacterData = UserDataManager.Instance.GetUserCharacter(characterID);


        }
    }
}
