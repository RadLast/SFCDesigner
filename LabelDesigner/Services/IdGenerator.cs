using System.Diagnostics;

namespace LabelDesigner.Services
{
    /// <summary>
    /// Generátor unikátních ID.
    /// Slouží k přiřazování ID elementům při serializaci a deserializaci layoutu.
    /// </summary>
    public static class IdGenerator
    {
        #region Fields

        private static int nextId = 0;

        #endregion

        #region Public Methods

        /// <summary>
        /// Vrátí unikátní ID a inkrementuje čítač.
        /// </summary>
        public static int GetNextId()
        {
            Debug.WriteLine($"Generating ID: {nextId}");
            return nextId++;
        }

        /// <summary>
        /// Resetuje generátor ID na zadanou počáteční hodnotu.
        /// </summary>
        public static void Reset(int startId)
        {
            Debug.WriteLine($"Resetting ID generator to start at: {startId}");
            nextId = startId;
        }

        #endregion
    }
}
