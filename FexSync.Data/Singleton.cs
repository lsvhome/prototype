using System;
using System.Collections.Generic;
using System.Text;

namespace FexSync.Data
{
    public class Singleton<T> where T : new()
    {
        //// Special type, different for each T1 (T1 will get T type)
        private class LockObjectGenericType<T1>
        {
        }

        private static T instance;

        private static LockObjectGenericType<T> syncRoot = new LockObjectGenericType<T>();

        protected Singleton()
        {
        }

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new T();
                        }
                    }
                }

                return instance;
            }
        }
    }
}