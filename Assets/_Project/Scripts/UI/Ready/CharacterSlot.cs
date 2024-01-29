using System;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;

namespace CookApps.SampleTeamBattle
{
    public class CharacterSlot : CachedMonoBehaviour
    {
        [SerializeField] private CAButton button;

        public int CharacterId { get; private set; }
        public bool IsInDeck { get; private set; }

        public event Action<CharacterSlot> OnClickSlot;

        private void Awake()
        {
            button.onClick.AddListener(OnClick);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            button.onClick.RemoveListener(OnClick);
        }

        internal void SetCharacterData(bool isInDeck, int characterId)
        {
            IsInDeck = isInDeck;
            CharacterId = characterId;
        }

        private void OnClick()
        {
            OnClickSlot?.Invoke(this);
        }
    }
}
