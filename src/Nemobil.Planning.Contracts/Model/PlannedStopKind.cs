namespace Nemobil.Planning.Contracts.Model
{
    /// <summary>Art eines geplanten Halts. Neutral formuliert — bewusst unabhängig von
    /// den Halt-Typen einer bestimmten Implementierung. Nicht jede Art ist für jeden Aufrufer
    /// anwendbar; siehe die Konformitätsregel <c>[G3-Halt-Art]</c>.</summary>
    public enum PlannedStopKind
    {
        None = 0,
        Pickup = 1,
        Dropoff = 2,
        Relocation = 3,
        Charging = 4,
        Parking = 5,
        BusinessTrip = 6,
        Maintenance = 7,
        Absence = 8,
        Depot = 9,
        Chaining = 10,
        Unchaining = 11,

        /// <summary>Halt, dessen Art der Vertrag nicht benennt: das Planungsverfahren führt ihn
        /// unverändert mit, kann ihn aber keiner der Arten oben zuordnen (typisch: ein bereits
        /// bestehender Halt, den der Aufrufer selbst angelegt hat und den das Verfahren nur
        /// weiterreicht).
        /// <para>Ein Aufrufer darf einen solchen Halt ausschließlich über <see cref="PlannedStop.Key"/>
        /// mit einem bereits vorhandenen Halt korrelieren und dessen Ergebnisdaten aktualisieren.
        /// Er darf ihn NICHT neu anlegen — die Art wäre geraten. Findet er keinen passenden Halt,
        /// lässt er ihn weg.</para>
        /// <para><b>Zusicherung (SOLL):</b> ein Halt dieser Art SOLL einen <see cref="PlannedStop.Key"/>
        /// tragen. Ohne Schlüssel ist er unbrauchbar — er ist weder korrelierbar noch anlegbar;
        /// <b>der Aufrufer verwirft ihn dann und meldet ihn als Diagnose</b>. Das ist ausdrücklich
        /// erlaubtes Verhalten auf beiden Seiten: wer Fahrpläne erzeugt, darf einen schlüssellosen
        /// Halt dieser Art durchreichen, statt ihn stillschweigend zu unterdrücken — der Aufrufer
        /// erfährt so überhaupt, dass ein unbrauchbarer Halt entstanden ist. Kein SOLL im Sinne von
        /// „egal": ein Erzeuger, der den Schlüssel kennt, liefert ihn.</para></summary>
        Other = 12,
    }
}
