namespace Nemobil.Planning.Contracts.Model
{
    /// <summary>Zeitfenster [Min, Max] mit optionaler Toleranz. Neutral.
    /// <para>
    /// Die Toleranz benennt, um wie viele <b>Sekunden</b> der Bedienbeginn vor <see cref="Min"/>
    /// bzw. nach <see cref="Max"/> noch als zulässig gilt; bei einer Schicht gilt sie für den Beginn
    /// des ersten und das Ende des letzten Halts. Sie existiert, weil
    /// vereinbarte Termine in der Praxis einen Spielraum haben — ein Aufrufer, der Zeitfenster
    /// prüft, muss denselben Spielraum anwenden wie derjenige, der den Fahrplan erzeugt hat,
    /// sonst verwirft er zulässige Fahrpläne.
    /// </para>
    /// <para>
    /// <see cref="ToleranceIsSet"/> unterscheidet „keine Toleranz vereinbart" von
    /// „Toleranz null" — beide führen zu denselben Grenzen, aber nur im ersten Fall darf ein
    /// Aufrufer eine eigene Vorgabe einsetzen.
    /// </para>
    /// </summary>
    public readonly record struct TimeWindow(
        DateTime Min,
        DateTime Max,
        int ToleranceBefore = 0,
        int ToleranceAfter = 0,
        bool ToleranceIsSet = false);
}
