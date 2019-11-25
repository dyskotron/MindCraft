using strange.extensions.command.impl;
using strange.extensions.signal.impl;
using UnityEngine;

namespace MindCraft.Data.SaveLoadManager
{
    public class SaveGameSignal : Signal
    {
        
    }
    
    public class SaveGameCommand : Command
    {
        [Inject] public ISaveLoadManager SaveLoadManager { get; set; }
        
        public override void Execute()
        {
            Debug.LogWarning($"<color=\"aqua\">SaveGameCommand.Execute() : SAVEGAMEEE</color>");
            
            SaveLoadManager.SaveGame();    
        }
    }
}