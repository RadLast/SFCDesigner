using SFCDesigner.Models.Elements;
using System.Collections.Generic;

namespace SFCDesigner.Models
{
    /// <summary>
    /// Šablona štítku, obsahující velikost, metadata a kolekci objektů.
    /// </summary>
    public class LabelTemplate
    {
        #region Fields

        private double _width;
        private double _height;
        private Metadata _metadata = new Metadata();
        private List<LabelBase> _objects = new List<LabelBase>();

        #endregion

        #region Properties

        /// <summary>
        /// Šířka štítku.
        /// </summary>
        public double Width
        {
            get => _width;
            set => _width = value;
        }

        /// <summary>
        /// Výška štítku.
        /// </summary>
        public double Height
        {
            get => _height;
            set => _height = value;
        }

        /// <summary>
        /// Metadata šablony (autor, datum vytvoření atd.).
        /// </summary>
        public Metadata Metadata
        {
            get => _metadata;
            set => _metadata = value;
        }

        /// <summary>
        /// Kolekce objektů (LabelBase), které tvoří layout, text, obrázky, kódy atp.
        /// </summary>
        public List<LabelBase> Objects
        {
            get => _objects;
            set => _objects = value;
        }

        #endregion
    }
}