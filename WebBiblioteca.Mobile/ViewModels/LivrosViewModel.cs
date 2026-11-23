
using System.Collections.ObjectModel;
using WebBiblioteca.Mobile.Models;
using WebBiblioteca.Mobile.Services;

namespace WebBiblioteca.Mobile.ViewModels
{
    public class LivrosViewModel : BaseViewModel
    {
        private readonly ILivroService _livroService;

        private string _filtroDisponibilidade = "Todos";

        public string FiltroDisponibilidade
        {
            get => _filtroDisponibilidade;
            set
            {
                _filtroDisponibilidade = value ?? "Todos";
                OnPropertyChanged(nameof(FiltroDisponibilidade));
                FiltrarLivros();
            }
        }

        public ObservableCollection<Livro> Livros { get; set; } = new();

        private bool _estaACarregar;
        public bool EstaACarregar
        {
            get => _estaACarregar;
            set
            {
                _estaACarregar = value;
                OnPropertyChanged(nameof(EstaACarregar));
            }
        }

        private string _mensagem = string.Empty;
        public string Mensagem
        {
            get => _mensagem;
            set
            {
                _mensagem = value;
                OnPropertyChanged(nameof(Mensagem));
            }
        }

        public LivrosViewModel(ILivroService livroService)
        {
            _livroService = livroService;
        }

        public async Task CarregarLivrosAsync()
        {
            if (EstaACarregar)
                return;

            EstaACarregar = true;
            Mensagem = string.Empty;

            try
            {
                var livros = await _livroService.GetLivros();

                _todosLivros = livros;
                FiltrarLivros();
            }
            catch (Exception)
            {
                Mensagem = "Não foi possível carregar os livros.";
            }
            finally
            {
                EstaACarregar = false;
            }
        }


        private List<Livro> _todosLivros = new();

        private string _pesquisa = string.Empty;

        public string Pesquisa
        {
            get => _pesquisa;
            set
            {
                _pesquisa = value ?? string.Empty;
                OnPropertyChanged(nameof(Pesquisa));
                FiltrarLivros();
            }
        }

        private void FiltrarLivros()
        {
            Livros.Clear();

            foreach (var livro in _todosLivros)
            {
                bool correspondePesquisa =
                    string.IsNullOrWhiteSpace(Pesquisa) ||
                    (livro.Titulo?.Contains(
                        Pesquisa.Trim(),
                        StringComparison.OrdinalIgnoreCase) ?? false);

                bool correspondeDisponibilidade =
                    FiltroDisponibilidade == "Todos" ||
                    (FiltroDisponibilidade == "Disponíveis" &&
                     livro.ExemplaresDisponiveis > 0) ||
                    (FiltroDisponibilidade == "Indisponíveis" &&
                     livro.ExemplaresDisponiveis == 0);

                if (correspondePesquisa && correspondeDisponibilidade)
                {
                    Livros.Add(livro);
                }
            }

            if (_todosLivros.Count == 0)
            {
                Mensagem = "Não existem livros no catálogo.";
            }
            else if (Livros.Count == 0)
            {
                Mensagem = "Nenhum livro corresponde aos filtros.";
            }
            else
            {
                Mensagem = string.Empty;
            }
        }
    }
}