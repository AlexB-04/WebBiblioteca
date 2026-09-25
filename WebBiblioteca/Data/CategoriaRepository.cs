using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public class CategoriaRepository : ICategoriaRepository
    {
        private readonly DataContext _context;

        public CategoriaRepository(DataContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Categoria categoria)
        {
            await _context.Categorias.AddAsync(categoria);
            await _context.SaveChangesAsync();
        }

        public IQueryable<Categoria> GetAll()
        {
            return _context.Categorias.AsNoTracking();
        }

        public async Task<Categoria?> GetByIdAsync(int id)
        {
            return await _context.Categorias
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCategoria == id);
        }

        public async Task UpdateAsync(Categoria categoria)
        {
            _context.Categorias.Update(categoria);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Categoria categoria)
        {
            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasLivrosAsync(int id)
        {
            return await _context.Livros.AnyAsync(l => l.IdCategoria == id);
        }

        public IEnumerable<SelectListItem> GetComboCategorias()
        {
            var list = _context.Categorias.Select(c => new SelectListItem
            {
                Text = c.Nome,
                Value = c.IdCategoria.ToString()
            }).OrderBy(c => c.Text).ToList();

            list.Insert(0, new SelectListItem
            {
                Text = "(Selecione uma categoria...)",
                Value = "0"
            });

            return list;
        }
    }
}