namespace Nemobil.Planning.Contracts.Model
{
    /// <summary>Neutrale Planungsanfrage: die Eckdaten des Fahrtwunschs und die Suchparameter.
    /// <para>Die einzuplanenden Halte selbst stehen in <see cref="IPlanningItems.Items"/>. Für die
    /// Planung sind deren Orte und Zeitfenster maßgeblich; <see cref="Start"/>, <see cref="Target"/>,
    /// <see cref="Pickup"/> und <see cref="Dropoff"/> fassen den Wunsch zusammen und dienen der
    /// Einordnung, etwa für Diagnose oder Vorauswahl.</para></summary>
    public sealed class PlanningRequest
    {
        /// <summary>Gewünschter Einstiegsort.</summary>
        public required GeoPoint Start { get; init; }

        /// <summary>Gewünschter Zielort.</summary>
        public required GeoPoint Target { get; init; }

        /// <summary>Gewünschtes Einstiegs-Zeitfenster, falls angegeben.</summary>
        public TimeWindow? Pickup { get; init; }

        /// <summary>Gewünschtes Ankunfts-Zeitfenster, falls angegeben.</summary>
        public TimeWindow? Dropoff { get; init; }

        /// <summary>Kapazitätsbedarf der Anfrage.</summary>
        public required CapacityDemand Demand { get; init; }

        /// <summary>Ob der Fahrgast die Mitnahme weiterer Fahrgäste akzeptiert. Bei <c>false</c> darf
        /// während seiner Fahrt kein weiterer Fahrgast an Bord sein.</summary>
        public bool AllowCarpooling { get; init; }

        /// <summary>Vom Fahrgast tolerierte Abweichung vor bzw. nach dem gewünschten Zeitpunkt. Eine
        /// Implementierung kann sie nutzen, um zugesagte Zeitfenster zu bilden.</summary>
        public TimeSpan ToleratedDelayBefore { get; init; }

        /// <summary>Siehe <see cref="ToleratedDelayBefore"/>.</summary>
        public TimeSpan ToleratedDelayAfter { get; init; }

        /// <summary>Benötigte Fähigkeiten als Bezeichner (informativ). Verbindlich sind die numerischen
        /// Kennungen an den einzelnen Halten (<see cref="PlanningItem.Skills"/>).</summary>
        public IReadOnlyList<string> RequiredSkills { get; init; } = [];

        /// <summary>Zeitbereich, in dem die Anfrage bedient werden soll (vom frühesten Einstieg bis zur
        /// spätesten Ankunft).</summary>
        public required TimeWindow Schedule { get; init; }

        /// <summary>Höchstzahl der Vorschläge. Werte ≤ 0 bedeuten „keine Vorgabe“; die Implementierung
        /// wählt dann selbst eine Obergrenze.</summary>
        public int MaxProposals { get; init; }

        /// <summary>Suchradius in Metern: ein Fahrzeug kommt nur in Frage, wenn es (Standort bzw.
        /// Ausgangspunkt der Planung) höchstens so weit vom ersten einzuplanenden Halt entfernt ist
        /// (Luftlinie). Werte ≤ 0 bedeuten „keine Begrenzung“.</summary>
        public int MaxSearchRadiusMeters { get; init; }

        /// <summary>Fordert zusätzliche Diagnose an (z. B. ausführlichere Ausschlussgründe).</summary>
        public bool AnalyzeMode { get; init; }

        /// <summary>Korrelationskennung der Anfrage, z. B. zur Zuordnung konkurrierender Anfragen und
        /// ihrer Vorschläge.</summary>
        public required string CorrelationId { get; init; }
    }
}
