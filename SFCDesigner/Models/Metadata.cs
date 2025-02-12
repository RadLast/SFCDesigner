using System;

namespace SFCDesigner.Models
{
    /// <summary>
    /// Základní metadata dokumentu/šablony.
    /// </summary>
    public class Metadata
    {
        #region Fields

        private string _templateName = string.Empty;
        private string _currentFileName = "Document";
        private string _author = string.Empty;
        private DateTime _creationDate = DateTime.Now;
        private DateTime _lastModified = DateTime.Now;

        #endregion

        #region Properties

        public string TemplateName
        {
            get => _templateName;
            set => _templateName = value;
        }

        public string CurrentFileName
        {
            get => _currentFileName;
            set => _currentFileName = value;
        }

        public string Author
        {
            get => _author;
            set => _author = value;
        }

        public DateTime CreationDate
        {
            get => _creationDate;
            set => _creationDate = value;
        }

        public DateTime LastModified
        {
            get => _lastModified;
            set => _lastModified = value;
        }

        #endregion
    }
}