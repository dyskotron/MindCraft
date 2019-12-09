using System;
using System.Collections.Generic;
using Framewerk.Managers;
using strange.extensions.injector.api;
using strange.extensions.injector.impl;
using strange.extensions.promise.api;
using strange.extensions.promise.impl;
using UnityEngine;

namespace Framewerk.Popups
{
    public class PopupButtonSetting
    {
        public string optionText;
        public Action clickHandler;
        public IPromise clickPromise;
        public bool closesPopup = true;
    }

    public interface IPopupManager
    {
        void Init(string resourcePath, Transform popupParent);
        
        void CloseAllPopups();

        T InstantiatePopup<T>(params object[] popupMediatorInjects) where T : IPopupView;
        
        T InstantiatePopup<T>(params PopupButtonSetting[] popupOptions) where T : IPopupView;
        
        T InstantiatePopup<T>(string text, params PopupButtonSetting[] popupOptions) where T : IPopupView;
        
        T InstantiatePopup<T>(string caption, string text, params PopupButtonSetting[] popupOptions) where T : IPopupView;
    }

    public class PopupManager : IPopupManager
    {
        [Inject] public PopupOpenedSignal PopupOpenedSignal { get; set; }
        [Inject] public IUiManager UiManager { get; set; }
        [Inject] public IInjectionBinder InjectionBinder { get; set; }

        private List<IPopupMediator> _popups = new List<IPopupMediator>();

        private string _resourcePath;
        private Transform _popupParent;
        
        public void Init(string resourcePath, Transform popupParent)
        {
            PopupOpenedSignal.AddListener(OnPopupOpenedHandler);

            _resourcePath = resourcePath;
            _popupParent = popupParent;
        }
        
        public void CloseAllPopups()
        {
            foreach (var popup in _popups)
            {    
                popup.PopupClosedSignal.RemoveListener(OnPopupClosed);
                popup.Close();
            }
            
            _popups.Clear();
        }

        public T InstantiatePopup<T>(params object[] popupMediatorInjects) where T : IPopupView
        {
            return UiManager.InstantiateView<T>(_resourcePath, _popupParent, popupMediatorInjects);
        }

        public T InstantiatePopup<T>(params PopupButtonSetting[] popupOptions) where T : IPopupView
        {
            return UiManager.InstantiateView<T>(_resourcePath, _popupParent, new List<PopupButtonSetting>(popupOptions));
        }

        public T InstantiatePopup<T>(string text, params PopupButtonSetting[] popupOptions) where T : IPopupView
        {
            return UiManager.InstantiateView<T>(_resourcePath, _popupParent, text, new List<PopupButtonSetting>(popupOptions));
        }

        public T InstantiatePopup<T>(string caption, string text, params PopupButtonSetting[] popupOptions) where T : IPopupView
        {
            return UiManager.InstantiateView<T>(_resourcePath, _popupParent, caption, text, new List<PopupButtonSetting>(popupOptions));
        }
        
        private void OnPopupOpenedHandler(IPopupMediator popup)
        {
            _popups.Add(popup);
            popup.PopupClosedSignal.AddListener(OnPopupClosed);
        }

        private void OnPopupClosed(IPopupMediator popup)
        {
            popup.PopupClosedSignal.RemoveListener(OnPopupClosed);
            _popups.Remove(popup);
        }
    }
}