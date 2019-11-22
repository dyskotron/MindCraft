using System.Collections.Generic;
using Framewerk.UI.List;
using MindCraft.Controller;
using MindCraft.Data;
using strange.extensions.mediation.api;
using UnityEngine;

namespace MindCraft.View.Inventory
{
    public class InventoryMediator : ListMediator<InventoryView, InventoryItemData>
    {
        [Inject] public BlockTypeSelectedSignal BlockTypeSelectedSignal { get; set; }

        private HashSet<KeyCode> _keyCodes = new HashSet<KeyCode>();
        
        public override void OnRegister()
        {
            base.OnRegister();
            
            var items = new List<InventoryItemData>();
            
            items.Add(new InventoryItemData(BlockTypeId.Wood));
            items.Add(new InventoryItemData(BlockTypeId.Stone));
            items.Add(new InventoryItemData(BlockTypeId.Dirt));
            items.Add(new InventoryItemData(BlockTypeId.DirtWithGrass));
            items.Add(new InventoryItemData(BlockTypeId.DirtWithSand));
            items.Add(new InventoryItemData(BlockTypeId.Sand));
            items.Add(new InventoryItemData(BlockTypeId.Glass));
            items.Add(new InventoryItemData(BlockTypeId.Trunk));
            items.Add(new InventoryItemData(BlockTypeId.Leaves));
            
            SetData(items);

            _keyCodes.Add(KeyCode.Alpha1);
            _keyCodes.Add(KeyCode.Alpha2);
            _keyCodes.Add(KeyCode.Alpha3);
            _keyCodes.Add(KeyCode.Alpha4);
            _keyCodes.Add(KeyCode.Alpha5);
            _keyCodes.Add(KeyCode.Alpha6);
            _keyCodes.Add(KeyCode.Alpha7);
            _keyCodes.Add(KeyCode.Alpha8);
            _keyCodes.Add(KeyCode.Alpha9);
        }

        public override void RegisterMediator(IMediator mediator)
        {
            base.RegisterMediator(mediator);
            
            //TODO: proper init for lists
            if(ItemMediators.Count > 0)
                SelectItemAt(0);
        }

        protected override void ListItemSelected(int index, InventoryItemData dataProvider)
        {
            BlockTypeSelectedSignal.Dispatch(dataProvider.BlockTypeId);
        }

        private void Update()
        {
            // Select item by mouse scroll
            if (Mathf.Abs(Input.mouseScrollDelta.y) > 0)
            {
                var numItems = DataProviders.Count;
                var selectedId = SelectedItemIndexes[0];

                var newId = (int)(selectedId - Mathf.Sign(Input.mouseScrollDelta.y) + numItems) % numItems;
                SelectItemAt(newId);
                
                BlockTypeSelectedSignal.Dispatch(GetSelectedItem().BlockTypeId);
            }

            // Select by keyboard
            var id = 0;
            foreach (var keyCode in _keyCodes)
            {
                if (id >= DataProviders.Count)
                    break;
                
                if (Input.GetKeyDown(keyCode))
                {
                    SelectItemAt(id);
                    BlockTypeSelectedSignal.Dispatch((BlockTypeId)id);
                    break;
                }

                id++;
            }
        }
    }
}