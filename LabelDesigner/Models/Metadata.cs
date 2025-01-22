using System;

namespace LabelDesigner.Models
{
    public class Metadata
    {
        private string _templateName = string.Empty;
        private string _author = string.Empty;
        private DateTime _creationDate = DateTime.Now;
        private DateTime _lastModified = DateTime.Now;

        public string TemplateName
        {
            get => _templateName;
            set { _templateName = value; }
        }

        public string Author
        {
            get => _author;
            set { _author = value; }
        }

        public DateTime CreationDate
        {
            get => _creationDate;
            set { _creationDate = value; }
        }

        public DateTime LastModified
        {
            get => _lastModified;
            set { _lastModified = value; }
        }
    }
}
