using Framewerk.Managers;
using Framewerk.UI.List;
using MindCraft.Common;
using UnityEngine;

namespace MindCraft.View.Inventory
{
    public class InventoryItemMediator : ListItemMediator<InventoryItemView, InventoryItemData>
    {
        [Inject] public IAssetManager AssetManager { get; set; }

        public override void SetData(InventoryItemData dataProvider, int index)
        {
            base.SetData(dataProvider, index);

            View.Icon.sprite = AssetManager.GetSpriteFromAtlas(ResourcePath.MAIN_UI_ATLAS, dataProvider.BlockTypeId.ToString());
        }

        public override void SetSelected(bool selected)
        {
            base.SetSelected(selected);
            
            View.SelectedBorder.color = selected ? View.SelectedColor : Color.white;
        }
    }
}