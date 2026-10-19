using Microsoft.AspNetCore.Authorization;
using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Data;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Funcionario")]
    public class EmprestimosController : Controller
    {
        private readonly IEmprestimoRepository _emprestimoRepository;
        private readonly IReservaRepository _reservaRepository;

        public EmprestimosController(
            IEmprestimoRepository emprestimoRepository,
            IReservaRepository reservaRepository)
        {
            _emprestimoRepository = emprestimoRepository;
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
        [ValidateAntiForgeryToken]
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
            catch (DbException)
            {
                return Conflict("Ocorreu um conflito de acesso à base de dados. Atualize e tente novamente.");
            }

            return Ok();
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
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
            catch (DbException)
            {
                return Conflict("Ocorreu um conflito de acesso à base de dados. Atualize e tente novamente.");
            }

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post([FromBody] Emprestimo emprestimo)
        {
            try
            {
                await _reservaRepository.AtualizarReservasExpiradasAsync();
                string? erro = await _emprestimoRepository.ObterErroCriacaoAsync(emprestimo);

                if (erro != null)
                {
                    return BadRequest(erro);
                }

                if (!await _emprestimoRepository.CreateAsync(emprestimo))
                {
                    return BadRequest("Os dados foram alterados. Atualize e verifique o leitor, o limite e a disponibilidade dos livros.");
                }

            }
            catch (DbUpdateException)
            {
                return Conflict("Não foi possível guardar o empréstimo. Atualize os dados e tente novamente.");
            }
            catch (DbException)
            {
                return Conflict("Ocorreu um conflito de acesso à base de dados. Atualize os dados e tente novamente.");
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
        [ValidateAntiForgeryToken]
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
            catch (DbException)
            {
                return Conflict("Ocorreu um conflito de acesso à base de dados. Atualize e tente novamente.");
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
        [ValidateAntiForgeryToken]
        [Route("{id}")]
        public async Task<IActionResult> DeleteEmprestimo(int id)
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
            catch (DbException)
            {
                return Conflict("Ocorreu um conflito de acesso à base de dados. Atualize e tente novamente.");
            }

            return Ok();
        }
    }
}