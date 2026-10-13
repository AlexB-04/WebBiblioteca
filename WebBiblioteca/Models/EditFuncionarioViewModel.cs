using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class EditFuncionarioViewModel
    {
        [Required(ErrorMessage = "O funcionário é obrigatório.")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(50, ErrorMessage = "O nome pode conter no máximo 50 caracteres.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O apelido é obrigatório.")]
        [MaxLength(50, ErrorMessage = "O apelido pode conter no máximo 50 caracteres.")]
        [Display(Name = "Apelido")]
        public string Apelido { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O email não é válido.")]
        [MaxLength(256, ErrorMessage = "O email pode conter no máximo 256 caracteres.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O contacto é obrigatório.")]
        [MaxLength(20, ErrorMessage = "O contacto pode conter no máximo 20 caracteres.")]
        [Display(Name = "Contacto")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}