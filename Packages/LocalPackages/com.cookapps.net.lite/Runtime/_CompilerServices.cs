/*
 * Copyright (c) CookApps.
 */

using System.Runtime.CompilerServices;

#if UNITY_EDITOR || TECH_ONLY
[assembly: InternalsVisibleTo("Tests.NetLite.Editor")]
[assembly: InternalsVisibleTo("Tests.NetLite.Runtime")]
[assembly: InternalsVisibleTo("TestsEditor")]
[assembly: InternalsVisibleTo("Tests")]
[assembly: InternalsVisibleTo("CookApps.NetLite.Editor")]
[assembly: InternalsVisibleTo("InternalScript")]
#endif

namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit
    {
    }
}
