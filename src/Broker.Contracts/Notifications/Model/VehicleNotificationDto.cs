using System.Text.Json.Serialization;

namespace Broker.Contracts.Notifications.Model
{
    /// <summary>
    /// Gemeinsame Basis fuer Fahrzeug-Notifications (Cab, Pro). Selbst kein
    /// <c>INotification</c>, damit Mediator's polymorphic dispatch keinen
    /// Handler doppelt feuert (siehe <see cref="CabNotificationDto"/> /
    /// <see cref="ProNotificationDto"/>).
    /// </summary>
    public class VehicleNotificationDto : NgsiLdBaseType
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "Vehicle";

        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("dataProvider")]
        public string DataProvider { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("vehicleType")]
        public string VehicleType { get; set; }

        [JsonPropertyName("category")]
        public List<string> Category { get; set; } = new List<string>();

        [JsonPropertyName("location")]
        public GeoProperty Location { get; set; }

        [JsonPropertyName("previousLocation")]
        public GeoProperty PreviousLocation { get; set; }

        [JsonPropertyName("speed")]
        public double Speed { get; set; }

        [JsonPropertyName("heading")]
        public float Heading { get; set; }

        [JsonPropertyName("cargoWeight")]
        public double CargoWeight { get; set; }

        [JsonPropertyName("vehicleIdentificationNumber")]
        public string VehicleIdentificationNumber { get; set; }

        [JsonPropertyName("fleetVehicleId")]
        public string FleetVehicleId { get; set; }

        [JsonPropertyName("dateVehicleFirstRegistered")]
        public DateTime? DateVehicleFirstRegistered { get; set; }

        [JsonPropertyName("dateFirstUsed")]
        public DateTime? DateFirstUsed { get; set; }

        [JsonPropertyName("purchaseDate")]
        public DateTime? PurchaseDate { get; set; }

        [JsonPropertyName("mileageFromOdometer")]
        public double MileageFromOdometer { get; set; }

        [JsonPropertyName("vehicleConfiguration")]
        public string VehicleConfiguration { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }

        [JsonPropertyName("owner")]
        public List<string> Owner { get; set; }

        [JsonPropertyName("feature")]
        public List<string> Feature { get; set; }

        [JsonPropertyName("serviceProvided")]
        public List<string> ServiceProvided { get; set; }

        [JsonPropertyName("vehicleSpecialUsage")]
        public List<string> VehicleSpecialUsage { get; set; }

        [JsonPropertyName("refVehicleModel")]
        public string RefVehicleModel { get; set; }

        [JsonPropertyName("areaServed")]
        public string AreaServed { get; set; }

        [JsonPropertyName("serviceStatus")]
        public string ServiceStatus { get; set; }

        [JsonPropertyName("dateModified")]
        public DateTime? DateModified { get; set; }

        [JsonPropertyName("dateCreated")]
        public DateTime? DateCreated { get; set; }

        // "features" kommt als Objekt mit "dataset" und "@none" (NGSI-LD-Dataset-Form),
        // daher die verschachtelten Klassen unten.
        [JsonPropertyName("features")]
        public FeaturesContainer Features { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("shortId")]
        public float ShortId { get; set; }

        [JsonPropertyName("doorLock")]
        public string DoorLock { get; set; }

        [JsonPropertyName("deviation")]
        public double Deviation { get; set; }

        [JsonPropertyName("fuelLevel")]
        public float FuelLevel { get; set; }

        [JsonPropertyName("acceleration")]
        public List<double> Acceleration { get; set; }

        [JsonPropertyName("batteryPower")]
        public float BatteryPower { get; set; }

        [JsonPropertyName("licensePlate")]
        public string LicensePlate { get; set; }

        [JsonPropertyName("stateADStack")]
        public string StateADStack { get; set; }

        [JsonPropertyName("chargedEnergy")]
        public double ChargedEnergy { get; set; }

        [JsonPropertyName("chargingPower")]
        public double ChargingPower { get; set; }

        [JsonPropertyName("hydrogenTanks")]
        public List<string> HydrogenTanks { get; set; } = [];

        [JsonPropertyName("stateCoupling")]
        public string StateCoupling { get; set; }

        [JsonPropertyName("suppliedPower")]
        public double SuppliedPower { get; set; }

        [JsonPropertyName("batteryCurrent")]
        public double BatteryCurrent { get; set; }

        [JsonPropertyName("batteryVoltage")]
        public double BatteryVoltage { get; set; }

        [JsonPropertyName("consumedEnergy")]
        public double ConsumedEnergy { get; set; }

        [JsonPropertyName("remainingRange")]
        public double RemainingRange { get; set; }

        [JsonPropertyName("suppliedEnergy")]
        public double SuppliedEnergy { get; set; }

        [JsonPropertyName("chainedPosition")]
        public float ChainedPosition { get; set; }

        [JsonPropertyName("chainedVehicles")]
        public float ChainedVehicles { get; set; }

        [JsonPropertyName("inverterCurrent")]
        public double InverterCurrent { get; set; }

        [JsonPropertyName("nextStopArrival")]
        public string NextStopArrival { get; set; }

        [JsonPropertyName("stateOfSchedule")]
        public string StateOfSchedule { get; set; }

        [JsonPropertyName("massFlowHydrogen")]
        public double MassFlowHydrogen { get; set; }

        [JsonPropertyName("pressureHydrogen")]
        public double PressureHydrogen { get; set; }

        [JsonPropertyName("toBeChainedVehicles")]
        public List<string> ToBeChainedVehicles { get; set; }

        [JsonPropertyName("vehicleScheduleGuid")]
        public string VehicleScheduleGuid { get; set; }

        [JsonPropertyName("passengerInformation")]
        public string PassengerInformation { get; set; }

        // Kapazitaeten des Fahrzeugs (Sitze, Kindersitze, Gepaeck). Nullable, damit reine
        // Status-Updates (Position/Energie) ohne diese Felder die hinterlegte Kapazitaet
        // nicht ueberschreiben.
        [JsonPropertyName("seats")]
        public float? Seats { get; set; }

        [JsonPropertyName("childSeats")]
        public float? ChildSeats { get; set; }

        [JsonPropertyName("luggage")]
        public float? Luggage { get; set; }

        [JsonPropertyName("concentrationHydrogen")]
        public double ConcentrationHydrogen { get; set; }

        [JsonPropertyName("bearing")]
        public float Bearing { get; set; }

        [JsonPropertyName("consumption")]
        public double Consumption { get; set; }

        [JsonPropertyName("batteryLevel")]
        public double BatteryLevel { get; set; }

        [JsonPropertyName("nextStopLocation")]
        public GeoProperty NextStopLocation { get; set; }
    }

    public class FeaturesContainer
    {
        [JsonPropertyName("dataset")]
        public FeaturesDataset Dataset { get; set; }
    }

    public class FeaturesDataset
    {
        [JsonPropertyName("@none")]
        public FeatureDetail None { get; set; }
    }

    public class FeatureDetail
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class SensorsContainer
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
    }
}
