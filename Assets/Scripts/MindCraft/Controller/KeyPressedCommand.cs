using Framewerk.Managers;
using MindCraft.Common;
using MindCraft.View;
using Plugins.Framewerk;
using strange.extensions.command.impl;
using strange.extensions.signal.impl;
using UnityEngine;

namespace MindCraft.Controller
{
    public class KeyPressedSignal : Signal<KeyCode>
    {
        
    }

    public class KeyPressedCommand : Command
    {
        [Inject] public IUiManager UiManager { get; set; }
        [Inject] public ViewConfig ViewConfig { get; set; }
        
        [Inject] public KeyCode KeyCode { get; set; }
        
        public override void Execute()
        {
            if (KeyCode == KeyCode.Escape && !QuitGamePopupMediator.IsOpen)
                UiManager.InstantiateView<QuitGamePopupView>(ResourcePath.POPUPS_ROOT, ViewConfig.Popups);

        }
    }
}