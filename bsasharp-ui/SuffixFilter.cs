using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BrightIdeasSoftware;

namespace bsasharp_ui
{
    public class SuffixFilter : AbstractModelFilter
    {
        public static void SetSuffix(string value)
        {
            Debug.Assert(Tree != null && Tree.FileNameTrie != null);

            _suffixResults = string.IsNullOrEmpty(value) ? null : Tree.FileNameTrie.Retrieve(value);
        }

        public static BsaTree Tree { get; set; }

        private static IEnumerable<string> _suffixResults;

        public override bool Filter(object modelObject)
        {
            var node = modelObject as BsaNode;
            Debug.Assert(node != null);

            return _suffixResults == null || node.DescendantsAndSelf.Any(subnode => _suffixResults.Contains(subnode.Text));
        }
    }
}
