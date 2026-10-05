using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public interface IEmprestimoRepository
    {
        IQueryable<Emprestimo> GetAll(bool incluirEliminados = false);
        Task<Emprestimo?> GetByIdAsync(int id, bool incluirEliminados = false);
        Task<bool> CreateAsync(Emprestimo emprestimo);
        Task<bool> DevolverLivroAsync(int idEmprestimo, int idLivro);
        Task<bool> AlterarPrazoAsync(int id, DateTime prazoDevolucao);
        Task<bool> DeleteAsync(int id);
        Task<bool> PagarPenalizacaoAsync(int idEmprestimo, int idPenalizacao);
        Task<bool> EmprestimoExisteAsync(int id);
        Task<bool> LeitorExisteAsync(int id);
        Task<bool> TemEmprestimosEmAtrasoAsync(int idLeitor);
        Task<bool> TemPenalizacoesPorPagarAsync(int idLeitor);
        Task<int> ContarLivrosEmprestadosAsync(int idLeitor);
        Task<bool> TemLivroEmprestadoAsync(int idLeitor, int idLivro);
    }
}