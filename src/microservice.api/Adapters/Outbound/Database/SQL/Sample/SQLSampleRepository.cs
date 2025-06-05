// Adapters/Outbound/Database/SQL/Sample/SQLSampleTaskRepository.cs
using Dapper;
using Domain.Core.Base;
using Domain.Core.Interfaces.Outbound;
using Domain.Core.Models.Entity;
using Domain.Core.Models.ValueObjects;
using Domain.Core.ResultPattern;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Adapters.Outbound.Database.SQL.Sample
{
    /// <summary>
    /// Repository SQL para SampleTask seguindo performance best practices
    /// </summary>
    public sealed class SQLSampleTaskRepository : BaseSQLRepository<SampleTask, SampleTaskId>, ISampleTaskRepository
    {
        private const string TableName = "SampleTasks";

        // Query constants para melhor performance (evita recompilação)
        private const string SelectByIdQuery = $"SELECT Id, Name, TimerInMilliseconds, IsEnabled, CreatedAt, UpdatedAt FROM {TableName} WHERE Id = @Id";
        private const string SelectAllQuery = $"SELECT Id, Name, TimerInMilliseconds, IsEnabled, CreatedAt, UpdatedAt FROM {TableName} ORDER BY CreatedAt DESC";
        private const string SelectActiveQuery = $"SELECT Id, Name, TimerInMilliseconds, IsEnabled, CreatedAt, UpdatedAt FROM {TableName} WHERE IsEnabled = 1 ORDER BY CreatedAt DESC";
        private const string SelectByTimerRangeQuery = $"SELECT Id, Name, TimerInMilliseconds, IsEnabled, CreatedAt, UpdatedAt FROM {TableName} WHERE TimerInMilliseconds BETWEEN @MinTimer AND @MaxTimer ORDER BY TimerInMilliseconds";
        private const string InsertQuery = $"INSERT INTO {TableName} (Name, TimerInMilliseconds, IsEnabled, CreatedAt) OUTPUT INSERTED.Id VALUES (@Name, @TimerInMilliseconds, @IsEnabled, @CreatedAt)";
        private const string UpdateQuery = $"UPDATE {TableName} SET Name = @Name, TimerInMilliseconds = @TimerInMilliseconds, IsEnabled = @IsEnabled, UpdatedAt = @UpdatedAt WHERE Id = @Id";
        private const string UpdateTimerQuery = $"UPDATE {TableName} SET TimerInMilliseconds = @TimerInMilliseconds, UpdatedAt = @UpdatedAt WHERE Id = @Id";
        private const string DeleteQuery = $"DELETE FROM {TableName} WHERE Id = @Id";
        private const string ExistsQuery = $"SELECT CASE WHEN EXISTS(SELECT 1 FROM {TableName} WHERE Id = @Id) THEN 1 ELSE 0 END";

        public SQLSampleTaskRepository(
            ISQLConnectionAdapter connectionAdapter,
            ILogger<SQLSampleTaskRepository> logger)
            : base(connectionAdapter, logger)
        {
        }

        protected override async Task<BSResult<SampleTask>> GetByIdInternalAsync(SampleTaskId id, CancellationToken cancellationToken)
        {
            try
            {
                var row = await _connectionAdapter.QueryFirstOrDefaultAsync<SampleTaskRow>(
                    SelectByIdQuery,
                    new { Id = id.Value },
                    cancellationToken);

                if (row is null)
                {
                    return BSError.NotFound($"SampleTask com ID {id} não encontrada");
                }

                var sampleTask = MapRowToEntity(row);
                return BSResult<SampleTask>.Success(sampleTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar SampleTask por ID: {Id}", id);
                return BSError.Internal($"Erro ao buscar SampleTask: {ex.Message}");
            }
        }

        protected override async Task<BSResult<IReadOnlyList<SampleTask>>> GetAllInternalAsync(CancellationToken cancellationToken)
        {
            try
            {
                var rows = await _connectionAdapter.QueryAsync<SampleTaskRow>(
                    SelectAllQuery,
                    cancellationToken: cancellationToken);

                var sampleTasks = rows.Select(MapRowToEntity).ToArray();
                return BSResult<IReadOnlyList<SampleTask>>.Success(sampleTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todas as SampleTasks");
                return BSError.Internal($"Erro ao buscar SampleTasks: {ex.Message}");
            }
        }

        protected override async Task<BSResult<SampleTask>> AddInternalAsync(SampleTask entity, CancellationToken cancellationToken)
        {
            try
            {
                var parameters = new
                {
                    Name = entity.Name.Value,
                    TimerInMilliseconds = entity.Timer.Value,
                    IsEnabled = entity.IsEnabled,
                    CreatedAt = entity.CreatedAt
                };

                var newId = await _connectionAdapter.QueryFirstOrDefaultAsync<int>(
                    InsertQuery,
                    parameters,
                    cancellationToken);

                var createdEntity = SampleTask.Reconstruct(
                    new SampleTaskId(newId),
                    entity.Name,
                    entity.Timer,
                    entity.IsEnabled,
                    entity.CreatedAt);

                return BSResult<SampleTask>.Success(createdEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar SampleTask: {Name}", entity.Name);
                return BSError.Internal($"Erro ao adicionar SampleTask: {ex.Message}");
            }
        }

        protected override async Task<BSResult<SampleTask>> UpdateInternalAsync(SampleTask entity, CancellationToken cancellationToken)
        {
            try
            {
                var parameters = new
                {
                    Id = entity.Id.Value,
                    Name = entity.Name.Value,
                    TimerInMilliseconds = entity.Timer.Value,
                    IsEnabled = entity.IsEnabled,
                    UpdatedAt = DateTime.UtcNow
                };

                var rowsAffected = await _connectionAdapter.ExecuteAsync(
                    UpdateQuery,
                    parameters,
                    cancellationToken);

                if (rowsAffected == 0)
                {
                    return BSError.NotFound($"SampleTask com ID {entity.Id} não encontrada para atualização");
                }

                var updatedEntity = entity with { UpdatedAt = parameters.UpdatedAt };
                return BSResult<SampleTask>.Success(updatedEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar SampleTask: {Id}", entity.Id);
                return BSError.Internal($"Erro ao atualizar SampleTask: {ex.Message}");
            }
        }

        protected override async Task<Result> DeleteInternalAsync(SampleTaskId id, CancellationToken cancellationToken)
        {
            try
            {
                var rowsAffected = await _connectionAdapter.ExecuteAsync(
                    DeleteQuery,
                    new { Id = id.Value },
                    cancellationToken);

                if (rowsAffected == 0)
                {
                    return BSError.NotFound($"SampleTask com ID {id} não encontrada para exclusão");
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir SampleTask: {Id}", id);
                return BSError.Internal($"Erro ao excluir SampleTask: {ex.Message}");
            }
        }

        protected override async Task<BSResult<bool>> ExistsInternalAsync(SampleTaskId id, CancellationToken cancellationToken)
        {
            try
            {
                var exists = await _connectionAdapter.QueryFirstOrDefaultAsync<bool>(
                    ExistsQuery,
                    new { Id = id.Value },
                    cancellationToken);

                return BSResult<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar existência da SampleTask: {Id}", id);
                return BSError.Internal($"Erro ao verificar SampleTask: {ex.Message}");
            }
        }

        public async Task<BSResult<IReadOnlyList<SampleTask>>> GetByTimerRangeAsync(
            int minTimer,
            int maxTimer,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var rows = await _connectionAdapter.QueryAsync<SampleTaskRow>(
                    SelectByTimerRangeQuery,
                    new { MinTimer = minTimer, MaxTimer = maxTimer },
                    cancellationToken);

                var sampleTasks = rows.Select(MapRowToEntity).ToArray();
                return BSResult<IReadOnlyList<SampleTask>>.Success(sampleTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar SampleTasks por range de timer: {MinTimer}-{MaxTimer}", minTimer, maxTimer);
                return BSError.Internal($"Erro ao buscar SampleTasks por timer: {ex.Message}");
            }
        }

        public async Task<BSResult<IReadOnlyList<SampleTask>>> GetActiveTasksAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var rows = await _connectionAdapter.QueryAsync<SampleTaskRow>(
                    SelectActiveQuery,
                    cancellationToken: cancellationToken);

                var sampleTasks = rows.Select(MapRowToEntity).ToArray();
                return BSResult<IReadOnlyList<SampleTask>>.Success(sampleTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar SampleTasks ativas");
                return BSError.Internal($"Erro ao buscar SampleTasks ativas: {ex.Message}");
            }
        }

        public async Task<Result> UpdateTimerAsync(
            SampleTaskId id,
            TimerInMilliseconds timer,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var parameters = new
                {
                    Id = id.Value,
                    TimerInMilliseconds = timer.Value,
                    UpdatedAt = DateTime.UtcNow
                };

                var rowsAffected = await _connectionAdapter.ExecuteAsync(
                    UpdateTimerQuery,
                    parameters,
                    cancellationToken);

                if (rowsAffected == 0)
                {
                    return BSError.NotFound($"SampleTask com ID {id} não encontrada para atualização do timer");
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar timer da SampleTask: {Id}", id);
                return BSError.Internal($"Erro ao atualizar timer: {ex.Message}");
            }
        }

        private static SampleTask MapRowToEntity(SampleTaskRow row)
        {
            return SampleTask.Reconstruct(
                new SampleTaskId(row.Id),
                new TaskName(row.Name),
                new TimerInMilliseconds(row.TimerInMilliseconds),
                row.IsEnabled,
                row.CreatedAt,
                row.UpdatedAt);
        }

        // DTO para mapeamento do banco seguindo Object Calisthenics
        private sealed record SampleTaskRow
        {
            public int Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public int TimerInMilliseconds { get; init; }
            public bool IsEnabled { get; init; }
            public DateTime CreatedAt { get; init; }
            public DateTime? UpdatedAt { get; init; }
        }
    }
}