using System;
using strange.extensions.command.impl;
using strange.extensions.promise.api;
using strange.extensions.signal.impl;

namespace Framewerk.Popups.OkCancelWindow
{
    public class ShowOkCancelWindowSignal : Signal<Action<bool>, string>
    {
        
    }
    
    public class ShowOkCancelWindowCommand : Command
    {
        [Inject] public string Message { get; set; }
        [Inject] public Action<bool> WindowResult { get; set; }
        [Inject] public IPopupManager PopupManager { get; set; }
        
        public override void Execute()
        {
            Retain();

            PopupManager.InstantiatePopup<OkCancelWindowView>(Message, 
                new PopupButtonSetting()
                {
                    clickHandler = OnOkClicked, 
                    clickPromise = null, 
                    closesPopup = true, 
                    optionText = "Ok"
                },
                new PopupButtonSetting()
                {
                    clickHandler = OnCancelClicked, 
                    clickPromise = null, 
                    closesPopup = true, 
                    optionText = "Cancel"
                }
            );            
        }

        private void OnOkClicked()
        {
            WindowResult.Invoke(true);
            Release();
        }

        private void OnCancelClicked()
        {
            WindowResult.Invoke(false);
            Release();
        }
    }
}