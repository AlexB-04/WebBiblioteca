using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(50, ErrorMessage = "O nome pode conter no máximo 50 caracteres.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O apelido é obrigatório.")]
        [MaxLength(50, ErrorMessage = "O apelido pode conter no máximo 50 caracteres.")]
        [Display(Name = "Apelido")]
        public string Apelido { get; set; } = string.Empty;

        [Required(ErrorMessage = "O contacto é obrigatório.")]
        [MaxLength(20, ErrorMessage = "O contacto pode conter no máximo 20 caracteres.")]
        [Display(Name = "Contacto")]
        public string PhoneNumber { get; set; } = string.Empty;

        [BindNever]
        public string? Email { get; set; }

        [BindNever]
        public string? ImageUrl { get; set; }

        [Display(Name = "Foto")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Remover foto atual")]
        public bool RemovePhoto { get; set; }
    }
}