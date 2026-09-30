using Nemobil.Planning.Conformance;
using Nemobil.Planning.Contracts.Model;

namespace Nemobil.Planning.Reference.Tests
{
    /// <summary>Verhalten der Referenz-Implementierung, das über die Konformität hinausgeht: welche
    /// Diagnose sie liefert und welche Entscheidung sie in eindeutigen Fällen trifft. Diese Erwartungen
    /// gelten nur für die Referenz, nicht für andere Implementierungen.</summary>
    public class ReferencePlanningEngineTests
    {
        private static async Task<PlanningResult> PlanAsync(ConformanceScenario scenario, ReferencePlanningEngine? engine = null)
        {
            engine ??= new ReferencePlanningEngine();
            return await engine.PlanAsync(scenario.Request, scenario.CreateFleet(), scenario.CreateItems(), CancellationToken.None);
        }

        [Theory]
        [InlineData("G1-03", ReferenceExclusionReasons.OutOfRange)]
        [InlineData("G1-04", ReferenceExclusionReasons.Shift)]
        [InlineData("G2-01", ReferenceExclusionReasons.Capacity)]
        [InlineData("G2-03", ReferenceExclusionReasons.Energy)]
        [InlineData("G3-01", ReferenceExclusionReasons.NoVehicles)]
        public async Task LeeresErgebnis_NenntDenAusschlussgrund(string scenarioId, string expectedReason)
        {
            var result = await PlanAsync(ConformanceScenarios.Get(scenarioId));

            Assert.True(result.Successful);
            Assert.Empty(result.Proposals);
            Assert.Contains(expectedReason, result.Reason, StringComparison.Ordinal);
        }

        [Fact]
        public async Task EinzigesFreiesFahrzeug_BekommtDenVorschlagMitVollstaendigemFahrplan()
        {
            var result = await PlanAsync(ConformanceScenarios.Get("G1-01"));

            var proposal = Assert.Single(result.Proposals);
            Assert.Equal("veh-1", proposal.VehicleId);
            Assert.Equal("tour-1", proposal.TourId);
            Assert.Equal("g1-01_Pickup", proposal.RequestedPickupKey);
            Assert.Equal("g1-01_Dropoff", proposal.RequestedDropoffKey);
            Assert.Equal(
                ["tour-1_Start", "g1-01_Pickup", "g1-01_Dropoff", "tour-1_End"],
                proposal.Schedules[0].Stops.Select(s => s.Key));
            Assert.True(proposal.PassengerDistanceMeters > 0);
            Assert.True(proposal.Efficiency > 0);
        }

        [Fact]
        public async Task Obergrenze_BegrenztUndSortiertNachZusatzstrecke()
        {
            var result = await PlanAsync(ConformanceScenarios.Get("G1-02"));

            Assert.Equal(2, result.Proposals.Count);
            Assert.True(result.Proposals[0].Efficiency <= result.Proposals[1].Efficiency);
        }

        [Fact]
        public async Task OhneMitnahme_WeichtAufDasFreieFahrzeugAus()
        {
            var result = await PlanAsync(ConformanceScenarios.Get("G1-05"));

            Assert.All(result.Proposals, p => Assert.Equal("veh-b", p.VehicleId));
            Assert.Contains(result.Exclusions, e => e.Reason == ReferenceExclusionReasons.Carpooling || e.Reason == ReferenceExclusionReasons.TimeWindow);
        }

        [Fact]
        public async Task FixierteHalte_BleibenUnveraendertAmAnfang()
        {
            var scenario = ConformanceScenarios.Get("G4-02");
            var result = await PlanAsync(scenario);

            var stops = Assert.Single(result.Proposals).Schedules[0].Stops;
            var input = scenario.Vehicles[0].Stops;

            Assert.Equal(input[0].Key, stops[0].Key);
            Assert.Equal(input[1].Key, stops[1].Key);
            Assert.Equal(input[1].Departure, stops[1].Departure);
            Assert.Contains(stops, s => s.Key == "w1_Dropoff");
        }

        [Fact]
        public async Task MehrereEinstiege_HaltenDieVorgaengerReihenfolgeEin()
        {
            var result = await PlanAsync(ConformanceScenarios.Get("G1-06"));

            var keys = Assert.Single(result.Proposals).Schedules[0].Stops.Select(s => s.Key).ToList();
            Assert.True(keys.IndexOf("g1-06_Pickup_A") < keys.IndexOf("g1-06_Pickup_B"));
            Assert.True(keys.IndexOf("g1-06_Pickup_B") < keys.IndexOf("g1-06_Dropoff"));
        }

