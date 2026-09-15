using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Sapphire.Domain.Character;
using Sapphire.Infrastructure.Character;
using Sapphire.Presentation.Session;

namespace Sapphire.Presentation.CharacterFlow
{
    /// <summary>
    /// Lists the signed-in account's roster (up to CharacterRoster.MaxSlots
    /// cards - one per slot, "filled" or "empty", see CharacterSlotCardView),
    /// with 선택 (play) / 삭제 (delete, behind ConfirmDialog) on filled slots
    /// and 생성 (-> CharacterCreate) on empty ones. Reloads the roster from
    /// disk on Awake and after every mutation, rather than caching it only
    /// in memory, so a delete/create actually persists (CharacterCreate
    /// writes to the same file via the same repository type).
    /// </summary>
    public sealed class CharacterSelectController : MonoBehaviour
    {
        [SerializeField] private Sprite magePortrait;
        [SerializeField] private Sprite warriorPortrait;
        [SerializeField] private CharacterSlotCardView[] slotCards = new CharacterSlotCardView[CharacterRoster.MaxSlots];
        [SerializeField] private ConfirmDialog confirmDialog;

        private CharacterRosterFileRepository repository;
        private CharacterRoster roster;

        private void Awake()
        {
            repository = new CharacterRosterFileRepository();
            ReloadAndRefresh();
        }

        private void ReloadAndRefresh()
        {
            string accountId = CharacterSessionService.Instance.AccountId;
            roster = new CharacterRoster(repository.Load(accountId));
            RefreshCards();
        }

        private void RefreshCards()
        {
            for (int i = 0; i < slotCards.Length; i++)
            {
                CharacterSlotCardView card = slotCards[i];
                if (card == null)
                {
                    continue;
                }

                if (i < roster.Slots.Count)
                {
                    BindFilledCard(card, roster.Slots[i]);
                }
                else
                {
                    BindEmptyCard(card);
                }
            }
        }

        private void BindFilledCard(CharacterSlotCardView card, CharacterSlot slot)
        {
            if (card.filledRoot != null) card.filledRoot.SetActive(true);
            if (card.emptyRoot != null) card.emptyRoot.SetActive(false);
            if (card.nameText != null) card.nameText.text = slot.Name;
            if (card.classLevelText != null) card.classLevelText.text = $"{ClassLabel(slot.Class)} Lv.{slot.Level}";
            if (card.portraitImage != null) card.portraitImage.sprite = slot.Class == CharacterClass.Warrior ? warriorPortrait : magePortrait;

            if (card.selectButton != null)
            {
                card.selectButton.onClick.RemoveAllListeners();
                card.selectButton.onClick.AddListener(() => HandleSelect(slot));
            }

            if (card.deleteButton != null)
            {
                card.deleteButton.onClick.RemoveAllListeners();
                card.deleteButton.onClick.AddListener(() => HandleDeleteRequested(slot));
            }
        }

        private void BindEmptyCard(CharacterSlotCardView card)
        {
            if (card.filledRoot != null) card.filledRoot.SetActive(false);
            if (card.emptyRoot != null) card.emptyRoot.SetActive(true);

            if (card.createButton != null)
            {
                card.createButton.onClick.RemoveAllListeners();
                card.createButton.onClick.AddListener(HandleCreateRequested);
            }
        }

        private void HandleSelect(CharacterSlot slot)
        {
            CharacterSessionService.Instance.SelectCharacter(slot);
            SceneManager.LoadScene("VillageHub");
        }

        private void HandleDeleteRequested(CharacterSlot slot)
        {
            if (confirmDialog == null)
            {
                return;
            }

            confirmDialog.Show(slot.Name, "이 캐릭터를 삭제하시겠습니까? 되돌릴 수 없습니다.", () => HandleDeleteConfirmed(slot));
        }

        private void HandleDeleteConfirmed(CharacterSlot slot)
        {
            roster.TryRemoveCharacter(slot.Id);
            repository.Save(CharacterSessionService.Instance.AccountId, roster.Slots);
            RefreshCards();
        }

        private void HandleCreateRequested()
        {
            SceneManager.LoadScene("CharacterCreate");
        }

        private static string ClassLabel(CharacterClass characterClass)
        {
            return characterClass == CharacterClass.Warrior ? "전사" : "법사";
        }
    }
}
