using System;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CharacterSlot : CachedMonoBehaviour
    {
    //     [SerializeField] private CAButton button;
    //     [SerializeField] private SimpleImageColorSwapper gradeColorSwapper;
    //     [SerializeField] private Image thumbnail;
    //     [SerializeField] private GameObject[] starBgs;
    //     [SerializeField] private GameObject[] stars;
    //     [SerializeField] private SimpleSwapper[] typeSwappers;
    //     [SerializeField] private SimpleSwapper[] positionSwappers;
    //     [SerializeField] private TMP_Text levelText;
    //     [SerializeField] private GameObject deployedNode;
    //
    //     public int CharacterId { get; private set; }
    //     public bool IsInDeck { get; private set; }
    //     private bool isEnemy = false;
    //
    //     public event Action<CharacterSlot> OnClickSlot;
    //
    //     private void Awake()
    //     {
    //         button.onClick.AddListener(OnClick);
    //     }
    //
    //     protected override void OnDestroy()
    //     {
    //         base.OnDestroy();
    //         button.onClick.RemoveListener(OnClick);
    //     }
    //
    //     private void SetDefaultCharacterData(int characterId)
    //     {
    //         TestSpecCharacter specCharacter = SpecDataManager.Instance.TestSpecCharacter.Get(characterId);
    //         thumbnail.sprite = AtlasManager.Instance.GetSprite("CharacterIcon", ZString.Format("CharIcon_{0}", specCharacter.prefab_id));
    //         gradeColorSwapper.Swap(SimpleSwapType.Grade_0 + (int) specCharacter.grade - 1);
    //
    //         foreach (SimpleSwapper typeSwapper in typeSwappers)
    //         {
    //             typeSwapper.Swap(specCharacter.type.ToSimpleSwapType());
    //         }
    //
    //         foreach (SimpleSwapper positionSwapper in positionSwappers)
    //         {
    //             positionSwapper.Swap(specCharacter.position.ToSimpleSwapType());
    //         }
    //     }
    //
    //     internal void SetCharacterData(bool isInDeck, int characterId)
    //     {
    //         isEnemy = false;
    //         IsInDeck = isInDeck;
    //         CharacterId = characterId;
    //         SetDefaultCharacterData(characterId);
    //         UserCharacter userCharacter = UserDataManager.Instance.GetUserCharacter(characterId);
    //         for (var i = 0; i < starBgs.Length; i++)
    //         {
    //             starBgs[i].SetActive(true);
    //             stars[i].SetActive(i < userCharacter.StarLevel);
    //         }
    //
    //         levelText.text = userCharacter.Level.ToString();
    //
    //         deployedNode.SetActive(UserDataManager.Instance.IsDeployed(characterId));
    //     }
    //
    //     internal void SetEnemyCharacterData(int characterId, int level)
    //     {
    //         isEnemy = true;
    //         CharacterId = characterId;
    //         SetDefaultCharacterData(characterId);
    //         for (var i = 0; i < starBgs.Length; i++)
    //         {
    //             starBgs[i].SetActive(false);
    //         }
    //
    //         levelText.text = level.ToString();
    //     }
    //
    //     private void OnClick()
    //     {
    //         if (isEnemy)
    //         {
    //             return;
    //         }
    //
    //         OnClickSlot?.Invoke(this);
    //     }
    }
}
