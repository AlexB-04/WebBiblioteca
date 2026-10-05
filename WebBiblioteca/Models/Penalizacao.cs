using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class Penalizacao
    {
        public int IdPenalizacao { get; set; }
        public int IdEmprestimo { get; set; }
        public Emprestimo? Emprestimo { get; set; }
        public int IdEmprestimoDetalhe { get; set; }
        public EmprestimoDetalhe? Detalhe { get; set; }
        public int DiasAtraso { get; set; }
        public decimal Valor { get; set; }

        [MaxLength(300)]
        public string? Motivo { get; set; }

        public DateTime DataPenalizacao { get; set; }
        public bool Pago { get; set; }
        public DateTime? DataPagamento { get; set; }
    }
}