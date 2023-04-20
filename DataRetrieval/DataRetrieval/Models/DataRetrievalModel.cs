using System.ComponentModel;

namespace DataRetrieval.Models
{
    public class DataRetrievalModel : INotifyPropertyChanged
    {
        private string name;
        private string value;

        public event PropertyChangedEventHandler PropertyChanged = (o, e) => { };

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Name"));
            }
        }

        public string Value
        {
            get { return value; }
            set
            {
                this.value = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Value"));
            }
        }
    }
}
