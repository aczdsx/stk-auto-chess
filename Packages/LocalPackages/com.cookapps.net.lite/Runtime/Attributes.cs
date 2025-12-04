/*
* Copyright (c) CookApps.
*/

using System;

namespace CookApps.NetLite
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GrpcServiceAttribute : Attribute
    {
        public Type ServiceType { get; }
        public GrpcServiceAttribute(Type serviceType)
        {
            ServiceType = serviceType;
        }
    }
}
