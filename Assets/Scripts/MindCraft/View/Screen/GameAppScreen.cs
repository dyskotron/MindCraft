using Framewerk.AppStateMachine;
using MindCraft.View.Inventory;

namespace MindCraft.View.Screen
{
    public class GameAppScreen : AppStateScreen
    {
        protected override void Enter()
        {
            base.Enter();
            
            InstantiateView<InventoryView>();
        }
    }
}