using System;
using System.Collections.Generic;
using strange.extensions.injector.impl;
using strange.framework.api;
using UnityEngine;

namespace Framewerk.StrangeCore
{
    /// <summary>
    /// Interface used to mark objects which needs Destroy method to be called
    /// when they got unbinded by calling injectionBinder.Unbind() in context.
    /// Works only with Singleton bindings as binder does not have any reference to factory type bindings.
    /// To use this functionality you need to use DestroyingBinder instead of Default CrossContextInjectionBinder
    /// or you can use premade contexts in Framewerk.StrangeCore that already utilize DestroyingBinder.
    /// </summary>
    public interface IDestroyable
    {
        void Destroy();
    }
    
    public class DestroyingBinder : CrossContextInjectionBinder
    {
        public override void Unbind(object key, object name)
        {
            if (bindings.ContainsKey(key))
            {
                Dictionary<object, IBinding> dict = bindings[key];
                object bindingName = name ?? BindingConst.NULLOID;
                if (dict.ContainsKey(bindingName))
                {
                    var destroyable = dict[bindingName].value as IDestroyable; 
                    destroyable?.Destroy();
                    dict.Remove(bindingName);
                }
            }
        }
    }
}