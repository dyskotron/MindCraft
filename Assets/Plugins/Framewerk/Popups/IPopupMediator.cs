using strange.extensions.signal.impl;

namespace Framewerk.Popups
{
    public interface IPopupMediator
    {
        void Close();    

        Signal<IPopupMediator> PopupClosedSignal { get; }
    }
}