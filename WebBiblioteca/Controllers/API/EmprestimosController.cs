using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Data;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmprestimosController : Controller
    {
        private readonly IEmprestimoRepository _emprestimoRepository;
        private readonly ILeitorRepository _leitorRepository;
        private readonly ILivroRepository _livroRepository;
        private readonly IReservaRepository _reservaRepository;

        public EmprestimosController(
            IEmprestimoRepository emprestimoRepository,
            ILeitorRepository leitorRepository,
            ILivroRepository livroRepository,
            IReservaRepository reservaRepository)
        {
            _emprestimoRepository = emprestimoRepository;
            _leitorRepository = leitorRepository;
            _livroRepository = livroRepository;
            _reservaRepository = reservaRepository;
        }

        [HttpGet]
        [Route("{id?}")]
        public async Task<IActionResult> GetEmprestimos(int? id, int? idLeitor, bool incluirEliminados = false)
        {
            if (id == null)
            {
                var emprestimosFiltrados = _emprestimoRepository.GetAll(incluirEliminados);

                if (idLeitor.HasValue)
                {
                    emprestimosFiltrados = emprestimosFiltrados
                        .Where(emprestimo => emprestimo.IdLeitor == idLeitor.Value);
                }

                var emprestimos = await emprestimosFiltrados
                    .OrderByDescending(emprestimo => emprestimo.DataEmprestimo)
                    .ToListAsync();

                return Ok(emprestimos.Select(emprestimo => new
                {
                    emprestimo.IdEmprestimo,
                    emprestimo.IdLeitor,
                    Leitor = emprestimo.Leitor == null ? null : emprestimo.Leitor.Nome,
                    emprestimo.DataEmprestimo,
                    emprestimo.PrazoDevolucao,
                    emprestimo.Eliminado,
                    emprestimo.DataEliminacao,
                    Alteracoes = emprestimo.Alteracoes
                        .OrderBy(alteracao => alteracao.IdEmprestimoAlteracao)
                        .Select(alteracao => new
                        {
                            alteracao.IdEmprestimoAlteracao,
                            alteracao.DataAlteracao,
                            alteracao.PrazoAnterior,
                            alteracao.PrazoNovo
                        }).ToList(),
                    Penalizacoes = emprestimo.Penalizacoes
                        .OrderBy(penalizacao => penalizacao.IdPenalizacao)
                        .Select(penalizacao => new
                        {
                            penalizacao.IdPenalizacao,
                            penalizacao.IdEmprestimoDetalhe,
                            penalizacao.DiasAtraso,
                            penalizacao.Valor,
                            penalizacao.Motivo,
                            penalizacao.DataPenalizacao,
                            penalizacao.Pago,
                            penalizacao.DataPagamento
                        }).ToList(),
                    Detalhes = emprestimo.Detalhes
                        .OrderBy(detalhe => detalhe.IdEmprestimoDetalhe)
                        .Select(detalhe => new
                        {
                            detalhe.IdEmprestimoDetalhe,
                            detalhe.IdLivro,
                            Livro = detalhe.Livro == null ? null : detalhe.Livro.Titulo,
                            detalhe.DataDevolucao
                        }).ToList()
                }).ToList());
            }

            var emprestimoEncontrado = await _emprestimoRepository.GetByIdAsync(id.Value, incluirEliminados);

            if (emprestimoEncontrado == null ||
                (idLeitor.HasValue && emprestimoEncontrado.IdLeitor != idLeitor.Value))
            {
                return NotFound();
            }

            return Ok(new
            {
                emprestimoEncontrado.IdEmprestimo,
                emprestimoEncontrado.IdLeitor,
                Leitor = emprestimoEncontrado.Leitor == null ? null : emprestimoEncontrado.Leitor.Nome,
                emprestimoEncontrado.DataEmprestimo,
                emprestimoEncontrado.PrazoDevolucao,
                emprestimoEncontrado.Eliminado,
                emprestimoEncontrado.DataEliminacao,
                Alteracoes = emprestimoEncontrado.Alteracoes
                    .OrderBy(alteracao => alteracao.IdEmprestimoAlteracao)
                    .Select(alteracao => new
                    {
                        alteracao.IdEmprestimoAlteracao,
                        alteracao.DataAlteracao,
                        alteracao.PrazoAnterior,
                        alteracao.PrazoNovo
                    }).ToList(),
                Penalizacoes = emprestimoEncontrado.Penalizacoes
                    .OrderBy(penalizacao => penalizacao.IdPenalizacao)
                    .Select(penalizacao => new
                    {
                        penalizacao.IdPenalizacao,
                        penalizacao.IdEmprestimoDetalhe,
                        penalizacao.DiasAtraso,
                        penalizacao.Valor,
                        penalizacao.Motivo,
                        penalizacao.DataPenalizacao,
                        penalizacao.Pago,
                        penalizacao.DataPagamento
                    }).ToList(),
                Detalhes = emprestimoEncontrado.Detalhes
                    .OrderBy(detalhe => detalhe.IdEmprestimoDetalhe)
                    .Select(detalhe => new
                    {
                        detalhe.IdEmprestimoDetalhe,
                        detalhe.IdLivro,
                        Livro = detalhe.Livro == null ? null : detalhe.Livro.Titulo,
                        detalhe.DataDevolucao
                    }).ToList()
            });
        }

        [HttpPut]
        [Route("{id}/prazo")]
        public async Task<IActionResult> AlterarPrazo(int id, [FromBody] Emprestimo dados)
        {
            if (dados == null || dados.PrazoDevolucao == default)
            {
                return BadRequest("Indique um prazo de devolução válido.");
            }

            var emprestimo = await _emprestimoRepository.GetByIdAsync(id);

            if (emprestimo == null)
            {
                return NotFound();
            }

            if (emprestimo.Detalhes.Any(detalhe => detalhe.DataDevolucao != null))
            {
                return BadRequest("Não é possível alterar o prazo após a devolução de um livro.");
            }

            if (emprestimo.PrazoDevolucao.Date < DateTime.Today)
            {
                return BadRequest("Não é possível alterar o prazo de um empréstimo em atraso.");
            }

            if (dados.PrazoDevolucao.Date < DateTime.Today)
            {
                return BadRequest("O novo prazo não pode ser anterior ao dia de hoje.");
            }

            try
            {
                bool alterado = await _emprestimoRepository.AlterarPrazoAsync(id, dados.PrazoDevolucao);

                if (!alterado)
                {
                    return BadRequest("Não foi possível alterar o prazo. Respeite o período contado desde a data do empréstimo: Aluno 15 dias, Professor 30 dias e Público em Geral 7 dias.");
                }
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível guardar a alteração do prazo.");
            }

            return Ok();
        }

        [HttpPut]
        [Route("{id}/penalizacoes/{idPenalizacao}/pagar")]
        public async Task<IActionResult> PagarPenalizacao(int id, int idPenalizacao)
        {
            var emprestimo = await _emprestimoRepository.GetByIdAsync(id);

            if (emprestimo == null)
            {
                return NotFound();
            }

            var penalizacao = emprestimo.Penalizacoes.FirstOrDefault(penalizacaoDaLista =>
                penalizacaoDaLista.IdPenalizacao == idPenalizacao);

            if (penalizacao == null)
            {
                return NotFound();
            }

            if (penalizacao.Pago)
            {
                return BadRequest("Esta penalização já foi paga.");
            }

            try
            {
                bool pago = await _emprestimoRepository.PagarPenalizacaoAsync(id, idPenalizacao);

                if (!pago)
                {
                    return BadRequest("Não foi possível registar o pagamento. Atualize os dados da penalização.");
                }
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível guardar o pagamento.");
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Emprestimo emprestimo)
        {
            if (emprestimo == null)
            {
                return BadRequest("Dados do empréstimo inválidos.");
            }

            if (emprestimo.IdLeitor <= 0)
            {
                return BadRequest("Indique um leitor válido.");
            }

            if (emprestimo.Detalhes == null || emprestimo.Detalhes.Count == 0)
            {
                return BadRequest("Selecione pelo menos um livro.");
            }

            bool leitorExiste = await _emprestimoRepository.LeitorExisteAsync(emprestimo.IdLeitor);

            if (!leitorExiste)
            {
                return BadRequest("O leitor indicado não existe.");
            }

            var leitor = await _leitorRepository.GetByIdAsync(emprestimo.IdLeitor);

            if (leitor == null)
            {
                return BadRequest("O leitor indicado não existe.");
            }

            if (leitor.BloqueadoAte.HasValue && leitor.BloqueadoAte.Value > DateTime.Now)
            {
                return BadRequest("O leitor está temporariamente bloqueado.");
            }

            if (leitor.TipoUtilizador != "Aluno" &&
                leitor.TipoUtilizador != "Professor" &&
                leitor.TipoUtilizador != "Público em Geral")
            {
                return BadRequest("O tipo de utilizador do leitor é inválido.");
            }

            bool temAtraso = await _emprestimoRepository
                .TemEmprestimosEmAtrasoAsync(leitor.IdLeitor);

            if (temAtraso)
            {
                return BadRequest("O leitor possui livros com devolução em atraso.");
            }

            bool temMulta = await _emprestimoRepository
                .TemPenalizacoesPorPagarAsync(leitor.IdLeitor);

            if (temMulta)
            {
                return BadRequest("O leitor possui penalizações por pagar.");
            }

            var idsLivros = new List<int>();

            foreach (var detalhe in emprestimo.Detalhes)
            {
                if (detalhe == null || detalhe.IdLivro <= 0)
                {
                    return BadRequest("Indique livros válidos nos detalhes do empréstimo.");
                }

                if (idsLivros.Contains(detalhe.IdLivro))
                {
                    return BadRequest("O mesmo livro não pode aparecer duas vezes no empréstimo.");
                }

                idsLivros.Add(detalhe.IdLivro);

                var livro = await _livroRepository.GetByIdAsync(detalhe.IdLivro);

                if (livro == null)
                {
                    return BadRequest("Um dos livros indicados não existe.");
                }

                bool jaTemLivro = await _emprestimoRepository
                    .TemLivroEmprestadoAsync(leitor.IdLeitor, livro.IdLivro);

                if (jaTemLivro)
                {
                    return BadRequest("O leitor já tem um empréstimo ativo de um dos livros.");
                }

                if (livro.ExemplaresDisponiveis <= 0)
                {
                    return BadRequest("Um dos livros não tem exemplares disponíveis.");
                }

                bool podeEmprestar = await _reservaRepository
                    .PodeEmprestarAsync(leitor.IdLeitor, livro.IdLivro);

                if (!podeEmprestar)
                {
                    return BadRequest("Os exemplares de um dos livros estão destinados a outros leitores na fila de reservas.");
                }
            }

            int livrosEmprestados = await _emprestimoRepository
                .ContarLivrosEmprestadosAsync(leitor.IdLeitor);

            if (livrosEmprestados + idsLivros.Count > leitor.LimiteEmprestimos)
            {
                return BadRequest("O empréstimo ultrapassa o limite de livros do leitor.");
            }

            try
            {
                bool criado = await _emprestimoRepository.CreateAsync(emprestimo);

                if (!criado)
                {
                    return BadRequest("Não foi possível criar o empréstimo. Atualize os dados e verifique a disponibilidade dos livros.");
                }
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível guardar o empréstimo. Verifique o leitor e os livros associados.");
            }

            var emprestimoCriado = await _emprestimoRepository.GetByIdAsync(emprestimo.IdEmprestimo);

            if (emprestimoCriado == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                emprestimoCriado.IdEmprestimo,
                emprestimoCriado.IdLeitor,
                Leitor = emprestimoCriado.Leitor == null ? null : emprestimoCriado.Leitor.Nome,
                emprestimoCriado.DataEmprestimo,
                emprestimoCriado.PrazoDevolucao,
                emprestimoCriado.Eliminado,
                emprestimoCriado.DataEliminacao,
                Alteracoes = emprestimoCriado.Alteracoes
                    .OrderBy(alteracao => alteracao.IdEmprestimoAlteracao)
                    .Select(alteracao => new
                    {
                        alteracao.IdEmprestimoAlteracao,
                        alteracao.DataAlteracao,
                        alteracao.PrazoAnterior,
                        alteracao.PrazoNovo
                    }).ToList(),
                Penalizacoes = emprestimoCriado.Penalizacoes
                    .OrderBy(penalizacao => penalizacao.IdPenalizacao)
                    .Select(penalizacao => new
                    {
                        penalizacao.IdPenalizacao,
                        penalizacao.IdEmprestimoDetalhe,
                        penalizacao.DiasAtraso,
                        penalizacao.Valor,
                        penalizacao.Motivo,
                        penalizacao.DataPenalizacao,
                        penalizacao.Pago,
                        penalizacao.DataPagamento
                    }).ToList(),
                Detalhes = emprestimoCriado.Detalhes
                    .OrderBy(detalhe => detalhe.IdEmprestimoDetalhe)
                    .Select(detalhe => new
                    {
                        detalhe.IdEmprestimoDetalhe,
                        detalhe.IdLivro,
                        Livro = detalhe.Livro == null ? null : detalhe.Livro.Titulo,
                        detalhe.DataDevolucao
                    }).ToList()
            });
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] EmprestimoDetalhe detalhe)
        {
            if (detalhe == null || detalhe.IdLivro <= 0)
            {
                return BadRequest("Indique um livro válido para devolver.");
            }

            var emprestimo = await _emprestimoRepository.GetByIdAsync(id);

            if (emprestimo == null)
            {
                return NotFound();
            }

            var detalheExistente = emprestimo.Detalhes
                .FirstOrDefault(detalheDaLista => detalheDaLista.IdLivro == detalhe.IdLivro);

            if (detalheExistente == null)
            {
                return BadRequest("O livro indicado não pertence a este empréstimo.");
            }

            if (detalheExistente.DataDevolucao != null)
            {
                return BadRequest("Este livro já foi devolvido neste empréstimo.");
            }

            if (detalheExistente.Livro == null)
            {
                return BadRequest("O livro associado ao empréstimo não existe.");
            }

            try
            {
                bool devolvido = await _emprestimoRepository.DevolverLivroAsync(id, detalhe.IdLivro);

                if (!devolvido)
                {
                    return BadRequest("Não foi possível devolver o livro. Atualize os dados do empréstimo.");
                }
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível guardar a devolução. Verifique os dados associados.");
            }

            var emprestimoAtualizado = await _emprestimoRepository.GetByIdAsync(id);

            if (emprestimoAtualizado == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                emprestimoAtualizado.IdEmprestimo,
                emprestimoAtualizado.IdLeitor,
                Leitor = emprestimoAtualizado.Leitor == null ? null : emprestimoAtualizado.Leitor.Nome,
                emprestimoAtualizado.DataEmprestimo,
                emprestimoAtualizado.PrazoDevolucao,
                emprestimoAtualizado.Eliminado,
                emprestimoAtualizado.DataEliminacao,
                Alteracoes = emprestimoAtualizado.Alteracoes
                    .OrderBy(alteracao => alteracao.IdEmprestimoAlteracao)
                    .Select(alteracao => new
                    {
                        alteracao.IdEmprestimoAlteracao,
                        alteracao.DataAlteracao,
                        alteracao.PrazoAnterior,
                        alteracao.PrazoNovo
                    }).ToList(),
                Penalizacoes = emprestimoAtualizado.Penalizacoes
                    .OrderBy(penalizacao => penalizacao.IdPenalizacao)
                    .Select(penalizacao => new
                    {
                        penalizacao.IdPenalizacao,
                        penalizacao.IdEmprestimoDetalhe,
                        penalizacao.DiasAtraso,
                        penalizacao.Valor,
                        penalizacao.Motivo,
                        penalizacao.DataPenalizacao,
                        penalizacao.Pago,
                        penalizacao.DataPagamento
                    }).ToList(),
                Detalhes = emprestimoAtualizado.Detalhes
                    .OrderBy(detalheDaLista => detalheDaLista.IdEmprestimoDetalhe)
                    .Select(detalheDaLista => new
                    {
                        detalheDaLista.IdEmprestimoDetalhe,
                        detalheDaLista.IdLivro,
                        Livro = detalheDaLista.Livro == null ? null : detalheDaLista.Livro.Titulo,
                        detalheDaLista.DataDevolucao
                    }).ToList()
            });
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var emprestimo = await _emprestimoRepository.GetByIdAsync(id);

            if (emprestimo == null)
            {
                return NotFound();
            }

            if (emprestimo.Detalhes.Any(detalhe => detalhe.DataDevolucao == null))
            {
                return BadRequest("Devolva todos os livros antes de eliminar o empréstimo.");
            }

            if (emprestimo.Penalizacoes.Any(penalizacao => !penalizacao.Pago))
            {
                return BadRequest("Registe o pagamento das penalizações antes de eliminar o empréstimo.");
            }

            try
            {
                bool eliminado = await _emprestimoRepository.DeleteAsync(id);

                if (!eliminado)
                {
                    return BadRequest("Não foi possível eliminar o empréstimo. Verifique as devoluções e penalizações.");
                }
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível guardar a eliminação do empréstimo.");
            }

            return Ok();
        }
    }
}