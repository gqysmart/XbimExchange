﻿using System.ComponentModel;

namespace XplorerPlugin.CobieExport.UI
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CheckedListItem<T> : INotifyPropertyChanged
    {
        /// <summary>
        /// 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isChecked;
        private T _item;

        /// <summary>
        /// 
        /// </summary>
        public CheckedListItem()
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isChecked"></param>
        public CheckedListItem(T item, bool isChecked = false)
        {
            _item = item;
            _isChecked = isChecked;
        }

        /// <summary>
        /// 
        /// </summary>
        public T Item
        {
            get { return _item; }
            set
            {
                _item = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Item"));
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                _isChecked = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("IsChecked"));
            }
        }
    }
}
