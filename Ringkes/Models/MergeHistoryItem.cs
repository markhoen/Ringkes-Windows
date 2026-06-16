using System;
using System.IO;

namespace Ringkes.Models
{
    public class MergeHistoryItem
    {
        public string FileName
        {
            get;
            set;
        }

        public string OutputPath
        {
            get;
            set;
        }

        public int SourceCount
        {
            get;
            set;
        }

        public DateTime CreatedAt
        {
            get;
            set;
        }

        public string FolderName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(OutputPath))
                    return "";

                var dir = Path.GetDirectoryName(OutputPath);

                if (string.IsNullOrWhiteSpace(dir))
                    return "";

                return new DirectoryInfo(dir).Name;
            }
        }
    }
}