using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using System.Windows.Media;

namespace LabelDesigner.Models.Elements
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

        #endregion

        #region Properties

        public int ID
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public int Layer
        {
            get => _layer;
            set { _layer = value; OnPropertyChanged(); }
        }

        public virtual double LocationX
        {
            get => _locationX;
            set { _locationX = value; OnPropertyChanged(); }
        }

        public virtual double LocationY
        {
            get => _locationY;
            set { _locationY = value; OnPropertyChanged(); }
        }

        public double Width
        {
            get => _width;
            set { _width = value; OnPropertyChanged(); }
        }

        public double Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(); }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
