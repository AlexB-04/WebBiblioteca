namespace WebBiblioteca.Mobile.Views
{
    public partial class AreaLeitorPage : ContentPage
    {
        private const string EnderecoBase = "https://webbiblioteca-alexb.runasp.net";

        public AreaLeitorPage()
        {
            InitializeComponent();
        }

        private void OnLoginClicked(object? sender, EventArgs e)
        {
            LeitorWebView.Source = EnderecoBase + "/Account/Login";
        }

        private void OnHistoricoClicked(object? sender, EventArgs e)
        {
            LeitorWebView.Source = EnderecoBase + "/Historico";
        }
    }
}