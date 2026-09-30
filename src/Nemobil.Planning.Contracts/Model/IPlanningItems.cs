namespace Nemobil.Planning.Contracts.Model
{
    /// <summary>Die einzuplanenden Halte einer Anfrage.
    /// <para><see cref="Items"/> beschreibt jeden Halt neutral (siehe <see cref="PlanningItem"/>);
    /// <see cref="Count"/> entspricht <c>Items.Count</c> und erlaubt einen Leer-Check.
    /// <see cref="PlanningItemList"/> ist die offene Standard-Implementierung.</para></summary>
    public interface IPlanningItems
    {
        /// <summary>Anzahl der einzuplanenden Halte.</summary>
        int Count { get; }

        /// <summary>Die einzuplanenden Halte.</summary>
        IReadOnlyList<PlanningItem> Items { get; }
    }
}
