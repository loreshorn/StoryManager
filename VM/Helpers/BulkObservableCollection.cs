using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace StoryManager.VM.Helpers
{
    /// <summary>An <see cref="ObservableCollection{T}"/> that supports adding many items with a single <see cref="NotifyCollectionChangedAction.Reset"/>
    /// notification at the end, instead of one notification per item. This avoids O(N^2) work in subscribed <see cref="System.ComponentModel.ICollectionView"/>s
    /// and other listeners when bulk-loading large numbers of items.</summary>
    public class BulkObservableCollection<T> : ObservableCollection<T>
    {
        private bool _SuppressNotifications;

        public void AddRange(IEnumerable<T> Items)
        {
            if (Items == null)
                return;

            _SuppressNotifications = true;
            try
            {
                foreach (T Item in Items)
                    Add(Item);
            }
            finally { _SuppressNotifications = false; }

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_SuppressNotifications)
                base.OnCollectionChanged(e);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!_SuppressNotifications)
                base.OnPropertyChanged(e);
        }
    }
}
