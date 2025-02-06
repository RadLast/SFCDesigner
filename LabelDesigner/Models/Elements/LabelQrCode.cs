using LabelDesigner.Services;
using System.ComponentModel.DataAnnotations;

namespace LabelDesigner.Models.Elements
{
    /// <summary>
    /// Reprezentuje QR kód jako prvek štítku.
    /// Obsahuje Data (text pro QR) atd.
    /// </summary>
    public class LabelQrCode : LabelBase
    {
        private string _data = string.Empty;

        [Required]
        public string Data
        {
            get => _data;
            set
            {
                _data = value;
                OnPropertyChanged();
            }
        }
    }
}
