using System.Text.Json.Serialization;
using Mediator;

namespace Broker.Contracts.Notifications.Model
{
    /// <summary>
    /// The create cab schedule.
    /// </summary>
    public sealed class CreateCabScheduleDto
        : NgsiLdBaseType, INotification
    {
        /// <summary>
        /// Gets the guid.
        /// </summary>
        [JsonPropertyName("id")]
        public string Guid { get; internal set; }

        /// <summary>
        /// Gets the proposal guid.
        /// </summary>
        [JsonPropertyName("proposalGuid")]
        public string ProposalGuid { get; internal set; }

        /// <summary>
        /// Gets the schedule end.
        /// </summary>
        [JsonPropertyName("scheduleEnd")]
        public DateTime ScheduleEnd { get; internal set; }

        /// <summary>
        /// Gets the schedule start.
        /// </summary>
        [JsonPropertyName("scheduleStart")]
        public DateTime ScheduleStart { get; internal set; }

        /// <summary>
        /// Gets the default energy consuption per M.
        /// </summary>
        [JsonPropertyName("defaultEnergyConsuptionPerM")]
        public float DefaultEnergyConsuptionPerM { get; internal set; }

        /// <summary>
        /// Gets the label.
        /// </summary>
        [JsonPropertyName("label")]
        public string Label { get; internal set; }

        /// <summary>
        /// Gets the operating company.
        /// </summary>
        [JsonPropertyName("operatingCompany")]
        public string OperatingCompany { get; internal set; }

        /// <summary>
        /// Gets the total energy capacity.
        /// </summary>
        [JsonPropertyName("totalEnergyCapacity")]
        public int TotalEnergyCapacity { get; internal set; }

        /// <summary>
        /// Gets the vehicle type.
        /// </summary>
        [JsonPropertyName("vehicleType")]
        public string VehicleType { get; internal set; }
    }
}
