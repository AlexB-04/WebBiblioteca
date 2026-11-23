using WebBiblioteca.Mobile.Views;

namespace WebBiblioteca.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(
                nameof(LivroDetalhesPage),
                typeof(LivroDetalhesPage));
        }
    }
}