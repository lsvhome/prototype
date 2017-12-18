using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace FexSync.Data
{
    public class ThreadSafeListWithLock<T> : IList<T>
    {
        private readonly object lockList = new object();

        private List<T> internalList;

        public ThreadSafeListWithLock()
        {
            this.internalList = new List<T>();
        }

        // Other Elements of IList implementation
        public IEnumerator<T> GetEnumerator()
        {
            return this.Clone().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.Clone().GetEnumerator();
        }

        public List<T> Clone()
        {
            ThreadLocal<List<T>> threadClonedList = new ThreadLocal<List<T>>(() =>
            {
                return new List<T>();
            });

            lock (this.lockList)
            {
                this.internalList.ForEach(element => { threadClonedList.Value.Add(element); });
            }

            return threadClonedList.Value;
        }

        public void Add(T item)
        {
            lock (this.lockList)
            {
                this.internalList.Add(item);
            }
        }

        public bool Remove(T item)
        {
            bool isRemoved;

            lock (this.lockList)
            {
                isRemoved = this.internalList.Remove(item);
            }

            return isRemoved;
        }

        public void Clear()
        {
            lock (this.lockList)
            {
                this.internalList.Clear();
            }
        }

        public bool Contains(T item)
        {
            bool containsItem;

            lock (this.lockList)
            {
                containsItem = this.internalList.Contains(item);
            }

            return containsItem;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (this.lockList)
            {
                this.internalList.CopyTo(array, arrayIndex);
            }
        }

        public int Count
        {
            get
            {
                int count;

                lock (this.lockList)
                {
                    count = this.internalList.Count;
                }

                return count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public int IndexOf(T item)
        {
            int itemIndex;

            lock (this.lockList)
            {
                itemIndex = this.internalList.IndexOf(item);
            }

            return itemIndex;
        }

        public void Insert(int index, T item)
        {
            lock (this.lockList)
            {
                this.internalList.Insert(index, item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (this.lockList)
            {
                this.internalList.RemoveAt(index);
            }
        }

        public void RemoveAll(Predicate<T> match)
        {
            lock (this.lockList)
            {
                this.internalList.RemoveAll(match);
            }
        }

        public T this[int index]
        {
            get
            {
                lock (this.lockList)
                {
                    return this.internalList[index];
                }
            }

            set
            {
                lock (this.lockList)
                {
                    this.internalList[index] = value;
                }
            }
        }

        protected virtual void LockInternalListAndCommand(Action<IList<T>> action)
        {
            lock (this.lockList)
            {
                action(this.internalList);
            }
        }

        protected virtual T LockInternalListAndGet(Func<IList<T>, T> func)
        {
            lock (this.lockList)
            {
                return func(this.internalList);
            }
        }

        protected virtual TObject LockInternalListAndQuery<TObject>(Func<IList<T>, TObject> query)
        {
            lock (this.lockList)
            {
                return query(this.internalList);
            }
        }
    }
}
