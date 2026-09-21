using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Busca.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> fabrica;

    public ApiTests(WebApplicationFactory<Program> fabrica)
    {
        this.fabrica = fabrica;
    }

    private static object Documento(string referencia, string titulo, string texto)
        => new { referencia, titulo, texto };

    private async Task<HttpClient> ComCorpus()
    {
        var cliente = fabrica.CreateClient();
        var prefixo = Guid.NewGuid().ToString("N")[..8];

        await cliente.PostAsJsonAsync("/documentos/lote", new
        {
            documentos = Corpus.Documentos
                .Select(documento => Documento($"{prefixo}-{documento.Referencia}", documento.Titulo, documento.Texto))
                .ToArray(),
        });

        return cliente;
    }

    [Fact]
    public async Task Responde_a_verificacao_de_saude()
    {
        (await fabrica.CreateClient().GetAsync("/saude")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Indexa_um_documento()
    {
        var cliente = fabrica.CreateClient();
        var referencia = Guid.NewGuid().ToString("N");

        var resposta = await cliente.PostAsJsonAsync("/documentos", Documento(referencia, "Título", "texto sobre cenoura"));
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        corpo.GetProperty("comprimento").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Recusa_documento_sem_referencia()
    {
        var resposta = await fabrica.CreateClient()
            .PostAsJsonAsync("/documentos", Documento("  ", "t", "x"));

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Indexa_um_lote()
    {
        var cliente = fabrica.CreateClient();
        var prefixo = Guid.NewGuid().ToString("N")[..8];

        var resposta = await cliente.PostAsJsonAsync("/documentos/lote", new
        {
            documentos = new[]
            {
                Documento($"{prefixo}-1", "Um", "texto um"),
                Documento($"{prefixo}-2", "Dois", "texto dois"),
            },
        });

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        corpo.GetProperty("indexados").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Recusa_lote_com_documento_sem_referencia()
    {
        var resposta = await fabrica.CreateClient().PostAsJsonAsync("/documentos/lote", new
        {
            documentos = new[] { Documento("", "t", "x") },
        });

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Busca_e_devolve_os_achados_com_trecho()
    {
        var cliente = await ComCorpus();

        var resposta = await cliente.GetAsync("/busca?q=cenoura");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        corpo.GetProperty("total").GetInt32().Should().BeGreaterThan(0);
        corpo.GetProperty("achados")[0].GetProperty("trecho").GetString().Should().Contain("<mark>");
    }

    [Fact]
    public async Task A_resposta_mostra_como_a_consulta_foi_interpretada()
    {
        var cliente = await ComCorpus();

        var corpo = await cliente.GetFromJsonAsync<JsonElement>("/busca?q=%2Bbolo%20-cenoura");

        corpo.GetProperty("interpretacao").GetString().Should().Contain("+");
    }

    [Fact]
    public async Task Busca_sem_o_parametro_q_reclama()
    {
        (await fabrica.CreateClient().GetAsync("/busca")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Sugere_pelo_prefixo()
    {
        var cliente = await ComCorpus();

        var sugestoes = await cliente.GetFromJsonAsync<string[]>("/sugestoes?q=cen");

        sugestoes.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task Devolve_as_estatisticas_do_indice()
    {
        var cliente = await ComCorpus();

        var corpo = await cliente.GetFromJsonAsync<JsonElement>("/estatisticas");

        corpo.GetProperty("documentos").GetInt32().Should().BeGreaterThan(0);
        corpo.GetProperty("k1").GetDouble().Should().Be(1.2);
    }

    [Fact]
    public async Task Busca_um_documento_pela_referencia()
    {
        var cliente = fabrica.CreateClient();
        var referencia = Guid.NewGuid().ToString("N");
        await cliente.PostAsJsonAsync("/documentos", Documento(referencia, "Achado", "conteudo"));

        var resposta = await cliente.GetAsync($"/documentos/{referencia}");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Remove_um_documento()
    {
        var cliente = fabrica.CreateClient();
        var referencia = Guid.NewGuid().ToString("N");
        await cliente.PostAsJsonAsync("/documentos", Documento(referencia, "Some", "conteudo"));

        (await cliente.DeleteAsync($"/documentos/{referencia}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await cliente.GetAsync($"/documentos/{referencia}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Documento_inexistente_devolve_nao_encontrado()
    {
        var cliente = fabrica.CreateClient();

        (await cliente.GetAsync("/documentos/fantasma")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await cliente.DeleteAsync("/documentos/fantasma")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Lista_o_vocabulario()
    {
        var cliente = await ComCorpus();

        var vocabulario = await cliente.GetFromJsonAsync<string[]>("/vocabulario?quantidade=5");

        vocabulario.Should().NotBeNull().And.HaveCountLessThanOrEqualTo(5);
    }
}
