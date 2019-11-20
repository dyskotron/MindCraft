namespace Framewerk.Popups
{
    public class MessageBoxMediator : PopupMediator
    {
        [Inject] public string Message { get; set; }
        [Inject] public MessageBoxView View { get; set; }

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