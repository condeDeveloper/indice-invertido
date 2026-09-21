using Busca.Core;
using Busca.Core.Indice;

namespace Busca.Tests;

/// <summary>
/// Um punhado de documentos curtos que servem de corpus para os testes de
/// busca. São curtos de propósito: assim dá para conferir a ordenação na mão.
/// </summary>
public static class Corpus
{
    /// <summary>Os documentos do corpus.</summary>
    public static IReadOnlyList<DocumentoParaIndexar> Documentos { get; } =
    [
        new("d1", "Receita de bolo de cenoura",
            "O bolo de cenoura leva cenoura, ovo, óleo e açúcar. A cobertura de chocolate é opcional, " +
            "mas todo mundo faz a cobertura de chocolate mesmo assim."),

        new("d2", "Como plantar cenoura",
            "A cenoura gosta de solo fofo e profundo. Plantar cenoura em terra dura dá cenoura torta. " +
            "A colheita acontece uns noventa dias depois da semeadura."),

        new("d3", "Bolo de chocolate simples",
            "Um bolo de chocolate rápido, com chocolate em pó, farinha, ovo e leite. Sem cobertura."),

        new("d4", "Programação orientada a objetos",
            "Programar com objetos é organizar o programa em torno de dados e comportamento. " +
            "O programador pensa em responsabilidades antes de pensar em código."),

        new("d5", "Programação funcional",
            "Na programação funcional o programa é montado com funções puras e dados imutáveis. " +
            "Programar assim reduz o estado compartilhado."),
    ];

    /// <summary>Monta um motor com o corpus já indexado.</summary>
    public static MotorDeBusca Motor()
    {
        var motor = new MotorDeBusca();
        motor.Indexar(Documentos);
        return motor;
    }

    /// <summary>Monta um índice com o corpus já indexado.</summary>
    public static IndiceInvertido Indice()
    {
        var indice = new IndiceInvertido();
        indice.IndexarVarios(Documentos);
        return indice;
    }

    /// <summary>As referências dos documentos encontrados, na ordem do resultado.</summary>
    public static IReadOnlyList<string> Referencias(this Core.Pontuacao.Resultado resultado)
        => resultado.Achados.Select(achado => achado.Documento.Referencia).ToList();
}
