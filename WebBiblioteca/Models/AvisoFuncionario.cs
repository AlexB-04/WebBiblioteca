using System;
using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class AvisoFuncionario
    {
        public int IdAvisoFuncionario { get; set; }

        [Required]
        [MaxLength(500)]
        public string Mensagem { get; set; } = string.Empty;

        public DateTime DataCriacao { get; set; }
    }
}