using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public class LivroRepository : ILivroRepository
    {
        private readonly DataContext _context;

        public LivroRepository(DataContext context)
        {
            _context = context;
        }

        public IQueryable<Livro> GetAll()
        {
            return _context.Livros
                .Include(l => l.CategoriaAtual)
                .AsNoTracking();
        }

        public async Task<Livro?> GetByIdAsync(int id)
        {
            return await _context.Livros
                .Include(l => l.CategoriaAtual)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.IdLivro == id);
        }

        public async Task CreateAsync(Livro livro)
        {
            livro.IdLivro = 0;
            livro.CategoriaAtual = null;

            await _context.Livros.AddAsync(livro);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Livro livro)
        {
            livro.CategoriaAtual = null;

            _context.Livros.Update(livro);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Livro livro)
        {
            livro.CategoriaAtual = null;

            _context.Livros.Remove(livro);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> CategoriaExisteAsync(int id)
        {
            return await _context.Categorias
                .AnyAsync(c => c.IdCategoria == id);
        }
    }
}