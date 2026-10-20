using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Data;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers
{
    [Authorize(Roles = "Leitor")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class HistoricoController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ILeitorRepository _leitorRepository;
        private readonly IEmprestimoRepository _emprestimoRepository;
        private readonly IReservaRepository _reservaRepository;

        public HistoricoController(UserManager<User> userManager,
            ILeitorRepository leitorRepository,
            IEmprestimoRepository emprestimoRepository,
            IReservaRepository reservaRepository)
        {
            _userManager = userManager;
            _leitorRepository = leitorRepository;
            _emprestimoRepository = emprestimoRepository;
            _reservaRepository = reservaRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // A identificação do leitor vem sempre da conta autenticada.
            var utilizador = await _userManager.GetUserAsync(User);
            if (utilizador == null)
            {
                return Unauthorized();
            }

            if (!utilizador.Aprovado || utilizador.DeveAlterarPassword || !utilizador.EmailConfirmed)
            {
                return Forbid();
            }

            var leitor = await _leitorRepository.GetByUserIdAsync(utilizador.Id);
            if (leitor == null)
            {
                return NotFound("Não existe uma ficha de leitor associada a esta conta.");
            }

            var emprestimos = await _emprestimoRepository.GetAll(true)
                .Where(emprestimo => emprestimo.IdLeitor == leitor.IdLeitor)
                .OrderByDescending(emprestimo => emprestimo.DataEmprestimo)
                .ThenByDescending(emprestimo => emprestimo.IdEmprestimo)
                .ToListAsync();

            // Uma consulta ao histórico não altera o estado das reservas.
            var reservas = await _reservaRepository.GetAll()
                .Where(reserva => reserva.IdLeitor == leitor.IdLeitor)
                .OrderByDescending(reserva => reserva.DataReserva)
                .ThenByDescending(reserva => reserva.IdReserva)
                .ToListAsync();

            var model = new HistoricoViewModel
            {
                NomeLeitor = leitor.Nome ?? string.Empty,
                CodigoLeitor = leitor.CodigoLeitor,
                Leituras = HistoricoLeituras.Preparar(emprestimos)
            };

            foreach (var emprestimo in emprestimos)
            {
                foreach (var alteracao in emprestimo.Alteracoes.OrderByDescending(alteracao => alteracao.DataAlteracao))
                {
                    model.AlteracoesPrazo.Add(new HistoricoPrazoViewModel
                    {
                        IdEmprestimo = emprestimo.IdEmprestimo,
                        DataAlteracao = alteracao.DataAlteracao,
                        PrazoAnterior = alteracao.PrazoAnterior,
                        PrazoNovo = alteracao.PrazoNovo
                    });
                }
            }

            foreach (var reserva in reservas)
            {
                string estado;

                if (reserva.Ativa)
                {
                    if (reserva.DataDisponivel.HasValue)
                    {
                        estado = "Disponível para levantamento (estado registado)";
                    }
                    else
                    {
                        estado = "Em espera";
                    }
                }
                else
                {
                    var ultimaFinalizacao = reserva.Alteracoes
                        .Where(alteracao => alteracao.Acao == "Cancelamento" ||
                            alteracao.Acao == "Expiracao" || alteracao.Acao == "Levantamento")
                        .OrderByDescending(alteracao => alteracao.DataAlteracao)
                        .ThenByDescending(alteracao => alteracao.IdReservaAlteracao)
                        .FirstOrDefault();

                    if (ultimaFinalizacao?.Acao == "Cancelamento")
                    {
                        estado = "Cancelada";
                    }
                    else if (ultimaFinalizacao?.Acao == "Expiracao")
                    {
                        estado = "Expirada";
                    }
                    else if (ultimaFinalizacao?.Acao == "Levantamento")
                    {
                        estado = "Levantada";
                    }
                    else
                    {
                        estado = "Inativa (motivo não registado)";
                    }
                }

                model.Reservas.Add(new HistoricoReservaViewModel
                {
                    IdReserva = reserva.IdReserva,
                    TituloLivro = reserva.Livro?.Titulo ?? "Livro sem título",
                    DataReserva = reserva.DataReserva,
                    Estado = estado,
                    Ordem = reserva.Ordem,
                    DataDisponivel = reserva.DataDisponivel
                });

                foreach (var alteracao in reserva.Alteracoes.OrderByDescending(alteracao => alteracao.DataAlteracao))
                {
                    model.AlteracoesReservas.Add(new HistoricoAlteracaoReservaViewModel
                    {
                        IdReserva = reserva.IdReserva,
                        DataAlteracao = alteracao.DataAlteracao,
                        Acao = alteracao.Acao ?? "Sem descrição",
                        LivroAnterior = alteracao.LivroAnterior,
                        LivroNovo = alteracao.LivroNovo,
                        OrdemAnterior = alteracao.OrdemAnterior,
                        OrdemNova = alteracao.OrdemNova
                    });
                }
            }

            model.AlteracoesPrazo = model.AlteracoesPrazo
                .OrderByDescending(alteracao => alteracao.DataAlteracao).ToList();
            model.AlteracoesReservas = model.AlteracoesReservas
                .OrderByDescending(alteracao => alteracao.DataAlteracao).ToList();

            return View(model);
        }
    }
}