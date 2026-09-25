using Microsoft.AspNetCore.Mvc.Rendering;
using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public interface ICategoriaRepository
    {
        IQueryable<Categoria> GetAll();
        Task CreateAsync(Categoria categoria);
        Task<Categoria?> GetByIdAsync(int id);
        Task UpdateAsync(Categoria categoria);
        Task DeleteAsync(Categoria categoria);
        Task<bool> HasLivrosAsync(int id);
        IEnumerable<SelectListItem> GetComboCategorias();
    }
}