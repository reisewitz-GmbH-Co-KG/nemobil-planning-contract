namespace Nemobil.Planning.Contracts.Model
{
    /// <summary>Ein einzuplanender Halt einer Anfrage (Einstieg oder Ausstieg).
    /// <para><b>Was:</b> Ort, Zeitfenster, Bedienzeit, Belegung und Reihenfolge-Vorgaben eines Halts,
    /// den die Planung in einen Fahrplan einfügen soll. Eine Anfrage besteht aus mindestens einem
    /// Ausstieg und in der Regel einem Einstieg; mehrere Ein- oder Ausstiege (z. B. eine Gruppe, die an
    /// mehreren Stationen zusteigt) sind zulässig.</para>
    /// <para><b>Warum:</b> die Planung muss jeden Halt einzeln kennen, um ihn einfügen und seinen
    /// Schlüssel im Ergebnis zurückmelden zu können.</para></summary>
    public sealed class PlanningItem
    {
        /// <summary>Eindeutiger Schlüssel des Halts. Die Planung übernimmt ihn unverändert als
        /// <see cref="PlannedStop.Key"/> in den Fahrplan.</summary>
        public required string Key { get; init; }

        /// <summary>Art des Halts: <see cref="PlannedStopKind.Pickup"/> (Einstieg, belegt Kapazität) oder
        /// <see cref="PlannedStopKind.Dropoff"/> (Ausstieg, gibt sie frei).</summary>
        public required PlannedStopKind Kind { get; init; }

        /// <summary>Ort des Halts (WGS84).</summary>
        public required GeoPoint Location { get; init; }

        /// <summary>Zeitfenster, in dem die Bedienung am Halt beginnen muss (zuzüglich Toleranz des
        /// Fensters). Ohne Angabe ist der Halt zeitlich nur durch die Schicht und die Reihenfolge
        /// begrenzt.</summary>
        public TimeWindow? TimeWindow { get; init; }

        /// <summary>Dauer der Bedienung am Halt in Sekunden (Ein- bzw. Ausstiegszeit).</summary>
        public int ServiceSeconds { get; init; }

        /// <summary>Schlüssel anderer Halte derselben Anfrage, die im Fahrplan <b>vor</b> diesem Halt
        /// bedient werden müssen (z. B. der Einstieg vor dem Ausstieg).</summary>
        public IReadOnlyList<string> Predecessors { get; init; } = [];

        /// <summary>Belegung durch den Fahrgast, nach den Schlüssel-Konventionen von
        /// <see cref="PlannedStop.Quantities"/>. Werte sind positiv: ein Einstieg belegt sie, ein Ausstieg
        /// gibt sie frei. Über alle Halte einer Anfrage summieren sich Einstiege und Ausstiege zu denselben
        /// Werten — bei einem Einstieg und einem Ausstieg tragen beide also denselben Wert.</summary>
        public IReadOnlyDictionary<string, float> Quantities { get; init; } = new Dictionary<string, float>(StringComparer.Ordinal);

        /// <summary>Benötigte Fähigkeiten als numerische Kennungen (siehe <see cref="VehicleState.Skills"/>).</summary>
        public IReadOnlyList<int> Skills { get; init; } = [];

        /// <summary>Fachliche Priorität des Halts (siehe <see cref="PlannedStop.Priority"/>).</summary>
        public int Priority { get; init; }
    }
}
