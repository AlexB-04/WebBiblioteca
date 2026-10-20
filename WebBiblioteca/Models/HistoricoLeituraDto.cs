using WebBiblioteca.Models;

namespace WebBiblioteca.Models
{
    // Estes dados servem para a página MVC e para a resposta JSON da API.
    // Não enviar diretamente entidades EF Core, que contêm relações de navegação.
    public class HistoricoLeituraDto
    {
        public int IdEmprestimo { get; set; }
        public int IdEmprestimoDetalhe { get; set; }
        public int IdLivro { get; set; }
        public string TituloLivro { get; set; } = string.Empty;
        public string? AutorLivro { get; set; }
        public DateTime DataEmprestimo { get; set; }
        public DateTime PrazoDevolucao { get; set; }
        public DateTime? DataDevolucao { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int DiasAtraso { get; set; }
        public bool EmprestimoArquivado { get; set; }
        public DateTime? DataArquivo { get; set; }
        public decimal? ValorPenalizacao { get; set; }
        public bool? PenalizacaoPaga { get; set; }
        public DateTime? DataPagamento { get; set; }
    }

    public static class HistoricoLeituras
    {
        public static List<HistoricoLeituraDto> Preparar(IEnumerable<Emprestimo> emprestimos)
        {
            var leituras = new List<HistoricoLeituraDto>();

            foreach (var emprestimo in emprestimos)
            {
                foreach (var detalhe in emprestimo.Detalhes.OrderBy(detalhe => detalhe.IdEmprestimoDetalhe))
                {
                    var penalizacao = emprestimo.Penalizacoes.FirstOrDefault(penalizacaoDaLista =>
                        penalizacaoDaLista.IdEmprestimoDetalhe == detalhe.IdEmprestimoDetalhe);

                    DateTime dataReferencia = detalhe.DataDevolucao ?? DateTime.Now;
                    int diasAtraso = Math.Max(0, (dataReferencia.Date - emprestimo.PrazoDevolucao.Date).Days);
                    string estado;

                    if (detalhe.DataDevolucao.HasValue)
                    {
                        if (diasAtraso > 0)
                        {
                            estado = "Devolvido com atraso";
                        }
                        else
                        {
                            estado = "Devolvido";
                        }
                    }
                    else if (diasAtraso > 0)
                    {
                        estado = "Em atraso";
                    }
                    else
                    {
                        estado = "Por devolver";
                    }

                    leituras.Add(new HistoricoLeituraDto
                    {
                        IdEmprestimo = emprestimo.IdEmprestimo,
                        IdEmprestimoDetalhe = detalhe.IdEmprestimoDetalhe,
                        IdLivro = detalhe.IdLivro,
                        TituloLivro = detalhe.Livro?.Titulo ?? "Livro sem título",
                        AutorLivro = detalhe.Livro?.Autor,
                        DataEmprestimo = emprestimo.DataEmprestimo,
                        PrazoDevolucao = emprestimo.PrazoDevolucao,
                        DataDevolucao = detalhe.DataDevolucao,
                        Estado = estado,
                        DiasAtraso = diasAtraso,
                        EmprestimoArquivado = emprestimo.Eliminado,
                        DataArquivo = emprestimo.DataEliminacao,
                        ValorPenalizacao = penalizacao?.Valor,
                        PenalizacaoPaga = penalizacao?.Pago,
                        DataPagamento = penalizacao?.DataPagamento
                    });
                }
            }

            return leituras;
        }
    }
}