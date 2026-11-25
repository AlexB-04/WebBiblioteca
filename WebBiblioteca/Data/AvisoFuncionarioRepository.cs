using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public class AvisoFuncionarioRepository : IAvisoFuncionarioRepository
    {
        private readonly DataContext _context;

        public AvisoFuncionarioRepository(DataContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(AvisoFuncionario aviso)
        {
            aviso.DataCriacao = DateTime.Now;

            await _context.AvisosFuncionarios.AddAsync(aviso);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AvisoFuncionario>> GetRecentesAsync(int quantidade)
        {
            return await _context.AvisosFuncionarios
                .AsNoTracking()
                .OrderByDescending(aviso => aviso.DataCriacao)
                .ThenByDescending(aviso => aviso.IdAvisoFuncionario)
                .Take(quantidade)
                .ToListAsync();
        }
    }
}