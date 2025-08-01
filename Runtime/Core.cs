using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using IceCold.Interface;
using UnityEngine;

namespace IceCold
{
    public static class Core
    {
        public static Action Initialized;
        public static Action Deinitialized;
        public static Action willDeinitialize;
        
        private static readonly Dictionary<Type, IIceColdService> serviceInstances = new();
        
        private static bool isAttemptingToQuit = false;
        
        public static T GetService<T>() where T : class, IIceColdService
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
            
            IceColdLogger.LogError($"Service {requestedType.Name} not found");
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
                    IceColdLogger.Log($"Initializing Service: {type.Name} (Priority: {type.GetCustomAttribute<ServicePriorityAttribute>()?.Priority ?? int.MaxValue})");

                    var instance = (IIceColdService)Activator.CreateInstance(type);
                    serviceInstances[type] = instance;
                    instance.Initialize();
                }
            }
            Initialized?.Invoke();
        }

        private static bool ApplicationOnWillQuit()
        {
            Application.wantsToQuit -= ApplicationOnWillQuit;
            if (isAttemptingToQuit)
            {
                return false;
            }

            isAttemptingToQuit = true;
            
            _ = HandleQuitSequenceAsync();

            return false;
        }
        
        private static async Task HandleQuitSequenceAsync()
        {
            foreach (var service in serviceInstances.Values)
            {
                service.OnWillQuit();
            }

            var quitTasks = new List<Task<bool>>();
            foreach (var service in serviceInstances.Values)
            {
                if (service is IQuittingService quittingService)
                {
                    quitTasks.Add(quittingService.CanQuitAsync());
                }
            }

            if (quitTasks.Count == 0)
            {
                Application.Quit();
                return;
            }

            var results = await Task.WhenAll(quitTasks);

            if (results.All(canQuit => canQuit))
            {
                Application.Quit();
            }
            else
            {
                IceColdLogger.LogWarning("Application quit was aborted by a service.");
                isAttemptingToQuit = false;
                Application.wantsToQuit += ApplicationOnWillQuit;
            }
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
                IceColdLogger.Log($"Deinitializing Service: {service.GetType().Name} (Priority: {service.GetType().GetCustomAttribute<ServicePriorityAttribute>()?.Priority ?? int.MaxValue})");

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
                .Where(type => typeof(IIceColdService).IsAssignableFrom(type) && !type.IsAbstract);
        }
    }
}