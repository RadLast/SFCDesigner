using CommunityToolkit.Mvvm.ComponentModel;
using SFCDesigner.Models;
using System;

namespace SFCDesigner.ViewModels
{
    /// <summary>
    /// ViewModel pro metadata dokumentu/šablony (autor, datum, název, atd.).
    /// </summary>
    public class MetaDataViewModel : ObservableObject
    {
        #region Fields

        private readonly Metadata _metadata;

        // Lze ukládat nezávisle na _metadata (pokud nechcete přímo do modelu).
        private string _currentFileName = "New Document";

        #endregion

        #region Constructor

        /// <summary>
        /// Vytváří nový MetaDataViewModel pro daný model <see cref="Metadata"/>.
        /// </summary>
        public MetaDataViewModel(Metadata metadata)
        {
            _metadata = metadata;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Název (typ) šablony nebo dokumentu.
        /// </summary>
        public string TemplateName
        {
            get => _metadata.TemplateName;
            set
            {
                if (_metadata.TemplateName != value)
                {
                    _metadata.TemplateName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Aktuální soubor, se kterým se pracuje (zobrazuje se v UI).
        /// </summary>
        public string CurrentFileName
        {
            get => _currentFileName;
            set => SetProperty(ref _currentFileName, value);
        }

        /// <summary>
        /// Autor dokumentu/šablony.
        /// </summary>
        public string Author
        {
            get => _metadata.Author;
            set
            {
                if (_metadata.Author != value)
                {
                    _metadata.Author = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Datum vytvoření dokumentu.
        /// </summary>
        public DateTime CreationDate
        {
            get => _metadata.CreationDate;
            set
            {
                if (_metadata.CreationDate != value)
                {
                    _metadata.CreationDate = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Datum poslední úpravy dokumentu.
        /// </summary>
        public DateTime LastModified
        {
            get => _metadata.LastModified;
            set
            {
                if (_metadata.LastModified != value)
                {
                    _metadata.LastModified = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion
    }
}