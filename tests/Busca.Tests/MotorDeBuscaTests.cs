using Busca.Core;

namespace Busca.Tests;

public class MotorDeBuscaTests
{
    [Fact]
    public void Indexa_e_encontra()
    {
        var motor = new MotorDeBusca();
        motor.Indexar("x", "Título", "um texto qualquer sobre cenoura");

        motor.QuantidadeDeDocumentos.Should().Be(1);
        motor.Buscar("cenoura").Total.Should().Be(1);
    }

    [Fact]
    public void O_mais_relevante_vem_primeiro()
    {
        var motor = Corpus.Motor();

        // Os dois documentos falam de cenoura, mas com frequências e tamanhos
        // diferentes, então o BM25 precisa separar um do outro.
        var resultado = motor.Buscar("cenoura");

        resultado.Achados.Should().HaveCount(2);
        resultado.Achados[0].Pontuacao.Should().BeGreaterThan(resultado.Achados[1].Pontuacao);
    }

    [Fact]
    public void O_resultado_traz_o_trecho_destacado()
    {
        var achado = Corpus.Motor().Buscar("cenoura").Achados[0];

        achado.Trecho.Should().Contain("<mark>");
        achado.TermosCasados.Should().NotBeEmpty();
    }

    [Fact]
    public void O_resultado_conta_o_total_mesmo_paginando()
    {
        var motor = Corpus.Motor();

        var pagina = motor.Buscar("cenoura", quantidade: 1);

        pagina.Achados.Should().ContainSingle();
        pagina.Total.Should().Be(2);
    }

    [Fact]
    public void A_paginacao_e_estavel_entre_duas_buscas()
    {
        var motor = Corpus.Motor();

        var primeira = motor.Buscar("bolo cenoura chocolate", quantidade: 2, pular: 0).Referencias();
        var segunda = motor.Buscar("bolo cenoura chocolate", quantidade: 2, pular: 2).Referencias();

        primeira.Should().NotIntersectWith(segunda);
    }

    [Fact]
    public void A_busca_registra_o_tempo()
    {
        Corpus.Motor().Buscar("cenoura").Milissegundos.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Interpretar_mostra_como_a_consulta_foi_entendida()
    {
        var interpretacao = Corpus.Motor().Interpretar("+bolo -cenoura choc*").ToString();

        interpretacao.Should().Contain("+").And.Contain("-").And.Contain("*");
    }

    [Fact]
    public void Sugere_pelo_prefixo()
    {
        var sugestoes = Corpus.Motor().Sugerir("cen");

        sugestoes.Should().NotBeEmpty();
        sugestoes.Should().OnlyContain(termo => termo.StartsWith("cen", StringComparison.Ordinal));
    }

    [Fact]
    public void As_sugestoes_vem_do_termo_mais_frequente_para_o_menos()
    {
        var motor = new MotorDeBusca();
        motor.Indexar("a", "", "carro carro");
        motor.Indexar("b", "", "carro");
        motor.Indexar("c", "", "carro carta");

        var sugestoes = motor.Sugerir("car");

        sugestoes.Should().Equal("carr", "cart");
    }

    [Fact]
    public void O_vocabulario_guarda_radical_e_nao_a_palavra_digitada()
    {
        var motor = new MotorDeBusca();
        motor.Indexar("a", "", "casa casaco");

        // As duas caem no mesmo radical, então viram um termo só no índice —
        // é o preço de a busca por "casas" encontrar "casa".
        motor.Sugerir("cas").Should().ContainSingle();
    }

    [Fact]
    public void Prefixo_vazio_nao_sugere_nada()
    {
        Corpus.Motor().Sugerir("").Should().BeEmpty();
        Corpus.Motor().Sugerir(null).Should().BeEmpty();
    }

    [Fact]
    public void Remover_tira_o_documento_do_resultado()
    {
        var motor = Corpus.Motor();

        motor.Remover("d1").Should().BeTrue();

        motor.Buscar("cenoura").Referencias().Should().Equal("d2");
    }

    [Fact]
    public void Reindexar_atualiza_o_que_a_busca_encontra()
    {
        var motor = new MotorDeBusca();
        motor.Indexar("x", "Antigo", "cenoura");

        motor.Buscar("cenoura").Total.Should().Be(1);

        motor.Indexar("x", "Novo", "chocolate");

        motor.Buscar("cenoura").Total.Should().Be(0);
        motor.Buscar("chocolate").Total.Should().Be(1);
    }

    [Fact]
    public void Um_termo_que_esta_em_todo_documento_quase_nao_ordena()
    {
        var motor = new MotorDeBusca();
        for (var i = 0; i < 10; i++)
        {
            motor.Indexar($"d{i}", "", "comum " + (i == 3 ? "raro" : "outro"));
        }

        var resultado = motor.Buscar("comum raro");

        resultado.Achados[0].Documento.Referencia.Should().Be("d3");
    }

    [Fact]
    public void Pedir_pagina_alem_do_fim_devolve_lista_vazia_mas_mantem_o_total()
    {
        var resultado = Corpus.Motor().Buscar("cenoura", quantidade: 10, pular: 50);

        resultado.Achados.Should().BeEmpty();
        resultado.Total.Should().Be(2);
    }
}
