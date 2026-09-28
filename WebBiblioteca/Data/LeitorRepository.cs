using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public class LeitorRepository : ILeitorRepository
    {
        private readonly DataContext _context;

        public LeitorRepository(DataContext context)
        {
            _context = context;
        }

        public IQueryable<Leitor> GetAll()
        {
            return _context.Leitores.AsNoTracking();
        }

        public async Task<Leitor?> GetByIdAsync(int id)
        {
            return await _context.Leitores
                .AsNoTracking()
                .FirstOrDefaultAsync(leitor => leitor.IdLeitor == id);
        }

        public async Task CreateAsync(Leitor leitor)
        {
            leitor.IdLeitor = 0;
            leitor.Atrasos = 0;
            leitor.BloqueadoAte = null;
            leitor.UserId = null;
            leitor.User = null;

            await _context.Leitores.AddAsync(leitor);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Leitor leitor)
        {
            _context.Leitores.Update(leitor);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Leitor leitor)
        {
            _context.Leitores.Remove(leitor);
            await _context.SaveChangesAsync();
        }
    }
}