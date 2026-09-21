using System.Globalization;
using System.Text;

namespace Busca.Core.Analise;

/// <summary>
/// Põe o texto em uma forma única antes de indexar. Sem isso, "Ação", "ação" e
/// "acao" seriam três termos diferentes, e quem digita sem acento nunca
/// encontraria nada.
/// </summary>
public static class Normalizador
{
    /// <summary>
    /// Passa para caixa baixa e tira os acentos, preservando o ç como c.
    /// </summary>
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return string.Empty;
        }

        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var construtor = new StringBuilder(decomposto.Length);

        foreach (var c in decomposto)
        {
            // Na forma decomposta o acento vira um caractere separado, da
            // categoria "marca sem espaçamento"; basta descartá-lo.
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            construtor.Append(char.ToLowerInvariant(c));
        }

        return construtor.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Indica se o caractere faz parte de uma palavra.</summary>
    public static bool EhDePalavra(char c) => char.IsLetterOrDigit(c) || c == '_';
}
