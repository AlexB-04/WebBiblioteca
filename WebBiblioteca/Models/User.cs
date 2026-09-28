using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class User : IdentityUser
    {
        [Display(Name = "Nome")]
        [MaxLength(50, ErrorMessage = "O campo {0} pode conter no máximo {1} caracteres.")]
        public string? FirstName { get; set; }

        [Display(Name = "Apelido")]
        [MaxLength(50, ErrorMessage = "O campo {0} pode conter no máximo {1} caracteres.")]
        public string? LastName { get; set; }

        [Display(Name = "Nome completo")]
        public string FullName => $"{FirstName} {LastName}";
    }
}