using System;
using System.Collections;
using System.Collections.Generic;
using Autofac;
using System.Linq;

namespace desktop.common
{
    public interface IIocWrapper: IDisposable
    {
        void Init(IDictionary<Type, object> mappings);
        T Get<T>();
    }

    public class IocWrapper: IIocWrapper
    {
        public Autofac.IContainer container;

        public IocWrapper()
        {
        }

        public virtual void Init(IDictionary<Type,object> mappings)
        {
            /*
                try
                {
                    var builder = new ContainerBuilder();
                    foreach (var each in mappings.Keys)
                    {
                        builder.RegisterInstance<IConn>(mappings[each]);
                        builder.RegisterType(mappings[each].GetType()).As(each);
                    }

                this.container = builder.Build();

    #if DEBUG
                    try
                    {
                        foreach (var each in mappings.Keys)
                        {
                            this.container.Resolve(each);
                        }

                    }
                    catch (Exception)
                    {

                        throw;
                    }

    #endif

                }
                catch (Exception ex)
                {
                    ex.Process();
                    throw;
                }
            */
        }


        public T Get<T>()
        {
            try
            {
                return this.container.Resolve<T>();
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    var allRegisteredTypes =
                        from r in this.container.ComponentRegistry.Registrations
                        from s in r.Services
                        where  s != null && s is IDisposable
                        select s as IDisposable;

                    foreach (var each in allRegisteredTypes.ToList())
                    {
                        each.Dispose();
                        }

                    this.container.Dispose();
                }

                // Free your own state (unmanaged objects).
                // Set large fields to null.
                disposed = true;
            }
        }

        ~IocWrapper()
        {
            Dispose(false);
        }

        #endregion IDisposable
    }
}
