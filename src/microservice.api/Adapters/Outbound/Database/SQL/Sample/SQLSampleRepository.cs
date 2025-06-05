using Dapper;
using Domain.Core.Base;
using Domain.Core.Exceptions;
using Domain.Core.Interfaces.Outbound;
using Domain.Core.Models.Dto;
using Domain.Core.Models.Entity;
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



        public async ValueTask<(SampleTask, Exception exception)> AddSampleTaskAsync(TransactionAddSampleTask transaction)
        {
            try
            {
                using var operationContext = _loggingAdapter.StartOperation("AddSampleTaskAsync", transaction.CorrelationId);

                var sampleTask = transaction.getSampleTaskDto().MapSampleTask();

                _loggingAdapter.AddProperty("Name", sampleTask.Name);

                await _dbConnection.ExecuteWithRetryAsync(async (_connection) =>
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

                return sampleTask.Id != 0 ? (sampleTask, null) : (null, new InternalException("Erro ao adicionar nova configuração de  HealthCheck"));

            }
            catch (Exception ex)
            {
                return (null, HandleException("AddSampleTaskAsync", ex));
            }
        }


        public async ValueTask<(SampleTask, Exception exception)> GetSampleTaskByIdAsync(TransactionGetSampleTask transaction)
        {
            try
            {
                using var operationContext = _loggingAdapter.StartOperation("GetSampleTaskByIdAsync", transaction.CorrelationId);

                _loggingAdapter.AddProperty("Id", transaction.Id.ToString());

                var _sampleTask = await _dbConnection.ExecuteWithRetryAsync(async (connection) =>
                {
                    var query = @"SELECT * FROM Tasks WHERE ID = @ID ";
                    var queryArgs = new
                    {
                        ID = transaction.Id
                    };
                    return await connection.QueryFirstOrDefaultAsync<SampleTask>(query, queryArgs);
                });

                return _sampleTask is null ? (null, new InternalException("Task inexistente")) : (_sampleTask, null);

            }
            catch (Exception ex)
            {
                return (null, HandleException("GetSampleTaskByIdAsync", ex));
            }
        
        }


        public async ValueTask<(List<SampleTask>, Exception exception)> ListAllSampleTaskAsync(TransactionListSampleTask transaction)
        {
            try
            {
                using var operationContext = _loggingAdapter.StartOperation("ListAllSampleTaskAsync", transaction.CorrelationId);

                var _listSample =  await _dbConnection.ExecuteWithRetryAsync(async (_connection) =>
                {
                    var query = @"SELECT * FROM Tasks";

                    //var queryArgs = new
                    //{
                    //    ID = 1
                    //};

                    return( await _connection.QueryAsync<SampleTask>(query)).ToList();
                });

                return _listSample is null ? (null, new BusinessException("Task inexistente")):(_listSample,null);
            }
            catch (Exception ex)
            {
               return (null, HandleException("ListAllSampleTaskAsync", ex));
            }
        }


        public async ValueTask<(bool,Exception exception)> UpdateSampleTaskTimerAsync(TransactionUpdateSampleTaskTimer transaction)
        {
            try
            {

                var sampleTask = transaction.getSampleTaskDto().MapSampleTask();

                using var operationContext = _loggingAdapter.StartOperation("UpdateSampleTaskTimerAsync", transaction.CorrelationId);

                _loggingAdapter.AddProperty("Id", sampleTask.Id.ToString());
                _loggingAdapter.AddProperty("Timer", sampleTask.TimerOnMiliseconds.ToString());

                var _result = await _dbConnection.ExecuteWithRetryAsync(async (_connection) =>
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

                return (_result, null);
            }
            catch (Exception ex)
            {
                return (false, HandleException("RegistrarCreditoOrdemPagamento", ex));
            }

        }



        #endregion

    }
}
