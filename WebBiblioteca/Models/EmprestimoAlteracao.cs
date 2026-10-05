namespace WebBiblioteca.Models
{
    public class EmprestimoAlteracao
    {
        public int IdEmprestimoAlteracao { get; set; }
        public int IdEmprestimo { get; set; }
        public Emprestimo? Emprestimo { get; set; }
        public DateTime DataAlteracao { get; set; }
        public DateTime PrazoAnterior { get; set; }
        public DateTime PrazoNovo { get; set; }
    }
}