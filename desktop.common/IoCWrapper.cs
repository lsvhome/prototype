/*
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autofac;

namespace Desktop.Common
{
    public interface IIocWrapper : IDisposable
    {
        void Init(IDictionary<Type, object> mappings);

        T Resolve<T>();
    }

    public class IocWrapper : IIocWrapper
    {
        public Autofac.IContainer Container { get; set; }

        public IocWrapper()
        {
        }

        public virtual void Init(IDictionary<Type, object> mappings)
        {

            var builder = new ContainerBuilder();
            foreach (var each in mappings)
            {
                //builder.Regist erInstance(new PlatformServicesMac());
            }

            //builder.RegisterInstance<Desktop.Common.IPlatformServices>(new PlatformServicesMac());
            //builder.RegisterInstance<Net.Fex.Api.IConnection>(new Net.Fex.Api.Connection(new Uri("https://fex.net")));
            //// builder.RegisterInstance<net.fex.api.v1.IConnection>(new net.fex.api.v1.BaseConnection());

            this.Container = builder.Build();
        }

        public T Resolve<T>()
        {
            try
            {
                return this.Container.Resolve<T>();
            }
            catch (Exception ex)
            {
                ex.Process();
                throw;
            }
        }

        #region IDisposable

        private bool disposed = false;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    var allRegisteredTypes = this.Container
                        .ComponentRegistry
                        .Registrations
                        .SelectMany(registration => registration.Services)
                        .OfType<IDisposable>()
                        .ToList();

                    foreach (var each in allRegisteredTypes)
                    {
                        each.Dispose();
                    }

                    this.Container.Dispose();
                }

                // Free your own state (unmanaged objects).
                // Set large fields to null.
                this.disposed = true;
            }
        }

        ~IocWrapper()
        {
            this.Dispose(false);
        }

        #endregion IDisposable
    }
}
*/