using WebBiblioteca.Mobile.Models;

namespace WebBiblioteca.Mobile.Services
{
    public interface ILivroService
    {
        Task<List<Livro>> GetLivros();
        Task<Livro?> GetLivroById(int id);
    }
}