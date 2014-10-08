using System.Collections.Generic;
using System.Dynamic;
using BSAsharp;
using Humanizer;

namespace bsasharp_ui
{
    class BsaTree
    {
        private readonly Bsa _bsa;
        private readonly IDictionary<string, object> _bsaExpando;

        public dynamic Expando
        {
            get { return _bsaExpando; }
        }

        public BsaTree(Bsa bsa)
        {
            _bsa = bsa;
            _bsaExpando = CreateNodeExpando();
            CreateStructure();
        }

        private void CreateStructure()
        {
            foreach (var folder in _bsa)
            {
                var current = _bsaExpando;

                var splitPath = folder.Path.Split('\\');
                foreach (var chunk in splitPath)
                {
                    //previousExpando = currentExpando;
                    if (!current.ContainsKey(chunk))
                        current.Add(chunk, (current = CreateNodeExpando()));
                }

                foreach (var file in folder)
                {
                    var sizeText = file.OriginalSize.Bytes().Humanize("0.00");
                    if (file.IsCompressed)
                        sizeText += " (" + file.Size.Bytes().Humanize("0.00") + " compressed)";

                    current.Add(file.Name, new
                    {
                        File = file,
                        SizeText = sizeText
                    });
                }
            }
        }

        private static dynamic CreateNodeExpando()
        {
            return new ExpandoObject();
        }
    }
}
