namespace Framewerk.Popups.OkCancelWindow
{
    public class OkCancelWindowMediator : PopupMediator
    {
        [Inject] public string Message { get; set; }
        [Inject] public OkCancelWindowView View { get; set; }

        public override void OnRegister()
        {
            base.OnRegister();
            
            Init(View);
        }

        protected override void Init(PopupView popupView)
        {
            base.Init(popupView);

            View.MessageText.text = Message;
        }        
    }
}