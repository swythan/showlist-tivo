using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Tivo.Connect;
using Tivo.Connect.Entities;

namespace TivoAhoy.Phone.ViewModels
{
    public class VirtualizedShowList : PropertyChangedBase, IList
    {
        private readonly TivoConnection connection;
        private readonly IList<Channel> channels;
        private readonly DateTime time;
        
        private Dictionary<int, OfferViewModel> showCache;
 
        public VirtualizedShowList(
            TivoConnection connection, 
            IList<Channel> channels, 
            DateTime time)
        {
            this.connection = connection;
            this.channels = channels;
            this.time = time;

            this.showCache = new Dictionary<int, OfferViewModel>();

            var collectionChanged = channels as INotifyCollectionChanged;
            if (collectionChanged != null)
            {
                collectionChanged.CollectionChanged += OnCollectionChanged;
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.NotifyOfPropertyChange(() => this.Count);
        }
        
        public int Count
        {
            get { return this.channels.Count; }
        }

        public object this[int index]
        {
            get
            {
                if (!this.showCache.ContainsKey(index))
                {
                    var newModel = new OfferViewModel(this.channels[index]);

                    this.showCache[index] = newModel;

                    this.UpdateShowAsync(index);
                }

                return this.showCache[index];
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        private async Task UpdateShowAsync(int index)
        {
            var model = this.showCache[index];

            try
            {
                var rows = await this.connection.GetGridShowsAsync(
                    this.time, 
                    this.time, 
                    model.Channel.ChannelNumber, 
                    1, 
                    0);

                if (rows.Count > 0)
                {
                    model.Offer = rows[0].Offers.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
            }
        }
        
        public int IndexOf(object value)
        {
            var typedValue = value as OfferViewModel;

            if (typedValue == null ||
                !this.showCache.ContainsValue(typedValue))
            {
                return -1;
            }

            return this.showCache.First(x => x.Value == typedValue).Key;
        }

        #region Unimplemented IList members

        int IList.Add(object value)
        {
            throw new NotImplementedException();
        }

        void IList.Clear()
        {
            throw new NotImplementedException();
        }

        bool IList.Contains(object value)
        {
            throw new NotImplementedException();
        }

        int IList.IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        void IList.Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        bool IList.IsFixedSize
        {
            get { throw new NotImplementedException(); }
        }

        bool IList.IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        void IList.Remove(object value)
        {
            throw new NotImplementedException();
        }

        void IList.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        bool ICollection.IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }

        object ICollection.SyncRoot
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
