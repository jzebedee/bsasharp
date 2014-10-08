using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BSAsharp;
using Humanizer;

namespace bsasharp_ui
{
    [DebuggerDisplay("{Text} ({Count})")]
    public class BsaNode : List<BsaNode>
    {
        public string Text { get; set; }

        public string SizeText
        {
            get
            {
                if (Tag != null)
                {
                    var file = Tag as BsaFile;

                    var sizeText = file.OriginalSize.Bytes().Humanize("0.00");
                    if (file.IsCompressed)
                        sizeText += " (" + file.Size.Bytes().Humanize("0.00") + " compressed)";

                    return sizeText;
                }

                var sum = Descendants.Sum(node => node.Size);
                return sum.Bytes().Humanize("0");
            }
        }

        private int? _size;
        public int Size
        {
            get
            {
                if (_size.HasValue)
                    return _size.Value;

                return Descendants.Where(node => node.Tag == null).Sum(node => node.Size);
            }
            set { _size = value; }
        }
        public object Tag { get; set; }

        public IEnumerable<BsaNode> DescendantsAndSelf
        {
            get
            {
                yield return this;
                foreach (var d in Descendants)
                    yield return d;
            }
        }

        public IEnumerable<BsaNode> Descendants
        {
            get
            {
                return this.SelectMany(child => child.DescendantsAndSelf);
            }
        }
    }
}
