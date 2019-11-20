using Framewerk.AppStateMachine;
using MindCraft.Controller.Fsm;
using strange.extensions.command.impl;
using UnityEngine;

namespace MindCraft.Controller
{
    public class InitAppCommand : Command
    {
        [Inject] public IAppFsm AppFsm { get; set; }
        
        public override void Execute()
        {
            //load all data and shit here
            Debug.LogWarning($"<color=\"aqua\">InitAppCommand.Execute() : MindCraft started</color>");
            
            AppFsm.SwitchState(new GameAppState());
        }
    }
}