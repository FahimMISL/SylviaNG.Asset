using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SylviaNG.Assets.Domain.Entities;
using SylviaNG.Assets.Infrastructure.Data;
using System.Text.Json;

namespace SylviaNG.Assets.Infrastructure.Kafka
{
    /// <summary>
    /// Consumes Employee and JobInformation events from Kafka and syncs data
    /// into the Asset's local Employees table.
    /// </summary>
    public class EmployeeEventConsumer : BackgroundService
    {
        private readonly KafkaSettings _settings;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmployeeEventConsumer> _logger;

        private const string EMPLOYEE_TOPIC = "sylviang-employee.employee.events";
        private const string JOBINFORMATION_TOPIC = "sylviang-employee.jobinformation.events";

        public EmployeeEventConsumer(
            IOptions<KafkaSettings> options,
            IServiceProvider serviceProvider,
            ILogger<EmployeeEventConsumer> logger)
        {
            _settings = options.Value;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Employee sync consumer starting...");

            var config = new ConsumerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                GroupId = _settings.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => _logger.LogError("Kafka error: {Reason}", e.Reason))
                .Build();

            consumer.Subscribe(new[] { EMPLOYEE_TOPIC, JOBINFORMATION_TOPIC });
            _logger.LogInformation("Subscribed to topics: {Topics}", string.Join(", ", EMPLOYEE_TOPIC, JOBINFORMATION_TOPIC));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(1));
                    if (result == null) continue;

                    await ProcessMessageAsync(result.Message.Value, stoppingToken);
                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error");
                    await Task.Delay(5000, stoppingToken);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize event message — skipping");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing employee event");
                }
            }

            consumer.Close();
        }

        private async Task ProcessMessageAsync(string payload, CancellationToken ct)
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var entityName = root.GetProperty("entityName").GetString();
            var action = root.GetProperty("action").GetString();

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

            switch (entityName)
            {
                case "Employee":
                    await HandleEmployeeEventAsync(action, root, dbContext, ct);
                    break;

                case "JobInformation":
                    await HandleJobInformationEventAsync(action, root, dbContext, ct);
                    break;

                default:
                    _logger.LogWarning("Unknown entity: {EntityName}", entityName);
                    break;
            }
        }

        private async Task HandleEmployeeEventAsync(string? action, JsonElement root, ApplicationDBContext dbContext, CancellationToken ct)
        {
            var employeeId = root.GetProperty("employeeId").GetInt64();

            switch (action)
            {
                case "CREATED":
                case "UPDATED":
                    var firstName = root.TryGetProperty("firstName", out var fn) ? fn.GetString() : null;
                    var lastName = root.TryGetProperty("lastName", out var ln) ? ln.GetString() : null;
                    var employeeName = $"{firstName} {lastName}".Trim();

                    var existing = await dbContext.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, ct);

                    if (existing != null)
                    {
                        existing.EmployeeName = employeeName;
                    }
                    else
                    {
                        dbContext.Employees.Add(new Employee
                        {
                            EmployeeId = employeeId,
                            EmployeeName = employeeName
                        });
                    }

                    await dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("Synced Employee {EmployeeId} ({Action})", employeeId, action);
                    break;

                case "DELETED":
                    var toDelete = await dbContext.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, ct);

                    if (toDelete != null)
                    {
                        dbContext.Employees.Remove(toDelete);
                        await dbContext.SaveChangesAsync(ct);
                    }

                    _logger.LogInformation("Deleted Employee {EmployeeId}", employeeId);
                    break;
            }
        }

        private async Task HandleJobInformationEventAsync(string? action, JsonElement root, ApplicationDBContext dbContext, CancellationToken ct)
        {
            var employeeId = root.TryGetProperty("employeeId", out var eid) ? eid.GetInt64() : (long?)null;
            if (employeeId == null)
            {
                _logger.LogWarning("JobInformation event missing EmployeeId — skipping");
                return;
            }

            switch (action)
            {
                case "CREATED":
                case "UPDATED":
                    var employeeCode = root.TryGetProperty("employeeCode", out var ec) ? ec.GetString() : null;
                    var departmentId = root.TryGetProperty("departmentId", out var dept) && dept.ValueKind != JsonValueKind.Null ? dept.GetInt64() : (long?)null;
                    var designationId = root.TryGetProperty("designationId", out var desig) && desig.ValueKind != JsonValueKind.Null ? desig.GetInt64() : (long?)null;
                    var siteId = root.TryGetProperty("siteId", out var site) && site.ValueKind != JsonValueKind.Null ? site.GetInt64() : (long?)null;
                    var rfidNumber = root.TryGetProperty("rfidNumber", out var rfid) ? rfid.GetString() : null;
                    var rfidValue = long.TryParse(rfidNumber, out var parsedRfid) ? (long?)parsedRfid : null;

                    var existing = await dbContext.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeId == employeeId.Value, ct);

                    if (existing != null)
                    {
                        existing.EmployeeCode = employeeCode;
                        existing.DepartmentId = departmentId;
                        existing.DesignatioId = designationId;
                        existing.SiteId = siteId;
                        existing.RFId = rfidValue;
                    }
                    else
                    {
                        dbContext.Employees.Add(new Employee
                        {
                            EmployeeId = employeeId.Value,
                            EmployeeCode = employeeCode,
                            DepartmentId = departmentId,
                            DesignatioId = designationId,
                            SiteId = siteId,
                            RFId = rfidValue
                        });
                    }

                    await dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("Synced JobInformation for Employee {EmployeeId} ({Action})", employeeId, action);
                    break;

                case "DELETED":
                    var toClear = await dbContext.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeId == employeeId.Value, ct);

                    if (toClear != null)
                    {
                        toClear.EmployeeCode = null;
                        toClear.DepartmentId = null;
                        toClear.DesignatioId = null;
                        toClear.SiteId = null;
                        toClear.RFId = null;
                        await dbContext.SaveChangesAsync(ct);
                    }

                    _logger.LogInformation("Cleared job fields for Employee {EmployeeId}", employeeId);
                    break;
            }
        }
    }
}
