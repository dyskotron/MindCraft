using UnityEngine;

namespace MindCraft.Common
{
    public interface IMouseModeManager
    {
        void SetMouseLocked(bool locked);
    }

    public class MouseModeManager : IMouseModeManager
    {
        public void SetMouseLocked(bool locked)
        {
            if (locked)
            {
               
#if UNITY_STANDALONE_WIN
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = false;
                
#else           
                Cursor.lockState = CursorLockMode.Locked;
#endif
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }    
    }
}