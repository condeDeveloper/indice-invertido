namespace Busca.Core.Persistencia;

/// <summary>
/// Inteiros de tamanho variável, sete bits por byte.
/// </summary>
/// <remarks>
/// As listas de ocorrências são quase todas de números pequenos, principalmente
/// depois da codificação por diferença: um documento que veio logo depois do
/// anterior vira 1, e 1 cabe em um byte em vez de quatro. Em um índice de
/// verdade essa é a diferença entre o arquivo caber ou não em memória.
/// </remarks>
public static class Varint
{
    /// <summary>Escreve um inteiro não negativo.</summary>
    public static void Escrever(BinaryWriter escritor, int valor)
    {
        ArgumentNullException.ThrowIfNull(escritor);

        if (valor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "O varint aqui só guarda número não negativo.");
        }

        var restante = (uint)valor;

        while (restante >= 0x80)
        {
            escritor.Write((byte)(restante | 0x80));
            restante >>= 7;
        }

        escritor.Write((byte)restante);
    }

    /// <summary>Lê um inteiro não negativo.</summary>
    public static int Ler(BinaryReader leitor)
    {
        ArgumentNullException.ThrowIfNull(leitor);

        var resultado = 0;
        var deslocamento = 0;

        while (true)
        {
            if (deslocamento > 28)
            {
                throw new InvalidDataException("Varint longo demais para caber em um inteiro.");
            }

            var octeto = leitor.ReadByte();
            resultado |= (octeto & 0x7F) << deslocamento;

            if ((octeto & 0x80) == 0)
            {
                return resultado;
            }

            deslocamento += 7;
        }
    }

    /// <summary>
    /// Escreve uma sequência crescente guardando só a diferença entre um valor
    /// e o anterior.
    /// </summary>
    public static void EscreverDiferencas(BinaryWriter escritor, IReadOnlyList<int> valores)
    {
        ArgumentNullException.ThrowIfNull(valores);

        Escrever(escritor, valores.Count);

        var anterior = 0;
        foreach (var valor in valores)
        {
            if (valor < anterior)
            {
                throw new ArgumentException("A sequência precisa estar em ordem crescente.", nameof(valores));
            }

            Escrever(escritor, valor - anterior);
            anterior = valor;
        }
    }

    /// <summary>Lê de volta uma sequência gravada por diferenças.</summary>
    public static List<int> LerDiferencas(BinaryReader leitor)
    {
        var quantidade = Ler(leitor);
        var valores = new List<int>(quantidade);

        var acumulado = 0;
        for (var i = 0; i < quantidade; i++)
        {
            acumulado += Ler(leitor);
            valores.Add(acumulado);
        }

        return valores;
    }

    /// <summary>Quantos bytes o valor ocupa depois de codificado.</summary>
    public static int Tamanho(int valor)
    {
        if (valor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(valor));
        }

        var bytes = 1;
        var restante = (uint)valor;

        while (restante >= 0x80)
        {
            bytes++;
            restante >>= 7;
        }

        return bytes;
    }
}
