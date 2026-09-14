namespace WebBiblioteca.Models
{
    public class Livro
    {
        public int IdLivro { get; set; }
        public string? Titulo { get; set; }
        public string? Autor { get; set; }
        public string? Editora { get; set; }
        public int AnoPublicacao { get; set; }
        public string? Genero { get; set; }
        public int ExemplaresDisponiveis { get; set; }
        public int IdCategoria { get; set; }
        public Categoria? CategoriaAtual { get; set; }
    }
}