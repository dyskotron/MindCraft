using Framewerk;
using Framewerk.AppStateMachine;
using Framewerk.Managers;
using Framewerk.Popups;
using Framewerk.StrangeCore;
using MindCraft.Controller;
using MindCraft.Data;
using MindCraft.MapGeneration.Lookup;
using MindCraft.Model;
using MindCraft.Physics;
using MindCraft.View;
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
            
            //World
            injectionBinder.Bind<IWorldModel>().To<WorldModel>().ToSingleton();
            injectionBinder.Bind<IVoxelPhysicsWorld>().To<VoxelPhysicsWorld>();
            injectionBinder.Bind<Chunk>().To<Chunk>();
            injectionBinder.Bind<BlockMarker>().To<BlockMarker>();
            
            //Data
            injectionBinder.Bind<IWorldSettings>().To<WorldSettings>().ToSingleton();
            injectionBinder.Bind<ChunksRenderer>().To<ChunksRenderer>().ToSingleton();
            injectionBinder.Bind<IBlockDefs>().To<BlockDefs>().ToSingleton();
            injectionBinder.Bind<TextureLookup>().To<TextureLookup>().ToSingleton();
            
            //View
            mediationBinder.Bind<PlayerView>().To<PlayerMediator>();
            
            injectionBinder.Bind<GameAppScreen>().To<GameAppScreen>();
            
            commandBinder.Bind<ContextStartSignal>().To<InitAppCommand>();
            
        }
    }
}