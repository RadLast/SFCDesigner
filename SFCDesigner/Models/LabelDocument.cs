using SFCDesigner.Models.Elements;
using System.Collections.Generic;

namespace SFCDesigner.Models
{
    /// <summary>
    /// Dokument reprezentující jeden konkrétní štítek.
    /// Obsahuje metadata a kolekci elementů (LabelBase).
    /// </summary>
    public class LabelDocument
    {
        #region Properties

        public Metadata Metadata { get; set; } = new Metadata();
        public List<LabelBase> Elements { get; set; } = new List<LabelBase>();

        #endregion
    }
}