using System.ComponentModel.DataAnnotations;

namespace SFCDesigner.Models.Elements
{
    /// <summary>
    /// Reprezentuje QR kód jako prvek štítku.
    /// Obsahuje Data (text pro QR).
    /// </summary>
    public class LabelQrCode : LabelBase
    {
        #region Fields

        private string _data = string.Empty;

        #endregion

        #region Properties

        [Required]
        public string Data
        {
            get => _data;
            set => SetProperty(ref _data, value);
        }

        #endregion
    }
}