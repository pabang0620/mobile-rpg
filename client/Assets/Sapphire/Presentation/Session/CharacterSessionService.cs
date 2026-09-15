using UnityEngine;
using Sapphire.Domain.Character;

namespace Sapphire.Presentation.Session
{
    /// <summary>
    /// The one small cross-scene session service (Login -> CharacterSelect ->
    /// CharacterCreate -> VillageHub all read/write this) carrying which
    /// account signed in and which character was selected to play. This is
    /// deliberately the only static/singleton state this slice introduces -
    /// every other new piece of state (roster contents, name-input text,
    /// selected class card) lives on a scene-local MonoBehaviour instead of
    /// being reached for through more statics. Survives scene loads via
    /// DontDestroyOnLoad, since AccountId/SelectedCharacter must still be
    /// readable after SceneManager.LoadScene swaps the active scene.
    /// </summary>
    public sealed class CharacterSessionService : MonoBehaviour
    {
        private static CharacterSessionService instance;

        public static CharacterSessionService Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("CharacterSessionService");
                    instance = go.AddComponent<CharacterSessionService>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        public string AccountId { get; private set; }
        public CharacterSlot SelectedCharacter { get; private set; }

        public void SignIn(string accountId)
        {
            AccountId = accountId;
        }

        public void SelectCharacter(CharacterSlot slot)
        {
            SelectedCharacter = slot;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
