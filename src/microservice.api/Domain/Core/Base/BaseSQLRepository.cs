using Domain.Core.Interfaces.Outbound;
using Domain.Core.Settings;
using Microsoft.Extensions.Options;
using System.Data;
using System.Text;

namespace Domain.Core.Base
{
    public class BaseSQLRepository : BaseService, IDisposable
    {
    
        protected ISQLConnectionAdapter _dbConnection;
        protected IDbConnection _session;
        protected readonly IOptions<DBSettings> _dbsettings;

        public BaseSQLRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _dbConnection = serviceProvider.GetRequiredService<ISQLConnectionAdapter>();
            _dbsettings = serviceProvider.GetRequiredService<IOptions<DBSettings>>();
        }




        ~BaseSQLRepository()
        {
            Dispose(false);
        }


        #region DISPOSE


        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {

                    if (_session != null)
                    {
                        if (_session.State != ConnectionState.Closed)
                        {
                            _session.Close();
                        }
                        (_session as IDisposable)?.Dispose();
                        _session = null;
                    }

                    (_dbConnection as IDisposable)?.Dispose();
                    _dbConnection = null;
                }

                _disposed = true;
            }
        }

        #endregion
    }
}
