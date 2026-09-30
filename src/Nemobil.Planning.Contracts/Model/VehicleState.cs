namespace Nemobil.Planning.Contracts.Model
{
    /// <summary>Neutraler Zustand eines einplanbaren Fahrzeugs in genau einer Schicht (Tour).
    /// <para><b>Was:</b> alles, was eine Planung über ein Fahrzeug wissen muss, um eine Anfrage in
    /// seinen Fahrplan einzufügen — Schicht, aktueller Standort, Energie, Kapazitäten, Fähigkeiten und
    /// der vollständige Ist-Fahrplan.</para>
    /// <para><b>Warum:</b> eine Implementierung von <see cref="IPlanningEngine"/> soll ausschließlich
    /// aus dem Vertrag heraus arbeiten können, ohne die interne Repräsentation der Anbindung zu
    /// kennen.</para></summary>
    public sealed class VehicleState
    {
        /// <summary>Kennung des physischen Fahrzeugs (der Ressource). Ein Vorschlag nennt sie in
        /// <see cref="PlanningProposal.VehicleId"/>.</summary>
        public required string VehicleId { get; init; }

        /// <summary>Kennung der Schicht (Tour), in die geplant wird. Eindeutig innerhalb eines
        /// Flottenzustands; ein Vorschlag nennt sie in <see cref="PlanningProposal.TourId"/> und als
        /// <see cref="PlannedSchedule.VehicleScheduleId"/> seines Fahrplans.</summary>
        public required string TourId { get; init; }

        /// <summary>Schichtzeitraum: kein Halt darf vor <see cref="TimeWindow.Min"/> beginnen oder
        /// nach <see cref="TimeWindow.Max"/> enden (jeweils zuzüglich der Toleranz des Fensters).</summary>
        public required TimeWindow Shift { get; init; }

        /// <summary>Zuletzt bekannter Standort des Fahrzeugs, falls bekannt. Maßgeblich für die Planung ab
        /// <see cref="AvailableFrom"/>; ohne Angabe gilt der letzte unveränderliche Halt bzw. der
        /// Depot-Start als Ausgangspunkt.</summary>
        public GeoPoint? CurrentLocation { get; init; }

        /// <summary>Frühester Zeitpunkt, ab dem neue Halte eingeplant werden dürfen (z. B. der aktuelle
        /// Zeitpunkt bei einem bereits fahrenden Fahrzeug). Ohne Angabe gilt der Schichtbeginn.</summary>
        public DateTime? AvailableFrom { get; init; }

        /// <summary>Schlüssel des letzten bereits bedienten Halts. Dieser Halt und alle Halte vor ihm in
        /// <see cref="Stops"/> sind <b>unveränderlich</b>: sie liegen in der Vergangenheit oder werden
        /// gerade bedient. Eine Planung übernimmt sie unverändert an den Anfang des Fahrplans und fügt
        /// nichts vor ihnen ein. Ohne Angabe ist kein Halt über den Schlüssel fixiert; unabhängig davon
        /// ist ein Halt, dessen Abfahrt vor <see cref="AvailableFrom"/> liegt, ebenfalls unveränderlich.</summary>
        public string? LastFixedStopKey { get; init; }

        /// <summary>Kapazitäten des Fahrzeugs, benannt nach denselben Schlüsseln wie
        /// <see cref="PlannedStop.Quantities"/> (<c>ADULTS</c>, <c>CHILDS</c>, <c>LUGGAGE</c>,
        /// <c>REQUESTS</c>). Die Summe der Belegungen aller gleichzeitig beförderten Fahrgäste darf je
        /// Schlüssel diesen Wert nicht überschreiten. Ein Schlüssel ohne Eintrag ist unbeschränkt.</summary>
        public IReadOnlyDictionary<string, float> Capacity { get; init; } = new Dictionary<string, float>(StringComparer.Ordinal);

        /// <summary>Nutzbare Energiekapazität der Batterie in Wattstunden (Wh).</summary>
        public int EnergyCapacityWh { get; init; }

        /// <summary>Energie des Fahrzeugs zu Planungsbeginn in Wattstunden (Wh): nach dem letzten
        /// unveränderlichen Halt bzw. zum Schichtbeginn, wenn kein Halt fixiert ist.</summary>
        public int CurrentEnergyWh { get; init; }

        /// <summary>Durchschnittlicher Energieverbrauch in Wattstunden je gefahrenem Meter (Wh/m), wie ihn
        /// die Plattform für das Fahrzeug führt. Ein Richtwert für Planungen, die kein eigenes
        /// Verbrauchsmodell haben; 0 bedeutet „nicht bekannt“.</summary>
        public double ConsumptionWhPerMeter { get; init; }

        /// <summary>Fähigkeiten des Fahrzeugs als numerische Kennungen (z. B. Rollstuhlgerechtigkeit),
        /// in derselben Kodierung wie <see cref="PlanningItem.Skills"/> und <see cref="PlannedStop.Skills"/>.
        /// Ein Halt mit einer Fähigkeit, die das Fahrzeug nicht führt, darf nicht auf dieses Fahrzeug
        /// geplant werden.</summary>
        public IReadOnlyList<int> Skills { get; init; } = [];

        /// <summary>Der vollständige Ist-Fahrplan der Schicht in zeitlicher Reihenfolge, einschließlich
        /// Depot-Start und -Ende, sofern das Fahrzeug solche hat, und einschließlich bereits bedienter
        /// Halte. Die Schlüssel (<see cref="PlannedStop.Key"/>) sind stabil: ein Vorschlag muss jeden
        /// Fahrgast- und Depot-Halt dieses Fahrplans unter demselben Schlüssel wieder enthalten.</summary>
        public IReadOnlyList<PlannedStop> Stops { get; init; } = [];
    }
}
