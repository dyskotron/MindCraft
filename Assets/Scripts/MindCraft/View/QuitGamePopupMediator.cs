using Framewerk.UI;
using MindCraft.Common;
using MindCraft.Controller;
using MindCraft.Data.SaveLoadManager;
using UnityEngine;

namespace MindCraft.View
{
    public class QuitGamePopupMediator : ExtendedMediator
    {
        [Inject] public IPlayerController PlayerController { get; set; }
        [Inject] public IMouseModeManager MouseModeManager { get; set; }
        
        [Inject] public SaveGameSignal SaveGameSignal { get; set; }
        
        [Inject] public QuitGamePopupView View { get; set; }

        //temp fix no popup manager yet
        public static bool IsOpen = false;
        
        public override void OnRegister()
        {
            base.OnRegister();
            
            AddButtonListener(View.QuitButton, QuitButtonHandler);
            AddButtonListener(View.BackButton, BackButtonHandler);
            AddButtonListener(View.BackgroundButton, BackButtonHandler);
            
            MouseModeManager.SetMouseLocked(false);
            PlayerController.SetEnabled(false);

            IsOpen = true;
        }

        private void BackButtonHandler()
        {
            MouseModeManager.SetMouseLocked(true);
            PlayerController.SetEnabled(true);
            Destroy(View.gameObject);
            IsOpen = false;
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