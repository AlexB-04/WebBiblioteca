using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class RegisterNewUserViewModel
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(50)]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O apelido é obrigatório.")]
        [MaxLength(49)]
        [Display(Name = "Apelido")]
        public string Apelido { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Introduza um email válido.")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O contacto é obrigatório.")]
        [MaxLength(20)]
        [Display(Name = "Contacto")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione o tipo de leitor.")]
        [Display(Name = "Tipo de leitor")]
        [RegularExpression("^(Aluno|Professor|Público em Geral)$", ErrorMessage = "Selecione Aluno, Professor ou Público em Geral.")]
        public string TipoLeitor { get; set; } = string.Empty;
    }
}
