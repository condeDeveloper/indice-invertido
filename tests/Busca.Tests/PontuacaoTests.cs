using Busca.Core.Analise;
using Busca.Core.Pontuacao;

namespace Busca.Tests;

public class Bm25Tests
{
    private readonly Bm25 bm25 = Bm25.Padrao;

    [Fact]
    public void Os_parametros_padrao_sao_os_da_literatura()
    {
        bm25.K1.Should().Be(1.2);
        bm25.B.Should().Be(0.75);
    }

    [Theory]
    [InlineData(-0.1, 0.75)]
    [InlineData(1.2, -0.1)]
    [InlineData(1.2, 1.1)]
    public void Recusa_parametro_fora_da_faixa(double k1, double b)
    {
        var acao = () => new Bm25(k1, b);

        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void O_termo_raro_pesa_mais_que_o_comum()
    {
        var raro = Bm25.PesoNoCorpus(documentosComOTermo: 1, totalDeDocumentos: 1000);
        var comum = Bm25.PesoNoCorpus(documentosComOTermo: 900, totalDeDocumentos: 1000);

        raro.Should().BeGreaterThan(comum);
    }

    [Fact]
    public void O_peso_nunca_fica_negativo_nem_com_o_termo_em_tudo()
    {
        Bm25.PesoNoCorpus(1000, 1000).Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Corpus_vazio_nao_gera_peso()
    {
        Bm25.PesoNoCorpus(0, 0).Should().Be(0);
    }

    [Fact]
    public void A_pontuacao_cresce_com_a_frequencia()
    {
        var peso = Bm25.PesoNoCorpus(10, 1000);

        var uma = bm25.Pontuar(1, 100, 100, peso);
        var tres = bm25.Pontuar(3, 100, 100, peso);

        tres.Should().BeGreaterThan(uma);
    }

    [Fact]
    public void A_pontuacao_satura_conforme_a_frequencia_sobe()
    {
        var peso = Bm25.PesoNoCorpus(10, 1000);

        var ganhoInicial = bm25.Pontuar(2, 100, 100, peso) - bm25.Pontuar(1, 100, 100, peso);
        var ganhoTardio = bm25.Pontuar(21, 100, 100, peso) - bm25.Pontuar(20, 100, 100, peso);

        ganhoTardio.Should().BeLessThan(ganhoInicial);
    }

    [Fact]
    public void O_documento_curto_pontua_mais_que_o_longo_com_a_mesma_frequencia()
    {
        var peso = Bm25.PesoNoCorpus(10, 1000);

        var curto = bm25.Pontuar(2, 50, 100, peso);
        var longo = bm25.Pontuar(2, 500, 100, peso);

        curto.Should().BeGreaterThan(longo);
    }

    [Fact]
    public void Com_b_zero_o_comprimento_nao_importa()
    {
        var sem = new Bm25(b: 0);
        var peso = Bm25.PesoNoCorpus(10, 1000);

        sem.Pontuar(2, 50, 100, peso).Should().Be(sem.Pontuar(2, 500, 100, peso));
    }

    [Fact]
    public void Frequencia_zero_nao_pontua()
    {
        bm25.Pontuar(0, 100, 100, 1.0).Should().Be(0);
    }
}

public class DestacadorTests
{
    private readonly Destacador destacador = Destacador.Texto;

    private static IReadOnlyList<string> Termos(params string[] palavras)
        => palavras.Select(palavra => Analisador.Padrao.Termo(palavra)!).ToList();

    [Fact]
    public void Marca_o_termo_encontrado()
    {
        var trecho = destacador.Destacar("O bolo de cenoura ficou bom.", Termos("cenoura"));

        trecho.Should().Contain("*cenoura*");
    }

    [Fact]
    public void Marca_a_flexao_que_gerou_o_mesmo_radical()
    {
        var trecho = destacador.Destacar("Comprei duas cenouras hoje.", Termos("cenoura"));

        trecho.Should().Contain("*cenouras*");
    }

    [Fact]
    public void Preserva_a_caixa_original_do_texto()
    {
        var trecho = destacador.Destacar("A Cenoura é boa.", Termos("cenoura"));

        trecho.Should().Contain("*Cenoura*");
    }

    [Fact]
    public void Escolhe_a_janela_com_mais_termos()
    {
        var texto = new string('x', 300) + " cenoura bolo cenoura " + new string('y', 300);

        var trecho = destacador.Destacar(texto, Termos("cenoura", "bolo"));

        trecho.Should().Contain("*cenoura*").And.Contain("*bolo*");
    }

    [Fact]
    public void Poe_reticencias_quando_corta()
    {
        var texto = string.Join(' ', Enumerable.Repeat("palavra", 200)) + " cenoura";

        destacador.Destacar(texto, Termos("cenoura")).Should().StartWith("…");
    }

    [Fact]
    public void Sem_termo_casado_devolve_o_comeco_do_texto()
    {
        var trecho = destacador.Destacar("Um texto curto.", Termos("jabuticaba"));

        trecho.Should().Be("Um texto curto.");
    }

    [Fact]
    public void Texto_vazio_devolve_vazio()
    {
        destacador.Destacar("", Termos("x")).Should().BeEmpty();
        destacador.Destacar(null, Termos("x")).Should().BeEmpty();
    }

    [Fact]
    public void Sem_termo_nenhum_devolve_o_comeco()
    {
        destacador.Destacar("Um texto curto.", []).Should().Be("Um texto curto.");
    }

    [Fact]
    public void O_destacador_padrao_usa_a_marca_html()
    {
        Destacador.Padrao.Destacar("A cenoura.", Termos("cenoura")).Should().Contain("<mark>cenoura</mark>");
    }
}
