
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AnyUnit.Compat.PortableV4
{

    public static class Compat
    {

#if PORTABLE

        public static Type GetTypeInfo(this Type type)
        {
            return type;
        }

        public static MethodInfo GetMethodInfo(this Delegate del)
        {
            return del.Method;
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this Assembly assembly){
            return assembly.GetCustomAttributes(true).OfType<Attribute>();
        }


#endif
    }
}
    namespace AnyUnit.Compat.NetStandardV1
{
 public static class Compat
    {
#if NETSTANDARD1_0
        public static bool IsInstanceOfType(this Type type, Object obj){
            if(obj == null){
                return false;
            }
            var objTypeInfo = obj.GetType().GetTypeInfo();
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsAssignableFrom(objTypeInfo);
        }

        public static IEnumerable<Type> GetExportedTypes(this Assembly assembly)
        {
            return assembly.ExportedTypes;
        }

        public static IEnumerable<Type> GetInterfaces(this TypeInfo typeInfo){
            return typeInfo.ImplementedInterfaces;
        }

        public static Type[] GetGenericArguments(this TypeInfo typeInfo){
            return typeInfo.GenericTypeArguments;
        }

         public static PropertyInfo GetProperty(this Type target, string name, BindingFlags flags){
            
          var methods = target.GetProperties(flags);
          if(methods == null){
              return null;
          }
          methods = methods.Where(m=>m.Name == name);
          switch (methods.Count()) {
            case 0:
            case 1:
                return methods.FirstOrDefault();
            default:
                throw new AmbiguousMatchException(String.Format("Unlikely but there is more than one {0} property on {1}", name, target));
          }
           
        }

         public static IEnumerable<PropertyInfo> GetProperties(this Type target, BindingFlags flags){
            var isDefault = flags == BindingFlags.Default;
            var isStatic = (flags & BindingFlags.Static) == BindingFlags.Static;
            var isInstance = (flags & BindingFlags.Instance) == BindingFlags.Instance;
            var isPublic = (flags & BindingFlags.Public) == BindingFlags.Public;
            var isNonPublic = (flags & BindingFlags.NonPublic) == BindingFlags.NonPublic;
            var isFlatten = (flags & BindingFlags.FlattenHierarchy) == BindingFlags.FlattenHierarchy;
           

            var methodSet = new List<PropertyInfo>();
            var typeInfo = target.GetTypeInfo();
            if(isFlatten || isInstance ){
                var baseType = typeInfo.BaseType;
                if(baseType != null){
                    var props = baseType.GetTypeInfo().DeclaredProperties;
                    methodSet.AddRange(props.Where(m=>(m.GetMethod.IsStatic && isFlatten)
                                                   || !m.GetMethod.IsStatic && isInstance));
                }
            }
            var info = target.GetTypeInfo();
            methodSet.AddRange(info.DeclaredProperties);

            if(isDefault){
                return null;
            }

            return methodSet.Where(m=> 
                                                  (isStatic && m.GetMethod.IsStatic)
                                                ||
                                                  (isInstance && !m.GetMethod.IsStatic) 
                                                || 
                                                  (isPublic && m.GetMethod.IsPublic) 
                                                || 
                                                  (isNonPublic && !m.GetMethod.IsPublic) 
                                             );
         }

        public static MethodInfo GetMethod(this Type target, string name, Type[] paramTypes){
          var flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;  
          var methods = target.GetMethods(flags);
          if(methods == null){
              return null;
          }
          methods = methods.Where(m=>m.Name == name).Where(m=>Enumerable.SequenceEqual(m.GetParameters().Select(p=>p.ParameterType), paramTypes));
          switch (methods.Count()) {
            case 0:
            case 1:
                return methods.FirstOrDefault();
            default:
                throw new AmbiguousMatchException(String.Format("More than one {0} method on {1}", name, target));
          }
        }

        public static MethodInfo GetMethod(this Type target, string name, BindingFlags flags){
          var methods = target.GetMethods(flags);
          if(methods == null){
              return null;
          }
          methods = methods.Where(m=>m.Name == name);
          switch (methods.Count()) {
            case 0:
            case 1:
                return methods.FirstOrDefault();
            default:
                throw new AmbiguousMatchException(String.Format("More than one {0} method on {1}", name, target));
          }
        }


        public static IEnumerable<MethodInfo> GetMethods(this Type target, BindingFlags flags){
            var isDefault = flags == BindingFlags.Default;
            var isStatic = (flags & BindingFlags.Static) == BindingFlags.Static;
            var isInstance = (flags & BindingFlags.Instance) == BindingFlags.Instance;
            var isPublic = (flags & BindingFlags.Public) == BindingFlags.Public;
            var isNonPublic = (flags & BindingFlags.NonPublic) == BindingFlags.NonPublic;
            var isFlatten = (flags & BindingFlags.FlattenHierarchy) == BindingFlags.FlattenHierarchy;

            var methodSet = new List<MethodInfo>();
            var typeInfo = target.GetTypeInfo();
            if(isFlatten || isInstance){
                var baseType = typeInfo.BaseType;
                if(baseType != null){
                    var methods = baseType.GetTypeInfo().DeclaredMethods;
                    methodSet.AddRange(methods.Where(m=>(m.IsStatic && isFlatten)
                                                     || !m.IsStatic && isInstance));
                }
            }
            methodSet.AddRange(target.GetTypeInfo().DeclaredMethods);

            if(isDefault){
                return null;
            }

            return methodSet.Where(m=> 
                                                  (isStatic && m.IsStatic)
                                                ||
                                                  (isInstance && !m.IsStatic) 
                                                || 
                                                  (isPublic && m.IsPublic) 
                                                || 
                                                  (isNonPublic && !m.IsPublic) 
                                             );
        }
#endif

    }

#if NETSTANDARD1_0

    public static class ThreadPool{
        public static void QueueUserWorkItem(Action<object> callback, object state){
            Task.Run(()=> callback(state));
        }
    }

    [Flags]
    public enum BindingFlags{      
        Default = 0,
      //  IgnoreCase = 1,
      //  DeclaredOnly = 2,
        Instance = 4,
        Static = 8,
        Public = 16,
        NonPublic = 32,
        FlattenHierarchy = 64,
        //InvokeMethod = 256,
        //CreateInstance = 512,
        //GetField = 1024,
        //SetField = 2048,
        //GetProperty = 4096,
        //SetProperty = 8192,
      //  PutDispProperty = 16384,
      //  PutRefDispProperty = 32768,
      //  ExactBinding = 65536,
      //  SuppressChangeType = 131072,
       // OptionalParamBinding = 262144,
       // IgnoreReturn = 16777216
    }
#endif

}