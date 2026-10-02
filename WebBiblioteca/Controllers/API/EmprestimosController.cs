using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Data;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers.API
{
    [Route("api/[controller]/{id?}")]
    [ApiController]
    public class EmprestimosController : Controller
    {
        private readonly IEmprestimoRepository _emprestimoRepository;
        private readonly ILeitorRepository _leitorRepository;
        private readonly ILivroRepository _livroRepository;

        public EmprestimosController(
            IEmprestimoRepository emprestimoRepository,
            ILeitorRepository leitorRepository,
            ILivroRepository livroRepository)
        {
            _emprestimoRepository = emprestimoRepository;
            _leitorRepository = leitorRepository;
            _livroRepository = livroRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetEmprestimos(int? id)
        {
            if (id == null)
            {
                var emprestimos = await _emprestimoRepository.GetAll()
                    .OrderByDescending(emprestimo => emprestimo.DataEmprestimo)
                    .ToListAsync();

                return Ok(emprestimos.Select(emprestimo => new
                {
                    emprestimo.IdEmprestimo,
                    emprestimo.IdLeitor,
                    Leitor = emprestimo.Leitor == null ? null : emprestimo.Leitor.Nome,
                    emprestimo.DataEmprestimo,
                    emprestimo.PrazoDevolucao,
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

            var emprestimoEncontrado = await _emprestimoRepository.GetByIdAsync(id.Value);

            if (emprestimoEncontrado == null)
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
    }
}