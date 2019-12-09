using System;
using Plugins.Framewerk;
using strange.extensions.injector.api;
using strange.extensions.mediation.api;
using UnityEngine;

namespace Framewerk.Managers
{
    public interface IUiManager
    {
        GameObject InstantiateView(GameObject viewPrefab, Transform parent = null, params object[] mediatorInjects);

        GameObject InstantiateViewExplicitType(GameObject viewPrefab, Transform parent = null, params Tuple<object, Type>[] mediatorInjectsWithType);
        
        GameObject InstantiateView(string path, Transform parent = null, params object[] mediatorInjects);

        GameObject InstantiateViewExplicitType(string path, Transform parent = null, params Tuple<object, Type>[] mediatorInjectsWithTypes);        
        
        T InstantiateView<T>(string path, Transform parent = null, params object[] mediatorInjects) where T : IView;

        T InstantiateViewExplicitType<T>(string path = "", Transform parent = null, params Tuple<object, Type>[] mediatorInjectsWithTypes) where T : IView;        

        string GetViewName(Type type);
    }

    public class UiManager : IUiManager
    {
        public const string UI_PREFABS_ROOT = "UI/";
        public const string VIEW_SUFFIX = "View";

        [Inject]
        public IAssetManager AssetManager { get; set; }
        
        [Inject]
        public IInjectionBinder InjectionBinder { get; set; }

        [Inject]
        public ViewConfig ViewConfig
        {
            set
            {
                _uiParent = value.UiDefault;
            }
        }

        private Transform _uiParent;

        /// <summary>
        /// Instantiate UI by path.
        /// </summary>
        /// <param name="path">Path to UI prefab from UI root</param>
        /// <param name="parent">Where UI prefab should be instantiated</param>
        /// <param name="mediatorInjects">Objects injected into mediator, types get infered from object</param>
        /// <returns></returns>
        public GameObject InstantiateView(string path, Transform parent = null, params object[] mediatorInjects)
        {
            if (parent == null)
                parent = _uiParent;

            BindParams(mediatorInjects);

            GameObject uiObj = AssetManager.GetAsset<GameObject>(UI_PREFABS_ROOT + path, parent);
            
            UnbindParams(mediatorInjects);
            
            return uiObj;
        }
        
        /// <summary>
        /// Instantiate UI by path.
        /// </summary>
        /// <param name="path">Path to UI prefab from UI root</param>
        /// <param name="parent">Where UI prefab should be instantiated</param>
        /// <param name="mediatorInjects">Objects injected into mediator, types specified explicitly.</param>
        /// <returns></returns>
        public GameObject InstantiateViewExplicitType(string path, Transform parent = null, params Tuple<object, Type>[] mediatorInjectsWithTypes)
        {
            if (parent == null)
                parent = _uiParent;

            BindParams(mediatorInjectsWithTypes);

            GameObject uiObj = AssetManager.GetAsset<GameObject>(UI_PREFABS_ROOT + path, parent);
            
            UnbindParams(mediatorInjectsWithTypes);
            
            return uiObj;
        }        

        /// <summary>
        /// Instantiate UI, finds path by View Class.
        /// </summary>
        /// <param name="path">Optional path that can be added between UI root and prefab name
        /// UI_ROOT/[ADDED CUSTOM PATH/]prefabname</param>
        /// <param name="parent">Where UI prefab should be instantiated</param>
        /// <returns></returns>
        public T InstantiateView<T>(string path = "", Transform parent = null, params object[] mediatorInjects) where T : IView
        {
            if (parent == null)
                parent = _uiParent;

            var uiObj = InstantiateView(GetViewPath(typeof(T), path), parent, mediatorInjects);
            var component = uiObj.GetComponent<T>();

            if (component == null)
                Debug.LogErrorFormat("UIManager.InstantiateView There is no {0} script attached on {1} Prefab", typeof(T), uiObj);

            return component;
        }
        
        /// <summary>
        /// Instantiate UI, finds path by View Class.
        /// </summary>
        /// <param name="path">Optional path that can be added between UI root and prefab name
        /// UI_ROOT/[ADDED CUSTOM PATH/]prefabname</param>
        /// <param name="parent">Where UI prefab should be instantiated</param>
        /// <returns></returns>
        public T InstantiateViewExplicitType<T>(string path = "", Transform parent = null, params Tuple<object, Type>[] mediatorInjectsWithTypes) where T : IView
        {
            if (parent == null)
                parent = _uiParent;

            var uiObj = InstantiateViewExplicitType(GetViewPath(typeof(T), path), parent, mediatorInjectsWithTypes);
            var component = uiObj.GetComponent<T>();

            if (component == null)
                Debug.LogErrorFormat("UIManager.InstantiateView There is no {0} script attached on {1} Prefab", typeof(T), uiObj);

            return component;
        }        
        
        public GameObject InstantiateView(GameObject viewPrefab, Transform parent = null, params object[] mediatorInjects)
        {
            if (viewPrefab == null)
                Debug.LogErrorFormat("UIManager.InstantiateView InstantiateView viewPrefab is null");

            if (parent == null)
                parent = _uiParent;

            BindParams(mediatorInjects);
            
            GameObject view = GameObject.Instantiate(viewPrefab, parent, false);
            
            UnbindParams(mediatorInjects);

            return view;
        }
        
        public GameObject InstantiateViewExplicitType(GameObject viewPrefab, Transform parent = null, params Tuple<object, Type>[] mediatorInjectsWithType)
        {
            if (viewPrefab == null)
                Debug.LogErrorFormat("UIManager.InstantiateView InstantiateView viewPrefab is null");

            if (parent == null)
                parent = _uiParent;

            BindParams(mediatorInjectsWithType);
            
            GameObject view = GameObject.Instantiate(viewPrefab, parent, false);
            
            UnbindParams(mediatorInjectsWithType);

            return view;
        }        
        
        public string GetViewName(Type type)
        {
            var name = type.Name;
            return name.Substring(0, name.Length - VIEW_SUFFIX.Length);
        }

        protected virtual string GetViewPath(Type type, string customPath)
        {
            return customPath + GetViewName(type);
        }
        
        private void BindParams(params Tuple<object, Type>[] bindparamsWithType)
        {
            if (bindparamsWithType != null)
            {
                foreach (var param in bindparamsWithType)
                {
                    InjectionBinder.Bind(param.Item2).ToValue(param.Item1);
                }
            }
        }

        private void UnbindParams(params Tuple<object, Type>[] bindparamsWithType)
        {
            if (bindparamsWithType != null)
            {
                foreach (var param in bindparamsWithType)
                {
                    InjectionBinder.Unbind(param.Item2);
                }
            }            
        }

        private void BindParams(params object[] bindparams)
        {
            if (bindparams != null)
            {
                foreach (var param in bindparams)
                {
                    InjectionBinder.Bind(param.GetType()).ToValue(param);
                }
            }
        }

        private void UnbindParams(params object[] bindparams)
        {
            if (bindparams != null)
            {
                foreach (var param in bindparams)
                {
                    InjectionBinder.Unbind(param.GetType());
                }
            }
        }
    }
}