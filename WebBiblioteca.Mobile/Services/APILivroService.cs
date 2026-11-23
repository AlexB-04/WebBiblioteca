
using System.Net;
using System.Text.Json;
using WebBiblioteca.Mobile.Models;

namespace WebBiblioteca.Mobile.Services
{
    public class APILivroService : ILivroService
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _enderecoBase;

        public APILivroService(HttpClient httpClient, string enderecoBase)
        {
            _httpClient = httpClient;
            _enderecoBase = new Uri(enderecoBase.TrimEnd('/') + "/");
        }

        // SELECT ALL
        public async Task<List<Livro>> GetLivros()
        {
            var url = new Uri(_enderecoBase, "api/Livros");

            using var resposta = await _httpClient.GetAsync(url);
            resposta.EnsureSuccessStatusCode();

            string conteudo = await resposta.Content.ReadAsStringAsync();

            var livros = JsonSerializer.Deserialize<List<Livro>>(
                conteudo,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return livros ?? new List<Livro>();
        }

        // SELECT BY ID
        public async Task<Livro?> GetLivroById(int id)
        {
            var url = new Uri(_enderecoBase, "api/Livros/" + id);

            using var resposta = await _httpClient.GetAsync(url);

            if (resposta.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            resposta.EnsureSuccessStatusCode();

            string conteudo = await resposta.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<Livro>(
                conteudo,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
    }
}