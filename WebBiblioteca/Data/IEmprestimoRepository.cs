using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public interface IEmprestimoRepository
    {
        IQueryable<Emprestimo> GetAll();
        Task<Emprestimo?> GetByIdAsync(int id);
        Task<bool> CreateAsync(Emprestimo emprestimo);
        Task<bool> EmprestimoExisteAsync(int id);
        Task<bool> LeitorExisteAsync(int id);
        Task<bool> TemEmprestimosEmAtrasoAsync(int idLeitor);
        Task<int> ContarLivrosEmprestadosAsync(int idLeitor);
        Task<bool> TemLivroEmprestadoAsync(int idLeitor, int idLivro);
    }
}