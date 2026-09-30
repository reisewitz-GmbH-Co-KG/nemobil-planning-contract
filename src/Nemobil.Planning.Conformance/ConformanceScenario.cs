using Nemobil.Planning.Contracts.Model;

namespace Nemobil.Planning.Conformance
{
    /// <summary>Erwartete Wirkungsart eines Szenarios (Prüfgruppe G1).</summary>
    public enum ScenarioExpectation
    {
        /// <summary>Keine Erwartung an die Anzahl der Vorschläge; das Szenario prüft nur Invarianten.</summary>
        Any = 0,

        /// <summary>Die Anfrage ist einplanbar: mindestens ein Vorschlag.</summary>
        AtLeastOneProposal = 1,

        /// <summary>Die Anfrage ist nicht einplanbar: kein Vorschlag und ein gesetzter Diagnosegrund.</summary>
        NoProposal = 2,
    }

    /// <summary>Ein Prüffall der Konformitätssuite: vollständige, neutrale Eingaben für einen
    /// <c>IPlanningEngine.PlanAsync</c>-Aufruf plus die erwartete Wirkungsart.</summary>
    public sealed class ConformanceScenario
    {
        /// <summary>Kennung im Konformitäts-Testkatalog, z. B. <c>G1-01</c>.</summary>
        public required string Id { get; init; }

        /// <summary>Sprechender Kurzname.</summary>
        public required string Name { get; init; }

        /// <summary>Prüfgruppe des Katalogs, aus der das Szenario stammt (G1 bis G4).</summary>
        public required string Group { get; init; }

        /// <summary>Beschreibung der Datenlage und der erwarteten Wirkung.</summary>
        public required string Description { get; init; }

        /// <summary>Erwartete Wirkungsart.</summary>
        public required ScenarioExpectation Expectation { get; init; }

        /// <summary>Die Planungsanfrage.</summary>
        public required PlanningRequest Request { get; init; }

        /// <summary>Der Flottenzustand.</summary>
        public IReadOnlyList<VehicleState> Vehicles { get; init; } = [];

        /// <summary>Die einzuplanenden Halte.</summary>
        public IReadOnlyList<PlanningItem> Items { get; init; } = [];

        /// <summary>Flottenzustand als Vertragsobjekt.</summary>
        public IFleetState CreateFleet() => new FleetSnapshot(Vehicles);

        /// <summary>Einzuplanende Halte als Vertragsobjekt.</summary>
        public IPlanningItems CreateItems() => new PlanningItemList(Items);

        /// <inheritdoc/>
        public override string ToString() => $"{Id} {Name}";
    }
}
