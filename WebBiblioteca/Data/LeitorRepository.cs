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
            leitor.CodigoLeitor = null;
            leitor.UserId = null;
            leitor.User = null;

            await _context.Leitores.AddAsync(leitor);
            await _context.SaveChangesAsync();
        }

        // Usado apenas pelo registo de contas, com UserId atribuído pelo servidor.
        public async Task CreateWithUserAsync(Leitor leitor)
        {
            leitor.IdLeitor = 0;
            leitor.Atrasos = 0;
            leitor.BloqueadoAte = null;
            leitor.CodigoLeitor = null;
            leitor.User = null;

            await _context.Leitores.AddAsync(leitor);
            await _context.SaveChangesAsync();
        }

        public async Task<Leitor?> GetByUserIdAsync(string userId)
        {
            return await _context.Leitores.AsNoTracking()
                .FirstOrDefaultAsync(leitor => leitor.UserId == userId);
        }

        public async Task<List<Leitor>> GetRegisteredReadersAsync()
        {
            return await _context.Leitores.AsNoTracking()
                .Include(leitor => leitor.User)
                .Where(leitor => leitor.User != null)
                .OrderBy(leitor => leitor.Nome)
                .ToListAsync();
        }

        public async Task UpdateAsync(Leitor leitor)
        {
            _context.Leitores.Update(leitor);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateProfileAsync(Leitor leitor)
        {
            // O perfil só altera o nome e o contacto da ficha.
            _context.Leitores.Attach(leitor);
            _context.Entry(leitor).Property(l => l.Nome).IsModified = true;
            _context.Entry(leitor).Property(l => l.Contacto).IsModified = true;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Leitor leitor)
        {
            _context.Leitores.Remove(leitor);
            await _context.SaveChangesAsync();
        }
    }
}
