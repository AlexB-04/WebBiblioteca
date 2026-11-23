using WebBiblioteca.Mobile.Models;
using WebBiblioteca.Mobile.Services;
using WebBiblioteca.Mobile.ViewModels;

namespace WebBiblioteca.Mobile.Views
{
    public partial class LivrosPage : ContentPage
    {
        private readonly LivrosViewModel _viewModel;

        public LivrosPage()
        {
            InitializeComponent();

            string enderecoBase = "https://webbiblioteca-alexb.runasp.net/";

            ILivroService livroService = new APILivroService(new HttpClient(), enderecoBase);

            _viewModel = new LivrosViewModel(livroService);

            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await _viewModel.CarregarLivrosAsync();
        }

        private async void OnAtualizarClicked(object? sender, EventArgs e)
        {
            await _viewModel.CarregarLivrosAsync();
        }


        private async void OnLivroSelecionado(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is not Livro livro)
            {
                return;
            }

            if (sender is CollectionView collectionView)
            {
                collectionView.SelectedItem = null;
            }

            await Shell.Current.GoToAsync(nameof(LivroDetalhesPage),
                new Dictionary<string, object>
                { { "livro", livro } });
        }
    }
}