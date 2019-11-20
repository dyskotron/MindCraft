using Framewerk.UI.List;
using strange.extensions.signal.impl;

namespace Framewerk.Popups
{
    public class PopupListMediator<TView, TData> : ListMediator<TView, TData>, IPopupMediator where TView : ListView
        where TData : class, IListItemDataProvider
    {
        public Signal<IPopupMediator> PopupClosedSignal { get; } = new Signal<IPopupMediator>();
        
        [Inject] public PopupOpenedSignal PopupOpenedSignal { get; set; }

        public override void OnRegister()
        {
            base.OnRegister();
            
            PopupOpenedSignal.Dispatch(this);
        }
        
        public override void OnRemove()
        {
            PopupClosedSignal.Dispatch(this);
            
            base.OnRemove();
        }

        public void Close()
        {
            PopupClosedSignal.Dispatch(this);
            
            Destroy(gameObject);
        }
    }
}