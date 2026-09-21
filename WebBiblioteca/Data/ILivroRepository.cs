using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public interface ILivroRepository
    {
        IQueryable<Livro> GetAll();
        Task<Livro?> GetByIdAsync(int id);
        Task CreateAsync(Livro livro);
        Task UpdateAsync(Livro livro);
        Task DeleteAsync(Livro livro);
        Task<bool> CategoriaExisteAsync(int id);
    }
}