using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Sapphire.Presentation.UI
{
    public sealed class MainMenuPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button[] menuButtons;
        [SerializeField] private SimpleMessagePanel messagePanel;
        [SerializeField] private string[] menuNames;

        public void Configure(GameObject root,Button opener,Button[] buttons,SimpleMessagePanel messages,string[] names)
        {panelRoot=root;openButton=opener;menuButtons=buttons;messagePanel=messages;menuNames=names;}

        private void Awake()
        {
            if(openButton!=null)openButton.onClick.AddListener(Toggle);
            if(menuButtons==null)return;
            for(int i=0;i<menuButtons.Length;i++){int index=i;if(menuButtons[i]!=null)menuButtons[i].onClick.AddListener(()=>Select(index));}
        }

        private void Update()
        {
            if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Toggle();
        }

        public void Toggle(){if(panelRoot!=null)panelRoot.SetActive(!panelRoot.activeSelf);}
        public void Close(){if(panelRoot!=null)panelRoot.SetActive(false);}
        private void Select(int index)
        {
            string label=menuNames!=null&&index<menuNames.Length?menuNames[index]:"메뉴";
            Close();
            if(messagePanel!=null)
            {
                messagePanel.SetText(label+" 기능은 다음 콘텐츠 단계에서 연결됩니다.");
                messagePanel.Show();
            }
        }
    }
}
