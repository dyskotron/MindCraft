using Framewerk;
using Framewerk.AppStateMachine;
using Framewerk.Managers;
using Framewerk.Popups;
using Framewerk.StrangeCore;
using MindCraft.Common;
using MindCraft.Controller;
using MindCraft.Data;
using MindCraft.Data.SaveLoadManager;
using MindCraft.MapGeneration.Utils;
using MindCraft.Model;
using MindCraft.Physics;
using MindCraft.View;
using MindCraft.View.Chunk;
using MindCraft.View.Inventory;
using MindCraft.View.Screen;
using strange.extensions.injector.api;
using Temari.Common;
using UnityEngine;

namespace MindCraft
{
    public class MindCraftContext : FramewerkMVCSContext
    {
        private ViewConfig _viewConfig;
        
        public MindCraftContext(MonoBehaviour view, ViewConfig viewConfig) : base(view, true)
        {
            _viewConfig = viewConfig;
        }

        protected override void mapBindings()
        {
            base.mapBindings();

            injectionBinder.Bind<IInjector>().To(injectionBinder.injector);

            //Framewerk core
            injectionBinder.Bind<ViewConfig>().ToValue(_viewConfig);
            injectionBinder.Bind<ICoroutineManager>().ToValue(CoroutineManager.Instance);
            injectionBinder.Bind<IUpdater>().ToValue(Updater.Instance);
            injectionBinder.Bind<IAppMonitor>().ToValue(AppMonitor.Instance);
            injectionBinder.Bind<IAssetManager>().To<AssetManager>().ToSingleton();
            injectionBinder.Bind<IUiManager>().To<UiManager>().ToSingleton();
            
            //General
            injectionBinder.Bind<IKeyboardMonitor>().To<KeyboardMonitor>().ToSingleton();
            injectionBinder.Bind<IMouseModeManager>().To<MouseModeManager>().ToSingleton();

            //Popups
            injectionBinder.Bind<IPopupManager>().To<PopupManager>().ToSingleton();
            injectionBinder.Bind<PopupOpenedSignal>().ToSingleton();
            injectionBinder.Bind<PopupClosedSignal>().ToSingleton();
            
            //FSM 
            injectionBinder.Bind<IAppFsm>().To<AppFsm>().ToSingleton();
            injectionBinder.Bind<AppStateEnterSignal>().ToSingleton();
            injectionBinder.Bind<AppStateExitSignal>().ToSingleton();

            //Controller
            injectionBinder.Bind<IPlayerController>().To<PlayerController>().ToSingleton();
            injectionBinder.Bind<IWorldRaycaster>().To<WorldRaycaster>().ToSingleton();

            //Model / Service
            injectionBinder.Bind<IInventoryModel>().To<InventoryModel>().ToSingleton();
            injectionBinder.Bind<IWorldModel>().To<WorldModel>().ToSingleton();
            injectionBinder.Bind<IVoxelPhysicsWorld>().To<VoxelPhysicsWorld>().ToSingleton();

            injectionBinder.Bind<ISaveLoadManager>().To<SaveLoadManager>().ToSingleton();
            
            //Data
            injectionBinder.Bind<IWorldSettings>().To<WorldSettings>().ToSingleton();
            injectionBinder.Bind<IWorldRenderer>().To<WorldRenderer>().ToSingleton();
            injectionBinder.Bind<IBiomeDefs>().To<BiomeDefs>().ToSingleton();
            injectionBinder.Bind<IBlockDefs>().To<BlockDefs>().ToSingleton();
            injectionBinder.Bind<TextureLookup>().To<TextureLookup>().ToSingleton();

            //View
            injectionBinder.Bind<ChunkView>().To<ChunkView>();
            injectionBinder.Bind<BlockMarker>().To<BlockMarker>();
            
            injectionBinder.Bind<GameAppScreen>().To<GameAppScreen>();
            mediationBinder.Bind<PlayerView>().To<PlayerMediator>();
            mediationBinder.Bind<QuitGamePopupView>().To<QuitGamePopupMediator>();
            mediationBinder.Bind<IntroPopupView>().To<IntroPopupMediator>();

            mediationBinder.Bind<InventoryView>().To<InventoryMediator>();
            mediationBinder.Bind<InventoryItemView>().To<InventoryItemMediator>();

            commandBinder.Bind<ContextStartSignal>().To<InitAppCommand>();
            
            commandBinder.Bind<KeyPressedSignal>().To<KeyPressedCommand>();
            
            commandBinder.Bind<BlockTypeSelectedSignal>().To<BlockTypeSelectedCommand>();
            commandBinder.Bind<SaveGameSignal>().To<SaveGameCommand>();
            
        }

        public override void OnRemove()
        {
            base.OnRemove();

            //Unbind everything using Native collections so we're not getting memory leak errors
            injectionBinder.Unbind<IWorldModel>();
            injectionBinder.Unbind<IWorldRenderer>();
            injectionBinder.Unbind<IBlockDefs>();
            injectionBinder.Unbind<IBiomeDefs>();
            injectionBinder.Unbind<TextureLookup>();
        }
    }
}