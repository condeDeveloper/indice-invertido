using Busca.Core.Analise;

namespace Busca.Tests;

public class NormalizadorTests
{
    [Theory]
    [InlineData("Ação", "acao")]
    [InlineData("CAFÉ", "cafe")]
    [InlineData("Peão", "peao")]
    [InlineData("Ünïcödé", "unicode")]
    [InlineData("já", "ja")]
    public void Tira_acento_e_baixa_a_caixa(string entrada, string esperado)
    {
        Normalizador.Normalizar(entrada).Should().Be(esperado);
    }

    [Fact]
    public void Texto_vazio_continua_vazio()
    {
        Normalizador.Normalizar(null).Should().BeEmpty();
        Normalizador.Normalizar("").Should().BeEmpty();
    }

    [Fact]
    public void Nao_mexe_no_que_ja_esta_normalizado()
    {
        Normalizador.Normalizar("banana").Should().Be("banana");
    }

    [Theory]
    [InlineData('a', true)]
    [InlineData('7', true)]
    [InlineData('_', true)]
    [InlineData(' ', false)]
    [InlineData('-', false)]
    public void Reconhece_o_que_e_caractere_de_palavra(char c, bool esperado)
    {
        Normalizador.EhDePalavra(c).Should().Be(esperado);
    }
}

public class TokenizadorTests
{
    [Fact]
    public void Separa_nas_pontuacoes_e_espacos()
    {
        var termos = Tokenizador.Separar("Olá, mundo! Tudo bem?").Select(o => o.Termo);

        termos.Should().Equal("ola", "mundo", "tudo", "bem");
    }

    [Fact]
    public void Numera_as_posicoes_a_partir_de_zero()
    {
        var posicoes = Tokenizador.Separar("um dois tres").Select(o => o.Posicao);

        posicoes.Should().Equal(0, 1, 2);
    }

    [Fact]
    public void Guarda_o_recorte_no_texto_original()
    {
        var segunda = Tokenizador.Separar("bolo de cenoura").Skip(2).First();

        segunda.Inicio.Should().Be(8);
        segunda.Tamanho.Should().Be(7);
        segunda.Fim.Should().Be(15);
    }

    [Fact]
    public void Mantem_numeros_como_termo()
    {
        Tokenizador.Separar("versao 2 de 2026").Select(o => o.Termo).Should().Contain("2026");
    }

    [Fact]
    public void Texto_vazio_nao_gera_termo()
    {
        Tokenizador.Separar("   ").Should().BeEmpty();
        Tokenizador.Separar(null).Should().BeEmpty();
    }
}

public class ListaDeParadaTests
{
    [Fact]
    public void A_lista_padrao_tem_as_palavras_mais_comuns()
    {
        ListaDeParada.Padrao.Contem("de").Should().BeTrue();
        ListaDeParada.Padrao.Contem("que").Should().BeTrue();
        ListaDeParada.Padrao.Contem("cenoura").Should().BeFalse();
    }

    [Fact]
    public void A_lista_guarda_as_palavras_sem_acento()
    {
        ListaDeParada.Padrao.Contem("nao").Should().BeTrue();
    }

    [Fact]
    public void A_lista_vazia_nao_descarta_nada()
    {
        ListaDeParada.Vazia.Contem("de").Should().BeFalse();
        ListaDeParada.Vazia.Quantidade.Should().Be(0);
    }

    [Fact]
    public void Da_para_acrescentar_e_tirar_palavras()
    {
        ListaDeParada.Padrao.Com("cenoura").Contem("cenoura").Should().BeTrue();
        ListaDeParada.Padrao.Sem("de").Contem("de").Should().BeFalse();
    }
}

public class RadicalizadorTests
{
    private readonly Radicalizador radicalizador = Radicalizador.Padrao;

    [Theory]
    [InlineData("carros", "carro")]
    [InlineData("flores", "flor")]
    [InlineData("papeis", "papel")]
    [InlineData("funis", "funil")]
    [InlineData("balões", "balao")]
    public void Reduz_o_plural(string palavra, string esperado)
    {
        radicalizador.Radicalizar(Normalizador.Normalizar(palavra))
            .Should().Be(radicalizador.Radicalizar(Normalizador.Normalizar(esperado)));
    }

    [Theory]
    [InlineData("programar", "programa")]
    [InlineData("programando", "programa")]
    [InlineData("programou", "programa")]
    public void Leva_as_formas_do_verbo_ao_mesmo_radical(string uma, string outra)
    {
        radicalizador.Radicalizar(uma).Should().Be(radicalizador.Radicalizar(outra));
    }

    [Fact]
    public void Reduz_o_adverbio_ao_mesmo_radical_do_adjetivo()
    {
        radicalizador.Radicalizar("rapidamente").Should().Be(radicalizador.Radicalizar("rapida"));
        radicalizador.Radicalizar("rapidamente").Should().Be("rapid");
    }

    [Fact]
    public void Nao_destroi_palavra_curta()
    {
        radicalizador.Radicalizar("sol").Should().Be("sol");
        radicalizador.Radicalizar("pe").Should().Be("pe");
    }

    [Fact]
    public void Nao_mexe_em_numero()
    {
        radicalizador.Radicalizar("2026").Should().Be("2026");
    }

    [Fact]
    public void Respeita_as_excecoes_do_plural()
    {
        radicalizador.Radicalizar("lapis").Should().Be("lapis");
        radicalizador.Radicalizar("virus").Should().Be("virus");
    }

    [Fact]
    public void Desligado_devolve_a_palavra_como_veio()
    {
        Radicalizador.Desligado.Radicalizar("programando").Should().Be("programando");
    }

    [Fact]
    public void Palavra_vazia_nao_quebra()
    {
        radicalizador.Radicalizar("").Should().BeEmpty();
    }
}

public class AnalisadorTests
{
    [Fact]
    public void A_esteira_descarta_palavra_de_parada()
    {
        Analisador.Padrao.Termos("o bolo de cenoura").Should().NotContain("de").And.NotContain("o");
    }

    [Fact]
    public void A_posicao_conta_as_palavras_descartadas()
    {
        var posicoes = Analisador.Padrao.Analisar("casa de praia").Select(o => o.Posicao);

        posicoes.Should().Equal(0, 2);
    }

    [Fact]
    public void A_esteira_simples_nao_descarta_nem_radicaliza()
    {
        Analisador.Simples.Termos("o bolo de cenouras").Should().Equal("o", "bolo", "de", "cenouras");
    }

    [Fact]
    public void Termo_isolado_passa_pela_mesma_esteira()
    {
        Analisador.Padrao.Termo("Cenouras").Should().Be(Analisador.Padrao.Termos("cenoura")[0]);
    }

    [Fact]
    public void Termo_que_e_palavra_de_parada_vira_nulo()
    {
        Analisador.Padrao.Termo("de").Should().BeNull();
    }

    [Fact]
    public void O_prefixo_e_normalizado_mas_nao_radicalizado()
    {
        Analisador.Padrao.Prefixo("Programaç").Should().Be("programac");
    }
}
