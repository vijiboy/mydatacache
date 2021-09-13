namespace AwesomeApp
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Xamarin.Forms;
    using appiocache;
    using System.IO;
    using System.Threading.Tasks;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class Monkey: INotifyPropertyChanged
    {
        
        public string Name { get; set; }
        public string Location { get; set; }
        private string imageurl = "https://upload.wikimedia.org/wikipedia/commons/b/b1/Loading_icon.gif";
        public string ImageUrl
        {
            get { return imageurl; } 
            set
            {
                if (!string.IsNullOrEmpty(value) && imageurl != value)
                {
                    imageurl = value;
                    //updateImageSourceAsync();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal async void updateImageSourceAsync()
        {
            //cachedImg = await appiocache.fromUri(new Uri(imageurl), null, BypassCache: true);
            cachedImg = await appiocache.fromUri(new Uri(imageurl), null, BypassCache: false);
            NotifyPropertyChanged("ImgSource");
        }

        private appiocache cachedImg = appiocache.fromUri(new Uri(@"https://upload.wikimedia.org/wikipedia/commons/b/b1/Loading_icon.gif"), null, BypassCache: false).Result;

        public ImageSource ImgSource
        {
            get
            {
                return ImageSource.FromStream(cachedImg.createStream);                
            }            
        } 

        public override string ToString()
        {
            return Name;
        }
    }
}
