using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public interface IAvisoFuncionarioRepository
    {
        Task CreateAsync(AvisoFuncionario aviso);
        Task<List<AvisoFuncionario>> GetRecentesAsync(int quantidade);
    }
}