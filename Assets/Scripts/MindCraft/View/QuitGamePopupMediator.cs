using Framewerk.UI;
using MindCraft.Data.SaveLoadManager;
using UnityEngine;

namespace MindCraft.View
{
    public class QuitGamePopupMediator : ExtendedMediator
    {
        [Inject] public QuitGamePopupView View { get; set; }
        
        [Inject] public SaveGameSignal SaveGameSignal { get; set; }
        
        public override void OnRegister()
        {
            base.OnRegister();
            
            AddButtonListener(View.QuitButton, QuitButtonHandler);
            AddButtonListener(View.BackButton, BackButtonHandler);
            AddButtonListener(View.BackgroundButton, BackButtonHandler);
            
            Cursor.lockState = CursorLockMode.None;
        }

        private void BackButtonHandler()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Destroy(View.gameObject);    
        }

        private void QuitButtonHandler()
        {
            SaveGameSignal.Dispatch();
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();     
#endif
        }
    }
}