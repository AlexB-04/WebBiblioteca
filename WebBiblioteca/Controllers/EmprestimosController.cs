using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using WebBiblioteca.Data;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers
{
    [Authorize(Roles = "Funcionario")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class EmprestimosController : Controller
    {
        private readonly IEmprestimoRepository _emprestimoRepository;
        private readonly ILeitorRepository _leitorRepository;
        private readonly ILivroRepository _livroRepository;
        private readonly IReservaRepository _reservaRepository;
        private readonly ILogger<EmprestimosController> _logger;

        public EmprestimosController(
            IEmprestimoRepository emprestimoRepository,
            ILeitorRepository leitorRepository,
            ILivroRepository livroRepository,
            IReservaRepository reservaRepository,
            ILogger<EmprestimosController> logger)
        {
            _emprestimoRepository = emprestimoRepository;
            _leitorRepository = leitorRepository;
            _livroRepository = livroRepository;
            _reservaRepository = reservaRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? pesquisa, string? estado, int? idLeitor)
        {
            try
            {
                var emprestimos = _emprestimoRepository.GetAll(true);

                if (idLeitor.HasValue)
                {
                    emprestimos = emprestimos.Where(e => e.IdLeitor == idLeitor.Value);
                }

                if (!string.IsNullOrWhiteSpace(pesquisa))
                {
                    string texto = pesquisa.Trim().ToUpper();
                    int.TryParse(pesquisa, out int id);

                    emprestimos = emprestimos.Where(e =>
                        e.IdEmprestimo == id ||
                        (e.Leitor != null &&
                            ((e.Leitor.Nome != null && e.Leitor.Nome.ToUpper().Contains(texto)) ||
                             (e.Leitor.CodigoLeitor != null && e.Leitor.CodigoLeitor.ToUpper().Contains(texto)))) ||
                        e.Detalhes.Any(d =>
                            d.Livro != null &&
                            d.Livro.Titulo != null &&
                            d.Livro.Titulo.ToUpper().Contains(texto)));
                }

                if (estado == "Eliminados")
                {
                    emprestimos = emprestimos.Where(e => e.Eliminado);
                }
                else
                {
                    emprestimos = emprestimos.Where(e => !e.Eliminado);

                    if (estado == "Ativos")
                    {
                        emprestimos = emprestimos.Where(e =>
                            e.Detalhes.Any(d => d.DataDevolucao == null));
                    }
                    else if (estado == "Atrasados")
                    {
                        emprestimos = emprestimos.Where(e =>
                            e.PrazoDevolucao < DateTime.Today &&
                            e.Detalhes.Any(d => d.DataDevolucao == null));
                    }
                    else if (estado == "Concluidos")
                    {
                        emprestimos = emprestimos.Where(e =>
                            e.Detalhes.All(d => d.DataDevolucao != null));
                    }
                    else if (estado == "PorPagar")
                    {
                        emprestimos = emprestimos.Where(e =>
                            e.Penalizacoes.Any(p => !p.Pago));
                    }
                }

                ViewBag.Pesquisa = pesquisa;
                ViewBag.Estado = estado;
                ViewBag.IdLeitor = idLeitor;

                var lista = await emprestimos
                    .OrderByDescending(e => e.IdEmprestimo)
                    .ToListAsync();

                return View(lista);
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var emprestimo = await _emprestimoRepository.GetByIdAsync(id, true);

                if (emprestimo == null)
                {
                    return NaoEncontrado();
                }

                return View(emprestimo);
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        public async Task<IActionResult> Create(int? idLeitor)
        {
            try
            {
                var model = new EmprestimoViewModel
                {
                    IdLeitor = idLeitor ?? 0
                };

                await PreencherListasAsync(model);

                return View(model);
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdLeitor,IdsLivros")] EmprestimoViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var emprestimo = new Emprestimo
                    {
                        IdLeitor = model.IdLeitor
                    };

                    foreach (int idLivro in model.IdsLivros)
                    {
                        emprestimo.Detalhes.Add(new EmprestimoDetalhe
                        {
                            IdLivro = idLivro
                        });
                    }

                    await _reservaRepository.AtualizarReservasExpiradasAsync();

                    string? erro = await _emprestimoRepository.ObterErroCriacaoAsync(emprestimo);

                    if (erro != null)
                    {
                        ModelState.AddModelError(string.Empty, erro);
                    }
                    else
                    {
                        bool criado = await _emprestimoRepository.CreateAsync(emprestimo);

                        if (criado)
                        {
                            TempData["EmprestimoMessage"] = "Empréstimo registado. Os exemplares foram descontados e os levantamentos das reservas foram registados.";
                            return RedirectToAction("Details", new { id = emprestimo.IdEmprestimo });
                        }
                        else
                        {
                            ModelState.AddModelError(
                                string.Empty,
                                "Os dados foram alterados. Verifique o leitor, o limite e os exemplares disponíveis e tente novamente.");
                        }
                    }
                }

                await PreencherListasAsync(model);
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var emprestimo = await _emprestimoRepository.GetByIdAsync(id);

                if (emprestimo == null)
                {
                    return NaoEncontrado();
                }

                var model = new PrazoEmprestimoViewModel
                {
                    IdEmprestimo = id,
                    PrazoDevolucao = emprestimo.PrazoDevolucao,
                    Emprestimo = emprestimo
                };

                return View(model);
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            [FromRoute] int id,
            [Bind("IdEmprestimo,PrazoDevolucao")] PrazoEmprestimoViewModel model)
        {
            try
            {
                if (id != model.IdEmprestimo)
                {
                    return NaoEncontrado();
                }

                model.Emprestimo = await _emprestimoRepository.GetByIdAsync(id);

                if (model.Emprestimo == null)
                {
                    return NaoEncontrado();
                }

                if (ModelState.IsValid && model.PrazoDevolucao.HasValue)
                {
                    bool alterado = await _emprestimoRepository.AlterarPrazoAsync(
                        id, model.PrazoDevolucao.Value);

                    if (alterado)
                    {
                        TempData["EmprestimoMessage"] = "Prazo guardado. As alterações ficam registadas no histórico.";
                        return RedirectToAction("Details", new { id });
                    }

                    ModelState.AddModelError(
                        string.Empty,
                        "Não foi possível alterar o prazo. Não pode haver atrasos nem livros devolvidos; respeite as datas indicadas abaixo.");
                }

                return View(model);
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        public Task<IActionResult> Devolver(int id, int idLivro)
        {
            return ConfirmarAsync(id, "Devolver", idLivro, 0);
        }

        public Task<IActionResult> Pagar(int id, int idPenalizacao)
        {
            return ConfirmarAsync(id, "Pagar", 0, idPenalizacao);
        }

        public Task<IActionResult> Delete(int id)
        {
            return ConfirmarAsync(id, "Delete", 0, 0);
        }

        [HttpPost]
        [ActionName("Devolver")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DevolverConfirmado([FromRoute] int id, int idLivro)
        {
            try
            {
                bool devolvido = await _emprestimoRepository.DevolverLivroAsync(id, idLivro);

                if (devolvido)
                {
                    TempData["EmprestimoMessage"] = "Devolução registada. O exemplar, a fila de reservas e as eventuais penalizações foram atualizados.";
                    return RedirectToAction("Details", new { id });
                }

                return await ConfirmarAsync(
                    id, "Devolver", idLivro, 0,
                    "Não foi possível devolver. O livro pode já ter sido devolvido; atualize os dados.");
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        [HttpPost]
        [ActionName("Pagar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PagarConfirmado([FromRoute] int id, int idPenalizacao)
        {
            try
            {
                bool pago = await _emprestimoRepository.PagarPenalizacaoAsync(id, idPenalizacao);

                if (pago)
                {
                    TempData["EmprestimoMessage"] = "Pagamento registado. O pagamento não remove um bloqueio temporário ainda em vigor.";
                    return RedirectToAction("Details", new { id });
                }

                return await ConfirmarAsync(
                    id, "Pagar", 0, idPenalizacao,
                    "Não foi possível registar o pagamento. Verifique se a penalização já está paga.");
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([FromRoute] int id)
        {
            try
            {
                bool eliminado = await _emprestimoRepository.DeleteAsync(id);

                if (eliminado)
                {
                    TempData["EmprestimoMessage"] = "Empréstimo eliminado da lista corrente. O histórico continua disponível no filtro Eliminados.";
                    return RedirectToAction("Index");
                }

                return await ConfirmarAsync(
                    id, "Delete", 0, 0,
                    "Devolva todos os livros e registe o pagamento das penalizações antes de eliminar.");
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        public async Task<IActionResult> Penalizacoes(string? estado)
        {
            try
            {
                var emprestimos = await _emprestimoRepository.GetAll(true)
                    .Where(e => e.Penalizacoes.Any())
                    .ToListAsync();

                var penalizacoes = emprestimos.SelectMany(e => e.Penalizacoes);

                if (estado == "Pagas")
                {
                    penalizacoes = penalizacoes.Where(p => p.Pago);
                }
                else if (estado != "Todas")
                {
                    penalizacoes = penalizacoes.Where(p => !p.Pago);
                }

                ViewBag.Estado = estado;

                var lista = penalizacoes
                    .OrderByDescending(p => p.DataPenalizacao)
                    .ToList();

                return View(lista);
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        private async Task PreencherListasAsync(EmprestimoViewModel model)
        {
            model.Leitores = await _leitorRepository.GetAll()
                .Where(l =>
                    l.UserId == null ||
                    (l.User != null &&
                     l.User.Aprovado &&
                     !l.User.DeveAlterarPassword &&
                     l.User.EmailConfirmed))
                .OrderBy(l => l.Nome)
                .ToListAsync();

            model.Livros = await _livroRepository.GetAll()
                .OrderBy(l => l.Titulo)
                .ToListAsync();
        }

        private async Task<IActionResult> ConfirmarAsync(
            int id, string acao, int idLivro, int idPenalizacao, string? erro = null)
        {
            try
            {
                var emprestimo = await _emprestimoRepository.GetByIdAsync(id, true);

                if (emprestimo == null)
                {
                    return NaoEncontrado();
                }

                var model = new OperacaoEmprestimoViewModel
                {
                    Emprestimo = emprestimo,
                    Acao = acao,
                    IdLivro = idLivro,
                    IdPenalizacao = idPenalizacao,
                    Erro = erro
                };

                if (acao == "Devolver")
                {
                    var detalhe = emprestimo.Detalhes.FirstOrDefault(d => d.IdLivro == idLivro);

                    if (detalhe == null)
                    {
                        return NaoEncontrado();
                    }

                    model.Titulo = "Confirmar devolução";
                    model.Mensagem = "Registar a devolução de «" + detalhe.Livro?.Titulo + "» com a data e hora atuais?";

                    if (detalhe.DataDevolucao != null)
                    {
                        model.Erro = "Este livro já foi devolvido.";
                    }
                }
                else if (acao == "Pagar")
                {
                    var penalizacao = emprestimo.Penalizacoes.FirstOrDefault(
                        p => p.IdPenalizacao == idPenalizacao);

                    if (penalizacao == null)
                    {
                        return NaoEncontrado();
                    }

                    model.Titulo = "Registar pagamento";
                    model.Mensagem = "Confirme apenas depois de receber o valor da penalização. Este registo não efetua uma cobrança eletrónica.";

                    if (penalizacao.Pago)
                    {
                        model.Erro = "Esta penalização já foi paga.";
                    }
                }
                else
                {
                    model.Titulo = "Eliminar empréstimo";
                    model.Mensagem = "Eliminar da lista corrente? As devoluções, penalizações e alterações continuam no histórico. Os exemplares não serão alterados.";

                    if (emprestimo.Detalhes.Any(d => d.DataDevolucao == null))
                    {
                        model.Erro = "Devolva todos os livros antes de eliminar.";
                    }
                    else if (emprestimo.Penalizacoes.Any(p => !p.Pago))
                    {
                        model.Erro = "Existem penalizações por pagar.";
                    }
                }

                if (emprestimo.Eliminado)
                {
                    model.Erro = "Este empréstimo já foi eliminado. O histórico é apenas de consulta.";
                }

                return View("Confirmar", model);
            }
            catch (DbUpdateException ex)
            {
                return ErroDados(ex);
            }
            catch (DbException ex)
            {
                return ErroDados(ex);
            }
        }

        private IActionResult NaoEncontrado()
        {
            Response.StatusCode = 404;
            return View("Erro", "O empréstimo, livro ou penalização indicado não foi encontrado.");
        }

        private IActionResult ErroDados(Exception ex)
        {
            _logger.LogError(ex, "Falha numa operação de empréstimos.");

            // Nem todos os erros de base de dados são conflitos (HTTP 409).
            // Sem identificar a causa exata, é mais seguro devolver erro de servidor.
            Response.StatusCode = 500;

            return View("Erro", "Não foi possível concluir a operação. Consulte os dados atuais antes de tentar novamente.");
        }
    }
}