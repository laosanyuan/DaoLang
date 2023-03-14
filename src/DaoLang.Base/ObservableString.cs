using System.ComponentModel;

namespace DaoLang.Base
{
    public class ObservableString : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableString(string value)
        {
            this.Value = value;
        }

        private string _value;
        public string Value
        {
            get => this._value;
            set
            {
                if (this._value == value)
                {
                    return;
                }
                this._value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
    }
}
