using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DatabaseModel
{


    public class CodeViewModel
    {
        public CodeViewModel(string Title, string Code, bool IsFile = false )
        {
            this.IsFile = IsFile;
            this.Title = Title;
            this.Code = Code;
        }

        public bool IsExpanded
        {
            get; set;
        }

        public bool IsFile
        {
            get;private set;
        }

        public ICommand Copy
        {
            get
            {
                return new DelegateCommand(() =>
              {
                  Clipboard.SetText(Code);
              });
            }
        }

        public string Title
        {
            get;
            private set;
        }

        public string Code
        {
            get;
            private set;
        }
    }
}
