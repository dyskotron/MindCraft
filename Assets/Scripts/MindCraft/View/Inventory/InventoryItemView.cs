using Framewerk.UI.List;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindCraft.View.Inventory
{
    public class InventoryItemView : ListItemView
    {
        public Image Icon;
        public Image SelectedBorder;
        public TMP_Text CountLabel;
        public Color SelectedColor;
    }
}