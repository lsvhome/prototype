using System;
using System.Collections;
using System.Collections.Generic;
using Unity;

namespace desktop.common
{
    public interface IIocWrapper: IDisposable
    {
        void Init(IDictionary<Type, object> mappings);
        T Get<T>();
    }

    public class IocWrapper: IIocWrapper
    {
        IUnityContainer container = new Unity.UnityContainer();

        public void Init(IDictionary<Type,object> mappings)
        {
            foreach (var each in mappings.Keys)
            {
                container.RegisterInstance(each, mappings[each]);
            }
        }

        public T Get<T>()
        {
            return this.container.Resolve<T>();
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
                    foreach (var each in this.container.Registrations)
                    {
                        var eachObject = this.container.Resolve(each.MappedToType);
                        var eachDisposableObject = eachObject as IDisposable;
                        if (eachDisposableObject != null)
                        {
                            eachDisposableObject.Dispose();
                        }
                    }

                    // Free other state (managed objects).

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
