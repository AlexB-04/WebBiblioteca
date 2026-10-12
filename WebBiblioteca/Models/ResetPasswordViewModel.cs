using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O email não é válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O link de recuperação está incompleto.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "A palavra-passe é obrigatória.")]
        [MinLength(6, ErrorMessage = "A palavra-passe deve ter pelo menos 6 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nova palavra-passe")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "A confirmação da palavra-passe é obrigatória.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "As palavras-passe não coincidem.")]
        [Display(Name = "Confirmar palavra-passe")]
        public string Confirm { get; set; } = string.Empty;
    }
}