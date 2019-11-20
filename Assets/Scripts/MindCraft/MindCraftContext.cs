using Framewerk;
using Framewerk.AppStateMachine;
using Framewerk.Managers;
using Framewerk.Popups;
using Framewerk.StrangeCore;
using MindCraft.Controller;
using MindCraft.Data;
using MindCraft.GameObjects;
using MindCraft.MapGeneration.Lookup;
using MindCraft.Model;
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

            //Framewerk core
            injectionBinder.Bind<ViewConfig>().ToValue(_viewConfig);
            injectionBinder.Bind<ICoroutineManager>().ToValue(CoroutineManager.Instance);
            injectionBinder.Bind<IAssetManager>().To<AssetManager>().ToSingleton();
            injectionBinder.Bind<IUiManager>().To<UiManager>().ToSingleton();
            injectionBinder.Bind<IAppMonitor>().ToValue(AppMonitor.Instance);
            
            injectionBinder.Bind<IInjector>().To(injectionBinder.injector);
            
            //Popups
            injectionBinder.Bind<IPopupManager>().To<PopupManager>().ToSingleton();
            injectionBinder.Bind<PopupOpenedSignal>().ToSingleton();
            injectionBinder.Bind<PopupClosedSignal>().ToSingleton();
            
            //FSM 
            injectionBinder.Bind<IAppFsm>().To<AppFsm>().ToSingleton();
            injectionBinder.Bind<AppStateEnterSignal>().ToSingleton();
            injectionBinder.Bind<AppStateExitSignal>().ToSingleton();


            injectionBinder.Bind<Chunk>().To<Chunk>();
            
            //Model
            injectionBinder.Bind<IWorldModel>().To<WorldModel>().ToSingleton();
            
            //Data
            injectionBinder.Bind<IWorldSettingsProvider>().To<WorldSettingsProvider>().ToSingleton();
            injectionBinder.Bind<IBlockDefs>().To<BlockDefs>().ToSingleton();
            injectionBinder.Bind<TextureLookup>().To<TextureLookup>().ToSingleton();
            
            //View
            injectionBinder.Bind<GameAppScreen>().To<GameAppScreen>();
            
            
            commandBinder.Bind<ContextStartSignal>().To<InitAppCommand>();
        }
    }
}