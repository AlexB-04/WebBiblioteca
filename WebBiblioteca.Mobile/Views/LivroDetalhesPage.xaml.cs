
using WebBiblioteca.Mobile.Models;

namespace WebBiblioteca.Mobile.Views
{
    public partial class LivroDetalhesPage : ContentPage, IQueryAttributable
    {
        public LivroDetalhesPage()
        {
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("livro", out var valor)
                && valor is Livro livro)
            {
                BindingContext = livro;
            }
        }
    }
}