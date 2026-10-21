using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Data;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers
{
    [Authorize(Roles = "Funcionario")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class DashboardController : Controller
    {
        private readonly IEmprestimoRepository _emprestimoRepository;
        private readonly IReservaRepository _reservaRepository;
        private readonly ILeitorRepository _leitorRepository;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IEmprestimoRepository emprestimoRepository,
            IReservaRepository reservaRepository,
            ILeitorRepository leitorRepository,
            ILogger<DashboardController> logger)
        {
            _emprestimoRepository = emprestimoRepository;
            _reservaRepository = reservaRepository;
            _leitorRepository = leitorRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardFuncionarioViewModel();

            try
            {
                DateTime hoje = DateTime.Today;
                DateTime limiteLevantamento = DateTime.Now.AddDays(-3);

                var emprestimos = _emprestimoRepository.GetAll();
                model.EmprestimosAtivos = await emprestimos.CountAsync(emprestimo =>
                    emprestimo.Detalhes.Any(detalhe => detalhe.DataDevolucao == null));

                model.EmprestimosAtrasados = await emprestimos.CountAsync(emprestimo =>
                    emprestimo.PrazoDevolucao < hoje &&
                    emprestimo.Detalhes.Any(detalhe => detalhe.DataDevolucao == null));

                // Inclui multas ainda por pagar mesmo de empréstimos já concluídos.
                model.PenalizacoesPorPagar = await _emprestimoRepository.GetAll(true)
                    .SelectMany(emprestimo => emprestimo.Penalizacoes)
                    .CountAsync(penalizacao => !penalizacao.Pago);

                var reservas = _reservaRepository.GetAll();
                model.ReservasAtivas = await reservas.CountAsync(reserva => reserva.Ativa);

                model.ReservasDisponiveis = await reservas.CountAsync(reserva =>
                    reserva.Ativa && reserva.DataDisponivel.HasValue &&
                    reserva.DataDisponivel.Value > limiteLevantamento);

                // A expiração é processada no módulo de reservas; o Dashboard não altera dados.
                model.ReservasComPrazoExpirado = await reservas.CountAsync(reserva =>
                    reserva.Ativa && reserva.DataDisponivel.HasValue &&
                    reserva.DataDisponivel.Value <= limiteLevantamento);

                model.LeitoresPendentesAprovacao = await _leitorRepository.GetAll()
                    .CountAsync(leitor => leitor.UserId != null &&
                        leitor.User != null && !leitor.User.Aprovado);

                await CarregarEventosAsync(model);
                return View(model);
            }
            catch (DbException ex)
            {
                return MostrarErroDados(model, ex);
            }
            catch (DbUpdateException ex)
            {
                return MostrarErroDados(model, ex);
            }
        }

        private async Task CarregarEventosAsync(DashboardFuncionarioViewModel model)
        {
            // DataReserva permite identificar a criação, mas não indica quem a efetuou.
            var criacoes = await _reservaRepository.GetAll()
                .OrderByDescending(reserva => reserva.DataReserva)
                .ThenByDescending(reserva => reserva.IdReserva)
                .Take(12)
                .Select(reserva => new
                {
                    reserva.IdReserva,
                    reserva.IdLeitor,
                    reserva.DataReserva,
                    NomeLeitor = reserva.Leitor != null ? reserva.Leitor.Nome : null,
                    TituloLivroAtual = reserva.Livro != null ? reserva.Livro.Titulo : null
                })
                .ToListAsync();

            foreach (var criacao in criacoes)
            {
                string nomeLeitor = string.IsNullOrWhiteSpace(criacao.NomeLeitor)
                    ? $"Leitor #{criacao.IdLeitor}" : criacao.NomeLeitor;
                string nomeLivro = criacao.TituloLivroAtual ?? "Livro sem título";

                model.EventosRecentes.Add(new DashboardEventoViewModel
                {
                    IdReserva = criacao.IdReserva,
                    Data = criacao.DataReserva,
                    Titulo = "Reserva criada",
                    Descricao = $"{nomeLeitor}. Livro atual: {nomeLivro}."
                });
            }

            // O histórico já é gravado pelo ReservaRepository. Não criamos registos duplicados.
            var alteracoes = await _reservaRepository.GetAll()
                .SelectMany(reserva => reserva.Alteracoes.Select(alteracao => new
                {
                    reserva.IdReserva,
                    reserva.IdLeitor,
                    NomeLeitor = reserva.Leitor != null ? reserva.Leitor.Nome : null,
                    alteracao.IdReservaAlteracao,
                    alteracao.DataAlteracao,
                    alteracao.Acao,
                    alteracao.LivroAnterior,
                    alteracao.LivroNovo
                }))
                .OrderByDescending(alteracao => alteracao.DataAlteracao)
                .ThenByDescending(alteracao => alteracao.IdReservaAlteracao)
                .Take(12)
                .ToListAsync();

            foreach (var alteracao in alteracoes)
            {
                string nomeLeitor = string.IsNullOrWhiteSpace(alteracao.NomeLeitor)
                    ? $"Leitor #{alteracao.IdLeitor}" : alteracao.NomeLeitor;
                string nomeLivro = alteracao.LivroNovo ?? alteracao.LivroAnterior ?? "Livro sem título";
                string descricao = $"{nomeLeitor}. Livro: {nomeLivro}.";

                if (alteracao.Acao == "AlteracaoLivro")
                {
                    descricao = $"{nomeLeitor}. De «{alteracao.LivroAnterior ?? "?"}» para «{alteracao.LivroNovo ?? "?"}».";
                }

                model.EventosRecentes.Add(new DashboardEventoViewModel
                {
                    IdReserva = alteracao.IdReserva,
                    Data = alteracao.DataAlteracao,
                    Titulo = ObterTituloEvento(alteracao.Acao),
                    Descricao = descricao
                });
            }

            model.EventosRecentes = model.EventosRecentes
                .OrderByDescending(evento => evento.Data)
                .ThenByDescending(evento => evento.IdReserva)
                .Take(12)
                .ToList();
        }

        private static string ObterTituloEvento(string? acao)
        {
            if (acao == "AlteracaoLivro")
            {
                return "Livro da reserva alterado";
            }
            else if (acao == "Cancelamento")
            {
                return "Reserva cancelada";
            }
            else if (acao == "Disponibilizacao")
            {
                return "Livro disponibilizado para levantamento";
            }
            else if (acao == "Indisponibilizacao")
            {
                return "Disponibilidade retirada";
            }
            else if (acao == "Levantamento")
            {
                return "Livro levantado";
            }
            else if (acao == "Expiracao")
            {
                return "Prazo de levantamento expirado";
            }
            else if (acao == "AtualizacaoFila")
            {
                return "Posição na fila atualizada";
            }

            return "Alteração registada na reserva";
        }

        private IActionResult MostrarErroDados(DashboardFuncionarioViewModel model, Exception ex)
        {
            _logger.LogError(ex, "Não foi possível carregar os dados do dashboard do funcionário.");
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            model.ErroDados = "Não foi possível consultar os dados da biblioteca. Tente novamente mais tarde.";
            return View("Index", model);
        }
    }
}