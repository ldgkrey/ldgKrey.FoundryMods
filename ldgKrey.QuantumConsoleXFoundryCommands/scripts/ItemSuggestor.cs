using Mod.QFSW.QC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ldgKrey.QuantumConsoleXFoundryCommands.Suggestors
{
    public sealed class ItemSuggestionAttribute : SuggestorTagAttribute
    {
        private readonly IQcSuggestorTag[] _tags = { new ItemSuggestionTag() };

        public override IQcSuggestorTag[] GetSuggestorTags()
        {
            return _tags;
        }
    }

    public struct ItemSuggestionTag : IQcSuggestorTag { }

    public class ItemSuggestor : BasicCachedQcSuggestor<string>
    {
        IEnumerable<string> itemTemplates = null;

        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
        {
            return context.HasTag<ItemSuggestionTag>();
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options)
        {
            if (itemTemplates == null)
                itemTemplates = ItemTemplateManager.getAllItemTemplates().Select(x => x.Value.name);

            return itemTemplates;
        }

        protected override IQcSuggestion ItemToSuggestion(string item)
        {
            return new RawSuggestion(item, true);
        }
    }
}
