using Nemobil.Planning.Contracts.Model;

namespace Nemobil.Planning.Contracts
{
    /// <summary>Zentraler Vertrag zur gekapselten Planung. Was &amp; Warum offen — Wie gekapselt.
    /// Routing-/Charging-/Chaining-Abhängigkeiten sind Implementierungsdetail der Engine (Konstruktor-Injektion).</summary>
    public interface IPlanningEngine
    {
        /// <summary>Plant die Anfrage in den übergebenen Flottenzustand unter Berücksichtigung der
        /// zu planenden Elemente. Liefert Vorschläge oder ein leeres Ergebnis mit Diagnosegrund.
        /// Seiteneffektfrei bzgl. Persistenz/Buchung (entscheidet die Anbindung) und bzgl. der
        /// übergebenen Eingaben (sie werden nicht verändert).
        /// <para>Alle Eingaben sind neutral lesbar: <paramref name="fleet"/> über
        /// <see cref="IFleetState.Vehicles"/>, <paramref name="items"/> über
        /// <see cref="IPlanningItems.Items"/>. Eine Implementierung, die nur den Vertrag kennt, kommt
        /// damit ohne Wissen über die Repräsentation der Anbindung aus.</para></summary>
        Task<PlanningResult> PlanAsync(PlanningRequest request, IFleetState fleet, IPlanningItems items, CancellationToken cancellationToken);
    }
}
