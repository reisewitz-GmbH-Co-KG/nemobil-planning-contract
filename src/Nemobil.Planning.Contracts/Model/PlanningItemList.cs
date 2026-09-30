namespace Nemobil.Planning.Contracts.Model
{
    /// <summary>Offene Standard-Implementierung von <see cref="IPlanningItems"/>: eine unveränderliche
    /// Liste von <see cref="PlanningItem"/>-Einträgen.</summary>
    public sealed class PlanningItemList : IPlanningItems
    {
        /// <summary>Erzeugt die Liste. Die Schlüssel (<see cref="PlanningItem.Key"/>) müssen eindeutig sein.</summary>
        /// <exception cref="ArgumentException">Ein Schlüssel kommt mehrfach vor.</exception>
        public PlanningItemList(IEnumerable<PlanningItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            var list = items.ToList();
            var duplicate = list.GroupBy(i => i.Key, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);

            if (duplicate is not null)
            {
                throw new ArgumentException($"Schlüssel '{duplicate.Key}' kommt mehrfach vor.", nameof(items));
            }

            Items = list.AsReadOnly();
        }

        /// <inheritdoc/>
        public int Count => Items.Count;

        /// <inheritdoc/>
        public IReadOnlyList<PlanningItem> Items { get; }
    }
}
