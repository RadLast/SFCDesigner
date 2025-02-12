using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Statická třída zajišťující generování unikátních ID pro prvky (podle jejich typu).
/// </summary>
public static class IdGenerator
{
    #region Fields

    /// <summary>
    /// Mapa: název typu (např. "LabelText") -> další volné ID (int).
    /// </summary>
    private static readonly Dictionary<string, int> nextIds = new();

    #endregion

    #region Public Methods

    /// <summary>
    /// Vrátí další volné ID pro daný typ. Pokud typ nezná, začne od 1.
    /// </summary>
    /// <param name="elementType">Název typu, např. nameof(LabelText).</param>
    /// <returns>Unikátní ID.</returns>
    public static int GetNextId(string elementType)
    {
        if (!nextIds.ContainsKey(elementType))
            nextIds[elementType] = 1;

        int id = nextIds[elementType];
        nextIds[elementType]++;
        return id;
    }

    /// <summary>
    /// Inicializuje generátor ID tak, aby nenarážel na již existující prvky.
    /// </summary>
    /// <param name="existingElements">Sekvence n-tic (typ, id).</param>
    public static void Initialize(IEnumerable<(string elementType, int id)> existingElements)
    {
        // Pokud nemáme žádné prvky, resetujeme
        if (!existingElements.Any())
        {
            nextIds.Clear();
            return;
        }

        // Pro každý zadaný typ nastavíme "start" ID tak, aby bylo vyšší než nejvyšší známé ID.
        foreach (var (elementType, id) in existingElements)
        {
            int newStartId = id + 1;
            if (!nextIds.ContainsKey(elementType))
            {
                nextIds[elementType] = newStartId;
            }
            else
            {
                nextIds[elementType] = Math.Max(nextIds[elementType], newStartId);
            }
        }
    }

    #endregion
}