using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSAsharp;
using ReactiveUI;

namespace bsasharp_ui
{
    class BsaViewModel : ReactiveObject
    {
        private Bsa _bsa;
        public Bsa Bsa
        {
            get { return _bsa; }
            set { this.RaiseAndSetIfChanged(ref _bsa, value); }
        }
    }
}
