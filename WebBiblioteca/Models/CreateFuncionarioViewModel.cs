using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class CreateFuncionarioViewModel
    {
        [Required, MaxLength(50)]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        [Display(Name = "Apelido")]
        public string Apelido { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        [Display(Name = "Contacto")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