        [Fact]
        public async Task Faehigkeit_WaehltDasPassendeFahrzeug()
        {
            var result = await PlanAsync(ConformanceScenarios.Get("G2-05"));

            Assert.NotEmpty(result.Proposals);
            Assert.All(result.Proposals, p => Assert.Equal("veh-2", p.VehicleId));
            Assert.Contains(result.Exclusions, e => e.Reason == ReferenceExclusionReasons.MissingSkills);
        }

        [Fact]
        public async Task EngesZeitfenster_WartetBisZumFensterbeginn()
        {
            var scenario = ConformanceScenarios.Get("G2-04");
            var result = await PlanAsync(scenario);

            var pickup = Assert.Single(result.Proposals).Schedules[0].Stops.Single(s => s.Key == "g2-04_Pickup");
            Assert.Equal(scenario.Items[0].TimeWindow!.Value.Min, pickup.ServiceStart);
            Assert.True(pickup.Arrival < pickup.ServiceStart);
        }

        [Fact]
        public async Task OhneHalte_IstUngueltigeEingabe()
        {
            var scenario = ConformanceScenarios.Get("G1-01");
            var engine = new ReferencePlanningEngine();

            var result = await engine.PlanAsync(scenario.Request, scenario.CreateFleet(), new PlanningItemList([]), CancellationToken.None);

            Assert.False(result.Successful);
            Assert.Empty(result.Proposals);
            Assert.StartsWith(ReferenceExclusionReasons.InvalidInput, result.Reason, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ZyklischeVorgaenger_SindUngueltigeEingabe()
        {
            var scenario = ConformanceScenarios.Get("G1-01");
            var items = scenario.Items
                .Select(i => new PlanningItem
                {
                    Key = i.Key,
                    Kind = i.Kind,
                    Location = i.Location,
                    TimeWindow = i.TimeWindow,
                    ServiceSeconds = i.ServiceSeconds,
                    Quantities = i.Quantities,
                    Predecessors = [scenario.Items.Single(o => o.Key != i.Key).Key],
                })
                .ToList();

            var result = await new ReferencePlanningEngine().PlanAsync(scenario.Request, scenario.CreateFleet(), new PlanningItemList(items), CancellationToken.None);

            Assert.False(result.Successful);
            Assert.Contains("Zyklus", result.Reason, StringComparison.Ordinal);
        }

        [Fact]
        public async Task FehlendeFaehigkeit_SchliesstDasFahrzeugAus()
        {
            var scenario = ConformanceScenarios.Get("G1-01");
            var items = scenario.Items
                .Select(i => new PlanningItem
                {
                    Key = i.Key,
                    Kind = i.Kind,
                    Location = i.Location,
                    TimeWindow = i.TimeWindow,
                    ServiceSeconds = i.ServiceSeconds,
                    Quantities = i.Quantities,
                    Predecessors = i.Predecessors,
                    Skills = [42],
                })
                .ToList();

            var result = await new ReferencePlanningEngine().PlanAsync(scenario.Request, scenario.CreateFleet(), new PlanningItemList(items), CancellationToken.None);

            Assert.Empty(result.Proposals);
            Assert.Contains(ReferenceExclusionReasons.MissingSkills, result.Reason, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Abbruch_WirdBeachtet()
        {
            var scenario = ConformanceScenarios.Get("G1-02");
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => new ReferencePlanningEngine().PlanAsync(scenario.Request, scenario.CreateFleet(), scenario.CreateItems(), cancellation.Token));
        }

        [Fact]
        public void StreckenModell_LuftlinieMalUmwegfaktor()
        {
            var model = new StraightLineTravelModel(speedKmh: 36, detourFactor: 1.0);

            // Ein Breitengrad-Hundertstel sind rund 1112 m; bei 36 km/h = 10 m/s rund 112 s.
            var estimate = model.Estimate(new GeoPoint(50.00, 10.0), new GeoPoint(50.01, 10.0));

            Assert.InRange(estimate.DistanceMeters, 1110, 1113);
            Assert.InRange(estimate.DrivingSeconds, 111, 112);
        }
    }
}
