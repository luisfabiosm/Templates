using Dapper;
using Domain.Core.Base;
using Domain.Core.Exceptions;
using Domain.Core.Models.Dto;
using Domain.Core.Models.Entity;
using Domain.Core.Ports.Outbound;
using Domain.UseCases.Sample.AddSampleTask;
using Domain.UseCases.Sample.GetSampleTask;
using Domain.UseCases.Sample.ListSampleTask;
using Domain.UseCases.Sample.UpdateSampleTaskTimer;
using Microsoft.Extensions.Hosting;
using System.Data;
using System.Data.Common;

namespace Adapters.Outbound.Database.SQL.Sample
{
    public class SQLSampleRepository : BaseSQLRepository, ISQLSampleRepository
    {
        public SQLSampleRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }


        #region SampleTaskToDo


        public async ValueTask<SampleTask> AddSampleTaskAsync(TransactionAddSampleTask transaction)
        {

            using var operationContext = StartOperation("AddSampleTaskAsync", transaction.CorrelationId);

            var sampleTask = transaction.getSampleTaskDto().MapSampleTask();

            AddTraceProperty("EntityId", sampleTask.Id.ToString());
            AddTraceProperty("EntityName", sampleTask.Name);

            // 3. Registra métrica
            RecordRequest("Repository.Add");

            await _dbConnectionAdapter.ExecuteWithRetryAsync(async (_connection) =>
            {
                var _parameters = new DynamicParameters();

                //INPUT
                _parameters.Add("@pName", sampleTask.Name);
                _parameters.Add("@pIntervalSeconds", sampleTask.TimerOnMiliseconds);
                _parameters.Add("@pIsEnabled", sampleTask.IsTimer);

                //OUTPUT
                _parameters.Add("@pId", 0, DbType.Int32, ParameterDirection.InputOutput);


                await _connection.ExecuteAsync("spx_AddSampleTask", _parameters,
                        commandTimeout: _dbsettings.Value.CommandTimeout,
                        commandType: CommandType.StoredProcedure);

                //OUTPUT
                sampleTask.Id = _parameters.Get<int>("@pId");
              
            });

            LogInformation("Entidade adicionada com sucesso: {EntityId}", sampleTask.Id);

            return sampleTask;

        }


        public async ValueTask<SampleTask> GetSampleTaskByIdAsync(TransactionGetSampleTask transaction)
        {

            using var operationContext = StartOperation("GetSampleTaskByIdAsync", transaction.CorrelationId);

            AddTraceProperty("Id", transaction.Id.ToString());

            var _sampleTask = await _dbConnectionAdapter.ExecuteWithRetryAsync(async (connection) =>
            {
                var query = @"SELECT * FROM Tasks WITH (NOLOCK) WHERE ID = @ID ";
                var queryArgs = new
                {
                    ID = transaction.Id
                };
                return await connection.QueryFirstOrDefaultAsync<SampleTask>(query, queryArgs);
            });

            LogInformation("Entidade recuperada com sucesso: {EntityId}", _sampleTask.Id);
      
            return _sampleTask;

        }


        public async ValueTask<List<SampleTask>> ListAllSampleTaskAsync(TransactionListSampleTask transaction)
        {

            using var operationContext = StartOperation("ListAllSampleTaskAsync", transaction.CorrelationId);

            var _listSample = await _dbConnectionAdapter.ExecuteWithRetryAsync(async (_connection) =>
            {
                var query = @"SELECT * FROM Tasks WITH (NOLOCK) ORDER BY Id DESC";

                return (await _connection.QueryAsync<SampleTask>(query)).ToList();
            });

            return _listSample;

        }


        public async ValueTask<bool> UpdateSampleTaskTimerAsync(TransactionUpdateSampleTaskTimer transaction)
        {

            var sampleTask = transaction.getSampleTaskDto().MapSampleTask();

            using var operationContext = StartOperation("UpdateSampleTaskTimerAsync", transaction.CorrelationId);

            AddTraceProperty("Id", sampleTask.Id.ToString());
            AddTraceProperty("Timer", sampleTask.TimerOnMiliseconds.ToString());

            var _result = await _dbConnectionAdapter.ExecuteWithRetryAsync(async (_connection) =>
            {
                var query = @"UPDATE Tasks SET TimerOnMiliseconds = @IntervalSeconds,  IsEnabled = @IsEnabled WHERE Id = @Id

                                SELECT COUNT(*) FROM Tasks WHERE Id = @Id;";


                var updateParams = new DynamicParameters();
                updateParams.Add("@IntervalSeconds", sampleTask.TimerOnMiliseconds, DbType.Int32);
                updateParams.Add("@IsEnabled", sampleTask.IsTimer, DbType.Boolean);
                updateParams.Add("@Id", sampleTask.Id, DbType.Int32);

                int _iret = await _connection.ExecuteScalarAsync<int>(query, updateParams);

                return _iret == 0 ? false : true;
            });


            LogInformation("Entidade atualizada com sucesso: {EntityId}", sampleTask.Id);

            return _result;


        }



        #endregion

    }
}
