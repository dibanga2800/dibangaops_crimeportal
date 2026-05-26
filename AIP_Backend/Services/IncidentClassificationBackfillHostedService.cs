#nullable enable

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIPBackend.Services
{
	/// <summary>
	/// Singleton background timer that periodically asks
	/// <see cref="IIncidentClassificationBackfillService"/> to drain any classification
	/// backlog. The actual work runs inside a fresh scoped DI lifetime so we get a
	/// clean DbContext per pass.
	/// </summary>
	public sealed class IncidentClassificationBackfillHostedService : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly IOptionsMonitor<IncidentClassificationBackfillOptions> _optionsMonitor;
		private readonly ILogger<IncidentClassificationBackfillHostedService> _logger;

		public IncidentClassificationBackfillHostedService(
			IServiceProvider serviceProvider,
			IOptionsMonitor<IncidentClassificationBackfillOptions> optionsMonitor,
			ILogger<IncidentClassificationBackfillHostedService> logger)
		{
			_serviceProvider = serviceProvider;
			_optionsMonitor = optionsMonitor;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			var options = _optionsMonitor.CurrentValue;
			if (!options.Enabled)
			{
				_logger.LogInformation("Incident classification backfill is disabled by configuration");
				return;
			}

			_logger.LogInformation(
				"Incident classification backfill started (startup delay {StartupDelaySeconds}s, interval {IntervalMinutes}m, batch {BatchSize})",
				options.StartupDelaySeconds,
				options.IntervalMinutes,
				options.BatchSize);

			try
			{
				if (options.StartupDelaySeconds > 0)
				{
					await Task.Delay(TimeSpan.FromSeconds(options.StartupDelaySeconds), stoppingToken);
				}

				while (!stoppingToken.IsCancellationRequested)
				{
					await RunOncePassAsync(stoppingToken);

					var current = _optionsMonitor.CurrentValue;
					if (!current.Enabled)
					{
						_logger.LogInformation("Incident classification backfill disabled at runtime; stopping");
						return;
					}

					var intervalMinutes = current.IntervalMinutes > 0 ? current.IntervalMinutes : 60;
					await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
				}
			}
			catch (OperationCanceledException)
			{
				// Normal shutdown.
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Incident classification backfill terminated unexpectedly");
			}
		}

		private async Task RunOncePassAsync(CancellationToken cancellationToken)
		{
			try
			{
				using var scope = _serviceProvider.CreateScope();
				var backfill = scope.ServiceProvider.GetRequiredService<IIncidentClassificationBackfillService>();
				var options = _optionsMonitor.CurrentValue;
				var batchSize = options.BatchSize > 0 ? options.BatchSize : 25;
				await backfill.RunAsync(batchSize, cancellationToken);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				// Swallow at the timer level so one bad pass never kills the hosted
				// service. The inner RunAsync already logs per-row failures.
				_logger.LogError(ex, "Incident classification backfill pass failed");
			}
		}
	}
}
