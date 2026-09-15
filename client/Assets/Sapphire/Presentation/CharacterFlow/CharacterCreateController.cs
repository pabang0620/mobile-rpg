using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Sapphire.Domain.Character;
using Sapphire.Infrastructure.Character;
using Sapphire.Presentation.Session;

namespace Sapphire.Presentation.CharacterFlow
{
    /// <summary>
    /// Class pick (mage/warrior card) + name input + 생성. On success the new
    /// character is saved to the account's roster file and selected as the
    /// character to play (docs/planning/01_PRODUCT.md's stated flow is
    /// select -> [if a slot is empty] create -> village, read as "create
    /// leads straight into the village" rather than back to the select
    /// screen - see AGENTS.md task notes).
    /// </summary>
    public sealed class CharacterCreateController : MonoBehaviour
    {
        [SerializeField] private Button mageCardButton;
        [SerializeField] private GameObject mageSelectedHighlight;
        [SerializeField] private Button warriorCardButton;
        [SerializeField] private GameObject warriorSelectedHighlight;
        [SerializeField] private InputField nameInput;
        [SerializeField] private Text errorText;
        [SerializeField] private Button createButton;

        private CharacterClass selectedClass = CharacterClass.Mage;
        private CharacterRosterFileRepository repository;

        private void Awake()
        {
            repository = new CharacterRosterFileRepository();

            if (mageCardButton != null)
            {
                mageCardButton.onClick.AddListener(() => SetSelectedClass(CharacterClass.Mage));
            }

            if (warriorCardButton != null)
            {
                warriorCardButton.onClick.AddListener(() => SetSelectedClass(CharacterClass.Warrior));
            }

            if (createButton != null)
            {
                createButton.onClick.AddListener(HandleCreateClicked);
            }

            SetSelectedClass(CharacterClass.Mage);
            SetError(string.Empty);
        }

        // D2 fix (2026-09-15): selection used to be shown only by toggling
        // the highlight glow GameObject's active state, leaving both cards
        // at identical brightness/scale (orchestrator screenshot
        // flow_create.png read as "unfinished" - a thick olive border was
        // the only cue). Now the selected card also goes full brightness +
        // a slight scale-up, the unselected one dims, matching
        // CharacterCreateSceneBuilder.BuildClassCard's initial dim tint
        // (same literals, duplicated here rather than shared across the
        // Editor/Presentation assembly boundary - see that file's comment).
        private static readonly Color SelectedCardTint = Color.white;
        private static readonly Color UnselectedCardTint = new Color(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Vector3 SelectedCardScale = new Vector3(1.04f, 1.04f, 1f);

        private void SetSelectedClass(CharacterClass characterClass)
        {
            selectedClass = characterClass;
            if (mageSelectedHighlight != null) mageSelectedHighlight.SetActive(characterClass == CharacterClass.Mage);
            if (warriorSelectedHighlight != null) warriorSelectedHighlight.SetActive(characterClass == CharacterClass.Warrior);
            ApplyCardSelectionVisual(mageCardButton, characterClass == CharacterClass.Mage);
            ApplyCardSelectionVisual(warriorCardButton, characterClass == CharacterClass.Warrior);
        }

        private static void ApplyCardSelectionVisual(Button cardButton, bool selected)
        {
            if (cardButton == null)
            {
                return;
            }

            Image cardImage = cardButton.GetComponent<Image>();
            if (cardImage != null)
            {
                cardImage.color = selected ? SelectedCardTint : UnselectedCardTint;
            }

            cardButton.transform.localScale = selected ? SelectedCardScale : Vector3.one;
        }

        private void HandleCreateClicked()
        {
            string accountId = CharacterSessionService.Instance.AccountId;
            string name = nameInput != null ? nameInput.text.Trim() : string.Empty;

            var roster = new CharacterRoster(repository.Load(accountId));
            CharacterRosterResult result = roster.TryAddCharacter(Guid.NewGuid().ToString("N"), name, selectedClass, out CharacterSlot created);

            if (result != CharacterRosterResult.Added)
            {
                SetError(DescribeRejection(result));
                return;
            }

            repository.Save(accountId, roster.Slots);
            CharacterSessionService.Instance.SelectCharacter(created);
            SceneManager.LoadScene("VillageHub");
        }

        private void SetError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
            }
        }

        private static string DescribeRejection(CharacterRosterResult result)
        {
            switch (result)
            {
                case CharacterRosterResult.RosterFull:
                    return $"캐릭터는 최대 {CharacterRoster.MaxSlots}개까지 만들 수 있습니다.";
                case CharacterRosterResult.NameTooShort:
                    return $"이름은 최소 {CharacterNameValidator.MinLength}자 이상이어야 합니다.";
                case CharacterRosterResult.NameTooLong:
                    return $"이름은 최대 {CharacterNameValidator.MaxLength}자까지 가능합니다.";
                case CharacterRosterResult.NameInvalidCharacters:
                    return "이름은 한글/영문/숫자만 사용할 수 있습니다.";
                case CharacterRosterResult.NameDuplicate:
                    return "이미 사용 중인 이름입니다.";
                default:
                    return "캐릭터를 생성할 수 없습니다.";
            }
        }
    }
}
