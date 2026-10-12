using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class RecoverPasswordViewModel
    {
        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = string.Empty;
    }
}
