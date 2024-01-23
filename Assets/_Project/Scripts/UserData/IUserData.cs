using System;
using Com.Cookapps.Sampleteambattle;

public interface IUserData
{
    void Initialize(string data);
}

public class UserDataInitializeInfoAttribute : Attribute
{
    public DataCategory DataCategory { get; }

    public UserDataInitializeInfoAttribute(DataCategory dataCategory)
    {
        DataCategory = dataCategory;
    }
}
