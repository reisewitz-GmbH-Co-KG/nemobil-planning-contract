using Nemobil.Planning.Contracts;
using Nemobil.Planning.Contracts.Model;
using Xunit;

namespace Nemobil.Planning.Conformance
{
    /// <summary>Ausführbare Konformitätssuite gegen <see cref="IPlanningEngine"/>.
    /// <para><b>Verwendung:</b> in einem xUnit-Testprojekt eine Klasse davon ableiten und
    /// <see cref="CreateEngine"/> überschreiben. Jede Prüfgruppe des Konformitäts-Testkatalogs läuft
    /// dann einmal je mitgeliefertem Prüffall; ein roter Test nennt die Verstöße im Klartext.</para>
    /// <code>
    /// public sealed class MeineEngineConformanceTests : PlanningEngineConformanceTests
    /// {
    ///     protected override IPlanningEngine CreateEngine() => new MeineEngine();
    /// }
    /// </code></summary>
    public abstract class PlanningEngineConformanceTests
    {
        /// <summary>Kennungen aller mitgelieferten Prüffälle, als xUnit-Datenquelle.</summary>
        public static IEnumerable<object[]> Scenarios => ConformanceScenarios.Ids.Select(id => new object[] { id });

        /// <summary>Tag, auf den die Prüffälle verschoben werden. Ohne Überschreiben gilt der nominale
        /// Tag der Fixtures (<see cref="ConformanceScenarios.NominalDay"/>).</summary>
        protected virtual DateOnly? ScenarioDay => null;

        /// <summary>G1 — der Prüffall löst die erwartete Wirkungsart aus.</summary>
        [Theory]
        [MemberData(nameof(Scenarios), MemberType = typeof(PlanningEngineConformanceTests))]
        public async Task G1_EingangWirkung(string scenarioId)
        {
            var run = await RunAsync(scenarioId);

            AssertConformant(run.Scenario, ConformanceChecks.CheckExpectation(run.Scenario, run.Result));
        }

        /// <summary>G2 — jeder Vorschlag hält die harten Randbedingungen ein.</summary>
        [Theory]
        [MemberData(nameof(Scenarios), MemberType = typeof(PlanningEngineConformanceTests))]
        public async Task G2_Invarianten(string scenarioId)
        {
            var run = await RunAsync(scenarioId);

            AssertConformant(run.Scenario, ConformanceChecks.CheckInvariants(run.Scenario, run.Result));
        }

        /// <summary>G3 — Obergrenze, Diagnose, Buchbarkeit, Seiteneffektfreiheit.</summary>
        [Theory]
        [MemberData(nameof(Scenarios), MemberType = typeof(PlanningEngineConformanceTests))]
        public async Task G3_Vertrag(string scenarioId)
        {
            var run = await RunAsync(scenarioId);

            var violations = ConformanceChecks.CheckContract(run.Scenario, run.Result)
                .Concat(ConformanceChecks.CheckInputsUnchanged(run.SnapshotBefore, run.SnapshotAfter))
                .ToList();

            AssertConformant(run.Scenario, violations);
        }

        /// <summary>G4 — jeder Fahrplan ist wohlgeformt, energetisch konsistent und vollständig.</summary>
        [Theory]
        [MemberData(nameof(Scenarios), MemberType = typeof(PlanningEngineConformanceTests))]
        public async Task G4_Fahrplan(string scenarioId)
        {
            var run = await RunAsync(scenarioId);

            AssertConformant(run.Scenario, ConformanceChecks.CheckSchedules(run.Scenario, run.Result));
        }

        /// <summary>Liefert die zu prüfende Implementierung. Wird je Test neu aufgerufen.</summary>
        protected abstract IPlanningEngine CreateEngine();

        /// <summary>Führt einen Prüffall gegen die Implementierung aus.</summary>
        protected async Task<ConformanceRun> RunAsync(string scenarioId)
        {
            var scenario = ConformanceScenarios.Get(scenarioId, ScenarioDay);
            var fleet = scenario.CreateFleet();
            var items = scenario.CreateItems();

            var before = ConformanceChecks.Snapshot(scenario.Request, fleet, items);
            var result = await CreateEngine().PlanAsync(scenario.Request, fleet, items, CancellationToken.None);
            var after = ConformanceChecks.Snapshot(scenario.Request, fleet, items);

            Assert.NotNull(result);

            return new ConformanceRun(scenario, result, before, after);
        }

        private static void AssertConformant(ConformanceScenario scenario, IReadOnlyList<string> violations)
        {
            if (violations.Count == 0)
            {
                return;
            }

            Assert.Fail($"Prüffall {scenario}: {violations.Count} Verstoß/Verstöße{Environment.NewLine}- {string.Join(Environment.NewLine + "- ", violations)}");
        }
    }

    /// <summary>Ergebnis eines Prüffall-Laufs.</summary>
    /// <param name="Scenario">Der Prüffall.</param>
    /// <param name="Result">Das Planungsergebnis der Implementierung.</param>
    /// <param name="SnapshotBefore">Eingaben vor dem Aufruf, serialisiert.</param>
    /// <param name="SnapshotAfter">Eingaben nach dem Aufruf, serialisiert.</param>
    public sealed record ConformanceRun(ConformanceScenario Scenario, PlanningResult Result, string SnapshotBefore, string SnapshotAfter);
}
