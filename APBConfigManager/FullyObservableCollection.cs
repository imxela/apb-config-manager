using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace APBConfigManager
{
    /// <summary>
    /// A list-like collection which raises an event when the collection is
    /// modified (item added, removed or collection refreshed) or when
    /// an item is modified (the item type must implement the
    /// <see cref="INotifyPropertyChanged"/> interface).
    /// </summary>
    public class FullyObservableCollection<T> : ObservableCollection<T> 
        where T : INotifyPropertyChanged
    {
        public new void Add(T item)
        {
            SubscribeItem(item);
            base.Add(item);
        }

        public new void Insert(Int32 index, T item)
        {
            SubscribeItem(item);
            base.Insert(index, item);
        }

        public new void InsertItem(Int32 index, T item)
        {
            SubscribeItem(item);
            base.InsertItem(index, item);
        }

        public new void Remove(T item)
        {
            UnsubscribeItem(item);
            base.Remove(item);
        }

        public new void RemoveAt(Int32 index)
        {
            UnsubscribeItem(Items[index]);
            base.RemoveAt(index);
        }

        public new void RemoveItem(Int32 index) 
        {
            UnsubscribeItem(Items[index]);
            base.RemoveItem(index);
        }

        public new void SetItem(Int32 index, T item)
        {
            UnsubscribeItem(Items[index]);
            SubscribeItem(item);
            base.SetItem(index, item);
        }

        public new void Clear()
        {
            foreach (T item in Items)
            {
                UnsubscribeItem(item);
            }

            base.Clear();
        }

        public new void ClearItems()
        {
            foreach (T item in Items)
            {
                UnsubscribeItem(item);
            }

            base.ClearItems();
        }

        private void SubscribeItem(T item)
        {
            item.PropertyChanged += Item_OnPropertyChanged;
        }

        private void UnsubscribeItem(T item)
        {
            item.PropertyChanged -= Item_OnPropertyChanged;
        }

        private void Item_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, sender, sender));
        }
    }
}
