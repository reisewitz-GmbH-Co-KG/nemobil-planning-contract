using Nemobil.Planning.Conformance;
using Nemobil.Planning.Contracts;

namespace Nemobil.Planning.Reference.Tests
{
    /// <summary>Die Referenz-Implementierung gegen die vollständige Konformitätssuite.</summary>
    public sealed class ReferenceEngineConformanceTests : PlanningEngineConformanceTests
    {
        protected override IPlanningEngine CreateEngine() => new ReferencePlanningEngine();
    }

    /// <summary>Dieselbe Suite mit auf einen anderen Tag verschobenen Prüffällen: die Verschiebung
    /// darf an der Konformität nichts ändern.</summary>
    public sealed class ReferenceEngineConformanceShiftedDayTests : PlanningEngineConformanceTests
    {
        protected override DateOnly? ScenarioDay => new DateOnly(2031, 3, 17);

        protected override IPlanningEngine CreateEngine() => new ReferencePlanningEngine();
    }
}
