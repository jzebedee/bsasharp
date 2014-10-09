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
                    Debug.Assert(Tag != null);

                    var sizeText = Tag.OriginalSize.Bytes().Humanize("0.00");
                    if (Tag.IsCompressed)
                        sizeText += " (" + Tag.Size.Bytes().Humanize("0.00") + " compressed)";

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
                return _size.HasValue ? _size.Value : Descendants.Where(node => node.Tag == null).Sum(node => node.Size);
            }
            set { _size = value; }
        }

        public string CompressionLevel
        {
            get
            {
                if (Tag == null)
                    return "";

                return string.Format("{0}", Tag.Strategy.Humanize());
            }
        }
        public BsaFile Tag { get; set; }

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
