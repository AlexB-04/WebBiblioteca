using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class ReservaFormViewModel
    {
        public int IdReserva { get; set; }
        [Display(Name = "Leitor")]
        public int IdLeitor { get; set; }
        [Display(Name = "Livro")]
        public int IdLivro { get; set; }

        [ValidateNever]
        public List<Leitor> Leitores { get; set; } = new List<Leitor>();
        [ValidateNever]
        public List<Livro> Livros { get; set; } = new List<Livro>();
    }
}
