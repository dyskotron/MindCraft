using Framewerk.UI;
using MindCraft.Controller;
using UnityEngine;

namespace MindCraft.View
{
    public class IntroPopupMediator : ExtendedMediator
    {
        [Inject] public IntroPopupView View { get; set; }
        [Inject] public IPlayerController PlayerController { get; set; }

        public override void OnRegister()
        {
            base.OnRegister();

            PlayerController.SetEnabled(false);
            
            AddButtonListener(View.OkButton, OkButtonHandler);
            
            //actually make sure player have read the intro text and clicked mkey button, mkey?
            //AddButtonListener(View.BackgroundButton, OkButtonHandler);
        }

        private void OkButtonHandler()
        {
            Destroy(View.gameObject);  
            PlayerController.SetEnabled(true);
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}