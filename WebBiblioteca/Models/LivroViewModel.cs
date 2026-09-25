using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebBiblioteca.Models
{
    public class LivroViewModel : Livro
    {
        public IEnumerable<SelectListItem>? Categorias { get; set; }
    }
}