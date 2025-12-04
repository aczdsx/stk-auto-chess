/*
* Copyright (c) CookApps.
*/

using System;

namespace CookApps.NetLite.Feat.DB
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal class BsonCollectionAttribute : Attribute
    {
        public string Name { get; }

        public BsonCollectionAttribute(string name)
        {
            Name = name;
        }
    }
}
