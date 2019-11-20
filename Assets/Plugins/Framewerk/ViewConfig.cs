using UnityEngine;
using UnityEngine.Serialization;

namespace Temari.Common
{
    public class ViewConfig : MonoBehaviour
    {
        public Camera Camera3d;
        public Camera UICamera;
        
        public Transform Container3d;
        public Transform UiBottom;
        public Transform UiDefault;
        public Transform Popups;
        public Transform UiOverlay;
    }
}