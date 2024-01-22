using System;
using System.Linq;

public static class InterfaceHelper
{
    public static Type[] GetAllImplementations<T>()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(T).IsAssignableFrom(p) && p.IsClass)
            .ToArray();
    }
}
