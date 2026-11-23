
using System.ComponentModel;

namespace WebBiblioteca.Mobile.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(string nomePropriedade)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nomePropriedade));
        }
    }
}