namespace Nemobil.Planning.Reference
{
    /// <summary>Diagnosegründe der Referenz-Implementierung. Sie erscheinen als
    /// <c>ExclusionInfo.Reason</c> je ausgeschlossenem Fahrzeug und zusammengefasst in
    /// <c>PlanningResult.Reason</c>, wenn kein Vorschlag entsteht.</summary>
    public static class ReferenceExclusionReasons
    {
        /// <summary>Der Flottenzustand enthält kein Fahrzeug.</summary>
        public const string NoVehicles = "NoVehicles";

        /// <summary>Kein Fahrzeug war einplanbar; die Einzelgründe stehen in den Ausschlüssen.</summary>
        public const string NoFeasibleVehicle = "NoFeasibleVehicle";

        /// <summary>Die Eingaben sind unvollständig oder widersprüchlich.</summary>
        public const string InvalidInput = "InvalidInput";

        /// <summary>Das Fahrzeug führt eine benötigte Fähigkeit nicht.</summary>
        public const string MissingSkills = "MissingSkills";

        /// <summary>Das Fahrzeug steht weiter als der Suchradius vom ersten einzuplanenden Halt entfernt.</summary>
        public const string OutOfRange = "OutOfRange";

        /// <summary>Für das Fahrzeug ist weder ein Standort noch ein Halt mit Ort bekannt.</summary>
        public const string UnknownPosition = "UnknownPosition";

        /// <summary>Schon der bestehende Fahrplan ist unter dem Streckenmodell nicht einhaltbar.</summary>
        public const string ExistingScheduleInfeasible = "ExistingScheduleInfeasible";

        /// <summary>Kein Einfügen hält ein Zeitfenster ein.</summary>
        public const string TimeWindow = "TimeWindow";

        /// <summary>Kein Einfügen bleibt innerhalb der Schicht.</summary>
        public const string Shift = "Shift";

        /// <summary>Kein Einfügen kommt mit der verfügbaren Energie aus.</summary>
        public const string Energy = "Energy";

        /// <summary>Kein Einfügen hält die Kapazitäten ein.</summary>
        public const string Capacity = "Capacity";

        /// <summary>Kein Einfügen ist ohne Mitnahme weiterer Fahrgäste möglich, obwohl die Anfrage keine
        /// Mitnahme erlaubt.</summary>
        public const string Carpooling = "Carpooling";
    }
}
