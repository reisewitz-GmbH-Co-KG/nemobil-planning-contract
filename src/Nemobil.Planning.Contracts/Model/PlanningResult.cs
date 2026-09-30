namespace Nemobil.Planning.Contracts.Model
{
    /// <summary>Neutrales Planungsergebnis. Leeres Ergebnis ist gültig und trägt einen Diagnosegrund.</summary>
    public sealed class PlanningResult
    {
        /// <summary>
        /// Gibt an, ob die Planungs-Engine den Lauf erfolgreich abgeschlossen hat.
        /// <para>
        /// <b>Wichtig:</b> <c>Successful == true</c> impliziert <b>nicht</b>, dass <see cref="Proposals"/>
        /// nicht leer ist. Die Engine kann einen erfolgreichen Lauf mit null Vorschlägen melden —
        /// etwa wenn keine Fahrzeuge erreichbar waren oder keines die Randbedingungen erfüllte. In diesem Fall
        /// enthält <see cref="Reason"/> einen diagnostischen Hinweis auf den Ausschlussgrund.
        /// </para>
        /// </summary>
        public bool Successful { get; init; }
        public IReadOnlyList<PlanningProposal> Proposals { get; init; } = [];
        public IReadOnlyList<ExclusionInfo> Exclusions { get; init; } = [];
        public string? Reason { get; init; }
    }
}
