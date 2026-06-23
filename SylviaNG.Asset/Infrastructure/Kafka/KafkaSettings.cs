namespace SylviaNG.Assets.Infrastructure.Kafka
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public string GroupId { get; set; } = "sylviang-asset-employee-sync";
    }
}
