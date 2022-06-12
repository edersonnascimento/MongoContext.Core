using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MongoContext.Core
{

    /// <summary>
    /// Created by: Éderson Curto do Nascimento
    /// Created at: 2017-01-28
    /// E-mail: ederson.nascimento@outlook.com
    /// </summary>
    public class MgDbMap : IMgDbMap
    {
        private readonly static ConcurrentDictionary<Type, MgDbMap> __classMaps = new ConcurrentDictionary<Type, MgDbMap>();

        public Type ClassType { get; private set; }
        public string Collection { get; set; }

        public MgDbMap(Type type) => ClassType = type;

        internal static void Register(MgDbMap classMap)
        {
            if (classMap != null && !__classMaps.ContainsKey(classMap.ClassType))
                __classMaps.AddOrUpdate(classMap.ClassType, classMap, (k, v) => v);
        }
        public static IEnumerable<MgDbMap> GetRegisteredMaps() => __classMaps.Values.ToList();
        public static MgDbMap GetClassMap(Type type)
        {
            if (__classMaps.ContainsKey(type))
                return __classMaps[type];

            return null;
        }
    }
    public interface IMgDbMap
    {
        Type ClassType { get; }
        string Collection { get; set; }
    }
    
    [AttributeUsage(AttributeTargets.Class)]
    public class CollectionAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
