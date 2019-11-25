using System.Collections.Generic;
using Framewerk;
using Framewerk.StrangeCore;
using UnityEngine;

namespace MindCraft.Controller
{
    public interface IKeyboardMonitor
    {
        void RegisterKeycode(KeyCode keyCode);
        void RemoveKeycode(KeyCode keyCode);
        void RemoveAll();
    }

    public class KeyboardMonitor : IDestroyable, IKeyboardMonitor
    {
        [Inject] public IUpdater Updater { get; set; }
        
        [Inject] public KeyPressedSignal KeyPressedSignal { get; set; }
        
        private List<KeyCode> _registeredKeycodes = new List<KeyCode>();

        [PostConstruct]
        public void PostConstruct()
        {
            Updater.EveryFrame(Update);
        }

        public void Destroy()
        {
            Updater.RemoveFrameAction(Update);
        }

        public void RegisterKeycode(KeyCode keyCode)
        {
            _registeredKeycodes.Add(keyCode);    
        }
        
        public void RemoveKeycode(KeyCode keyCode)
        {
            _registeredKeycodes.Remove(keyCode);    
        }

        public void RemoveAll()
        {
            _registeredKeycodes.Clear();    
        }
        
        private void Update()
        {
            foreach (var keyCode in _registeredKeycodes)
            {
                if(Input.GetKeyDown(keyCode))  
                    KeyPressedSignal.Dispatch(keyCode);
            } 
            
        }
    }
}