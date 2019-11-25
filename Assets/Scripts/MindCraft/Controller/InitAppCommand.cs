using Framewerk.AppStateMachine;
using Framewerk.Managers;
using MindCraft.Common;
using MindCraft.Controller.Fsm;
using MindCraft.View;
using strange.extensions.command.impl;
using Temari.Common;
using UnityEngine;

namespace MindCraft.Controller
{
    public class InitAppCommand : Command
    {
        [Inject] public IAppFsm AppFsm { get; set; }
        [Inject] public IUiManager UiManager { get; set; }
        [Inject] public ViewConfig ViewConfig { get; set; }
        
        [Inject] public IKeyboardMonitor KeyboardMonitor { get; set; }
        
        public override void Execute()
        {
            KeyboardMonitor.RegisterKeycode(KeyCode.Escape);
            
            //TODO: load stuff
            
            AppFsm.SwitchState(new GameAppState());

            UiManager.InstantiateView<IntroPopupView>(ResourcePath.POPUPS_ROOT, ViewConfig.Popups);
        }
    }
}