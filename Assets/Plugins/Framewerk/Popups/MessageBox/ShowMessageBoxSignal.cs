using strange.extensions.command.impl;
using strange.extensions.promise.api;
using strange.extensions.signal.impl;

namespace Framewerk.Popups
{
    public class ShowMessageBoxSignal : Signal<IPromise, string>
    {
        
    }
    
    public class ShowMessageBoxCommand : Command
    {
        [Inject] public string Message { get; set; }
        [Inject] public IPromise OkClickedPromise { get; set; }
        [Inject] public IPopupManager PopupManager { get; set; }
        
        public override void Execute()
        {
            PopupManager.InstantiatePopup<MessageBoxView>(Message, new PopupButtonSetting()
            {
                clickHandler = () => { }, 
                clickPromise = OkClickedPromise, 
                closesPopup = true, 
                optionText = "Ok"
            });
        }
    }
}