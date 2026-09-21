using Busca.Core.Consulta;

namespace Busca.Tests;

public class AnalisadorDeConsultaTests
{
    private readonly AnalisadorDeConsulta leitor = new();

    [Fact]
    public void Uma_palavra_vira_uma_clausula_opcional()
    {
        var consulta = leitor.Analisar("cenoura");

        consulta.Clausulas.Should().ContainSingle();
        consulta.Clausulas[0].Papel.Should().Be(Papel.Pode);
        consulta.Clausulas[0].No.Should().BeOfType<Termo>();
    }

    [Fact]
    public void O_mais_torna_a_clausula_obrigatoria()
    {
        leitor.Analisar("+cenoura").Clausulas[0].Papel.Should().Be(Papel.Deve);
    }

    [Fact]
    public void O_menos_torna_a_clausula_proibida()
    {
        leitor.Analisar("-cenoura").Clausulas[0].Papel.Should().Be(Papel.NaoPode);
    }

    [Fact]
    public void As_aspas_viram_frase()
    {
        var consulta = leitor.Analisar("\"bolo de chocolate\"");

        consulta.Clausulas[0].No.Should().BeOfType<Frase>();
    }

    [Fact]
    public void A_frase_guarda_a_folga_das_palavras_de_parada()
    {
        var frase = (Frase)leitor.Analisar("\"bolo de chocolate\"").Clausulas[0].No;

        frase.Termos.Should().HaveCount(2);
        frase.Folga.Should().Be(1);
    }

    [Fact]
    public void Frase_de_uma_palavra_so_vira_termo()
    {
        leitor.Analisar("\"cenoura\"").Clausulas[0].No.Should().BeOfType<Termo>();
    }

    [Fact]
    public void O_asterisco_vira_prefixo()
    {
        var no = leitor.Analisar("program*").Clausulas[0].No;

        no.Should().BeOfType<Prefixo>();
        ((Prefixo)no).Inicio.Should().Be("program");
    }

    [Fact]
    public void Combina_varios_papeis_na_mesma_consulta()
    {
        var consulta = leitor.Analisar("+bolo -cenoura chocolate");

        consulta.Do(Papel.Deve).Should().ContainSingle();
        consulta.Do(Papel.NaoPode).Should().ContainSingle();
        consulta.Do(Papel.Pode).Should().ContainSingle();
    }

    [Fact]
    public void Palavra_de_parada_sozinha_some_da_consulta()
    {
        leitor.Analisar("de").Vazia.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Consulta_vazia_nao_tem_clausula(string? entrada)
    {
        leitor.Analisar(entrada).Vazia.Should().BeTrue();
    }

    [Fact]
    public void Aspas_sem_fechar_nao_quebram()
    {
        var acao = () => leitor.Analisar("\"bolo de");

        acao.Should().NotThrow();
    }

    [Fact]
    public void Asterisco_sozinho_e_ignorado()
    {
        leitor.Analisar("*").Vazia.Should().BeTrue();
    }

    [Fact]
    public void A_consulta_se_descreve_de_volta()
    {
        leitor.Analisar("+bolo -cenoura").ToString().Should().Contain("+").And.Contain("-");
    }

    [Fact]
    public void Os_atalhos_montam_consultas_prontas()
    {
        Booleana.Todas(new Termo("a"), new Termo("b")).Do(Papel.Deve).Should().HaveCount(2);
        Booleana.Qualquer(new Termo("a")).Do(Papel.Pode).Should().ContainSingle();
    }
}

public class ExecutorTests
{
    [Fact]
    public void Um_termo_encontra_os_documentos_que_o_contem()
    {
        var resultado = Corpus.Motor().Buscar("cenoura");

        resultado.Referencias().Should().BeEquivalentTo(["d1", "d2"]);
    }

    [Fact]
    public void Termos_soltos_funcionam_como_ou()
    {
        var resultado = Corpus.Motor().Buscar("cenoura chocolate");

        resultado.Total.Should().Be(3);
    }

    [Fact]
    public void O_mais_exige_o_termo()
    {
        var resultado = Corpus.Motor().Buscar("+cenoura chocolate");

        resultado.Referencias().Should().BeEquivalentTo(["d1", "d2"]);
    }

    [Fact]
    public void Duas_clausulas_obrigatorias_se_cruzam()
    {
        var resultado = Corpus.Motor().Buscar("+cenoura +chocolate");

        resultado.Referencias().Should().Equal("d1");
    }

    [Fact]
    public void O_menos_exclui_o_termo()
    {
        var resultado = Corpus.Motor().Buscar("bolo -cenoura");

        resultado.Referencias().Should().Equal("d3");
    }

    [Fact]
    public void A_frase_exige_a_ordem()
    {
        var motor = Corpus.Motor();

        motor.Buscar("\"bolo de chocolate\"").Referencias().Should().Contain("d3");
        motor.Buscar("\"chocolate de bolo\"").Total.Should().Be(0);
    }

    [Fact]
    public void A_frase_atravessa_a_palavra_de_parada()
    {
        Corpus.Motor().Buscar("\"bolo de cenoura\"").Referencias().Should().Contain("d1");
    }

    [Fact]
    public void O_prefixo_pega_as_variacoes()
    {
        var resultado = Corpus.Motor().Buscar("program*");

        resultado.Referencias().Should().BeEquivalentTo(["d4", "d5"]);
    }

    [Fact]
    public void Prefixo_que_nao_casa_com_nada_devolve_vazio()
    {
        Corpus.Motor().Buscar("xyz*").Total.Should().Be(0);
    }

    [Fact]
    public void Consulta_sem_resultado_devolve_lista_vazia()
    {
        var resultado = Corpus.Motor().Buscar("jabuticaba");

        resultado.SemResultados.Should().BeTrue();
        resultado.Achados.Should().BeEmpty();
    }

    [Fact]
    public void Consulta_vazia_devolve_vazio()
    {
        Corpus.Motor().Buscar("   ").Total.Should().Be(0);
    }

    [Fact]
    public void O_radical_faz_a_busca_achar_a_flexao()
    {
        // O corpus tem "cenoura"; a busca por "cenouras" precisa encontrar.
        Corpus.Motor().Buscar("cenouras").Total.Should().Be(2);
    }

    [Fact]
    public void A_busca_ignora_acento()
    {
        Corpus.Motor().Buscar("programacao").Total.Should().BeGreaterThan(0);
    }
}
