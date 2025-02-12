using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace SFCDesigner.Models.Elements
{
    /// <summary>
    /// Abstraktní třída pro základní vlastnosti prvků (Text, Image, Barcode).
    /// Obsahuje ID, vrstvy (Layer), pozici (X,Y), velikost (Width,Height).
    /// </summary>
    [XmlInclude(typeof(LabelLayout))]
    [XmlInclude(typeof(LabelText))]
    [XmlInclude(typeof(LabelBarcode))]
    [XmlInclude(typeof(LabelImage))]
    [XmlInclude(typeof(LabelQrCode))]
    public abstract class LabelBase : INotifyPropertyChanged
    {
        #region Fields

        private int _id;
        private int _layer;
        private double _locationX;
        private double _locationY;
        private double _width;
        private double _height;
        private bool _locked;

        #endregion

        #region Properties

        public int ID
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public int Layer
        {
            get => _layer;
            set => SetProperty(ref _layer, value);
        }

        public virtual double LocationX
        {
            get => _locationX;
            set => SetProperty(ref _locationX, value);
        }

        public virtual double LocationY
        {
            get => _locationY;
            set => SetProperty(ref _locationY, value);
        }

        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        public bool Locked
        {
            get => _locked;
            set => SetProperty(ref _locked, value);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Vyvolá událost PropertyChanged, aby se UI aktualizovalo.
        /// </summary>
        /// <param name="propertyName">Název změněné vlastnosti (doplňuje se automaticky).</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Pomocná metoda pro nastavení pole a vyvolání notifikace při změně.
        /// Změna se propaguje pouze v případě, že je nová hodnota odlišná od stávající.
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}