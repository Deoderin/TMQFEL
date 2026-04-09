using System;
using System.Collections.Generic;

namespace TMQFEL.Core
{
    public sealed class SystemsService
    {
        private static SystemsService _instance;

        private readonly Dictionary<Type, object> _systems = new();

        public static SystemsService Instance => _instance ??= new SystemsService();
        

        public void Register<TSystem>(TSystem system) where TSystem : class
        {
            _systems[typeof(TSystem)] = system;
        }

        public TSystem Get<TSystem>() where TSystem : class
        {
            return (TSystem)_systems[typeof(TSystem)];
        }
    }
}
