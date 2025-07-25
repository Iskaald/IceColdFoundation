using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IceColdCore.Interface;
using UnityEngine;

namespace IceColdCore
{
    public static class Core
    {
        public static Action Initialized;
        public static Action Deinitialized;
        public static Action willDeinitialize;
        
        private static readonly Dictionary<Type, ICoreService> serviceInstances = new();
        
        public static T GetService<T>() where T : class, ICoreService
        {
            var requestedType = typeof(T);
            
            if (serviceInstances.TryGetValue(requestedType, out var exactService))
            {
                return (T) exactService;
            }

            foreach (var kvp in serviceInstances)
            {
                if (requestedType.IsAssignableFrom(kvp.Key))
                {
                    return (T)kvp.Value;
                }
            }
            
#if ICECOLD_DEBUG
            CoreLogger.LogError($"Service {requestedType.Name} not found");
#else
            Debug.LogError($"Service {requestedType.Name} not found");
#endif
            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeAllServices()
        {
            Application.quitting += DeinitializeAllServices;
            Application.wantsToQuit += ApplicationOnWillQuit;

            var serviceTypes = GetAllCoreServiceTypes();
            
            var sortedServiceTypes = serviceTypes.OrderBy(type =>
            {
                var priorityAttr = type.GetCustomAttribute<ServicePriorityAttribute>();
                return priorityAttr?.Priority ?? int.MaxValue;
            });

            foreach (var type in sortedServiceTypes)
            {
                if (!serviceInstances.ContainsKey(type))
                {
#if ICECOLD_DEBUG
                    CoreLogger.Log($"Initializing Service: {type.Name} (Priority: {type.GetCustomAttribute<ServicePriorityAttribute>()?.Priority ?? int.MaxValue})");
#else
                    Debug.Log($"Initializing Service: {type.Name} (Priority: {type.GetCustomAttribute<ServicePriorityAttribute>()?.Priority ?? int.MaxValue})");
#endif
                    var instance = (ICoreService)Activator.CreateInstance(type);
                    serviceInstances[type] = instance;
                    instance.Initialize();
                }
            }
            Initialized?.Invoke();
        }

        private static bool ApplicationOnWillQuit()
        {
            Application.wantsToQuit -= ApplicationOnWillQuit;
            willDeinitialize?.Invoke();
            
            var canQuit = true;
            foreach (var service in serviceInstances.Values)
            {
                if (canQuit) canQuit = service.OnWillQuit();
            }

            return canQuit;
        }

        private static void DeinitializeAllServices()
        {
            Application.quitting -= DeinitializeAllServices;
            
            var sortedServices = serviceInstances.Values.OrderByDescending(service =>
            {
                var priorityAttr = service.GetType().GetCustomAttribute<ServicePriorityAttribute>();
                return priorityAttr?.Priority ?? int.MaxValue;
            });
            
            foreach (var service in sortedServices)
            {
#if ICECOLD_DEBUG
                CoreLogger.Log($"Deinitializing Service: {service.GetType().Name} (Priority: {service.GetType().GetCustomAttribute<ServicePriorityAttribute>()?.Priority ?? int.MaxValue})");
#else
                Debug.Log($"Deinitializing Service: {service.GetType().Name} (Priority: {service.GetType().GetCustomAttribute<ServicePriorityAttribute>()?.Priority ?? int.MaxValue})");
#endif
                service.Deinitialize();
            }
            
            serviceInstances.Clear();
            Deinitialized?.Invoke();
        }

        private static IEnumerable<Type> GetAllCoreServiceTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        return e.Types.Where(t => t != null);
                    }
                })
                .Where(type => typeof(ICoreService).IsAssignableFrom(type) && !type.IsAbstract);
        }
    }
}