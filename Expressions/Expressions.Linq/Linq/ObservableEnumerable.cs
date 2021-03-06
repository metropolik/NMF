﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace NMF.Expressions.Linq
{
    public abstract class ObservableEnumerable<T> : INotifyEnumerable<T>, ICollection<T>, IEnumerable<T>, INotifyCollectionChanged, IDisposable
    {
        private int attachedCount;

        [DebuggerStepThrough]
        protected void OnAddItem(T item, int index = 0)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        [DebuggerStepThrough]
        protected void OnAddItems(IEnumerable<T> items, int index = 0)
        {
            var added = items as List<T> ?? items.ToList();
            if (added.Count > 0)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, added, index));
            }
        }

        [DebuggerStepThrough]
        protected void OnRemoveItem(T item, int index = 0)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        [DebuggerStepThrough]
        protected void OnRemoveItems(IEnumerable<T> items, int index = 0)
        {
            var removed = items as List<T> ?? items.ToList();
            if (removed.Count > 0)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed, index));
            }
        }

        [DebuggerStepThrough]
        protected void OnReplaceItems(IEnumerable<T> oldItems, IEnumerable<T> newItems, int index = 0)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                newItems as List<T> ?? newItems.ToList(),
                oldItems as List<T> ?? oldItems.ToList(),
                index));
        }

        [DebuggerStepThrough]
        protected void OnCleared()
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        [DebuggerStepThrough]
        protected void OnUpdateItem(T item, T old, int index = 0)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, old, index));
        }

        [DebuggerStepThrough]
        protected void OnMoveItem(T item, int oldIndex = 0, int newIndex = 0)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
        }

        [DebuggerStepThrough]
        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public abstract IEnumerator<T> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Detach();
            }
        }

        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return true; }
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        public virtual bool Contains(T item)
        {
            IEqualityComparer<T> comparer = EqualityComparer<T>.Default;
            foreach (var element in this)
            {
		        if (comparer.Equals(element, item))
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException("array");

            foreach (var item in this)
            {
                array[arrayIndex] = item;
                arrayIndex++;
            }
        }

        public virtual int Count
        {
            get
            {
                var counter = 0;
                using (var enumerator = GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        counter++;
                    }
                }
                return counter;
            }
        }

        public void Attach()
        {
            if (attachedCount == 0)
            {
                AttachCore();
                OnCleared();
            }
            attachedCount++;
        }

        protected abstract void AttachCore();

        protected abstract void DetachCore();

        public void Detach()
        {
            if (attachedCount == 1)
            {
                DetachCore();
            }
            attachedCount--;
            if (attachedCount < 0)
            {
                throw new InvalidOperationException("Cannot detach more often than has been attached");
            }
        }

        public bool IsAttached
        {
            get { return attachedCount > 0; }
        }
    }
}
