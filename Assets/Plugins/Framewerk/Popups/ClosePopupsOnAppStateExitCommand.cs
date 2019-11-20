using strange.extensions.command.impl;

namespace Framewerk.Popups
{
    public class ClosePopupsOnAppStateExitCommand : Command
    {
        [Inject] public IPopupManager PopupManager { get; set; }
        
        public override void Execute()
        {
            PopupManager.CloseAllPopups();
        }
    }
}