using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// Statická třída zajišťující generování unikátních ID pro jednotlivé typy prvků.
/// </summary>
public static class IdGenerator
{
    /// <summary>
    /// Slovník pro sledování posledně vydaného ID podle typu prvku. 
    /// Key: název typu (např. "LabelText"), Value: další volné ID.
    /// </summary>
    private static readonly Dictionary<string, int> nextIds = new();

    /// <summary>
    /// Vrátí unikátní ID pro daný typ prvku a interně inkrementuje čítač.
    /// Pokud typ ještě nezná, začne od 1.
    /// </summary>
    /// <param name="elementType">Název typu prvku (např. nameof(LabelText)).</param>
    /// <returns>Nové (unikátní) ID.</returns>
    public static int GetNextId(string elementType)
    {
        if (!nextIds.ContainsKey(elementType))
        {
            nextIds[elementType] = 1;
        }

        int id = nextIds[elementType];
        nextIds[elementType]++; // Inkrementujeme až po přiřazení ID

        Debug.WriteLine($"Generating ID for {elementType}: {id}");
        return id;
    }

    /// <summary>
    /// Načte existující prvky a nastaví počáteční stavy pro ID (aby se nepřekrývaly).
    /// </summary>
    /// <param name="existingElements">Sekvence tuple (elementType, id) pro již existující prvky.</param>
    public static void Initialize(IEnumerable<(string elementType, int id)> existingElements)
    {
        Debug.WriteLine("Initializing ID Generator...");

        // Pokud je seznam prázdný, resetujeme ID na výchozí hodnoty
        if (!existingElements.Any())
        {
            nextIds.Clear();
            Debug.WriteLine("No existing elements found, resetting ID generator.");
            return;
        }

        foreach (var (elementType, id) in existingElements)
        {
            // Nastavíme nextId pro tento typ = id + 1
            int newStartId = id + 1;

            // Pokud už existuje klíč, nastavíme maximum
            if (!nextIds.ContainsKey(elementType))
            {
                nextIds[elementType] = newStartId;
            }
            else
            {
                nextIds[elementType] = Math.Max(nextIds[elementType], newStartId);
            }
        }

        Debug.WriteLine("Current ID map after initialization:");
        foreach (var kvp in nextIds)
        {
            Debug.WriteLine($"{kvp.Key}: {kvp.Value}");
        }
    }
}
