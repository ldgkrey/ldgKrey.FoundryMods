using Mod.QFSW.QC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ldgKrey.QuantumConsoleXFoundryCommands.Suggestors
{
    public sealed class TeleportSuggestionAttribute : SuggestorTagAttribute
    {
        private readonly IQcSuggestorTag[] _tags = { new TeleportSuggestionTag() };

        public override IQcSuggestorTag[] GetSuggestorTags()
        {
            return _tags;
        }
    }

    public struct TeleportSuggestionTag : IQcSuggestorTag { }

    public class TeleportSuggestor : BasicCachedQcSuggestor<string>
    {
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
        {
            return context.HasTag<TeleportSuggestionTag>();
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options)
        {
            return GameRoot.getClientCharacter().getWaypointDict().Values.Select(x => x.description);
        }

        protected override IQcSuggestion ItemToSuggestion(string item)
        {
            return new RawSuggestion(item, true);
        }
    }
}
