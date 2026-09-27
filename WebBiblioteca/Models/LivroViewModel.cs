using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class LivroViewModel : Livro
    {
        public IEnumerable<SelectListItem>? Categorias { get; set; }

        [Display(Name = "Capa")]
        public IFormFile? ImageFile { get; set; }
    }
}