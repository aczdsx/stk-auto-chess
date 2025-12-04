/*
* Copyright (c) CookApps.
*/

using Autofac;
using CookApps.NetLite.Feat.DB;
using Module = Autofac.Module;

namespace CookApps.NetLite.Manager.Modules
{
    /// <summary>
    /// Autofac의 Module을 상속받아 DB 관련 의존성 주입을 담당하는 클래스입니다.
    /// </summary>
    internal class DatabaseModule : Module
    {
        protected override void Load(ContainerBuilder cb)
        {
            // DB 등록
            cb.RegisterType<PlatformDB>().AsSelf().InstancePerLifetimeScope();
            cb.RegisterType<CommonDB>().AsSelf().InstancePerLifetimeScope();
        }
    }
}
