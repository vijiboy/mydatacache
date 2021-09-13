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

    public class Monkey: INotifyPropertyChanged
    {
        //appiocache emptyImage = 
        public string Name { get; set; }
        public string Location { get; set; }
        private string imageurl = "";
        public string ImageUrl
        {
            get { return imageurl; } 
            set
            {
                if (!string.IsNullOrEmpty(value) && imageurl != value)
                {
                    imageurl = value;
                    cachedImg = appiocache.fromUri(new Uri(value)).Result;
                }
            }
        }
        
        private ImageSource imgsource;
        private appiocache cachedImg = null; // TODO 1: assign default image

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageSource ImgSource
        {
            get
            { 
                if(cachedImg!=null) // TODO 1: assign default image
                    return ImageSource.FromStream(cachedImg.createStream);
                return null;
            }            
        } 

        public override string ToString()
        {
            return Name;
        }
    }
}
