using Domain.Core.Ports.Outbound;
using Domain.Core.Settings;
using Microsoft.Extensions.Options;
using System.Data;
using System.Text;

namespace Domain.Core.Base
{
    public class BaseSQLRepository : BaseService, IDisposable
    {
        protected ISQLConnectionAdapter _dbConnectionAdapter;
        protected readonly IOptions<DBSettings> _dbsettings;

        public BaseSQLRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _dbConnectionAdapter = serviceProvider.GetRequiredService<ISQLConnectionAdapter>();
            _dbsettings = serviceProvider.GetRequiredService<IOptions<DBSettings>>();
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
                    (_dbConnectionAdapter as IDisposable)?.Dispose();
                    _dbConnectionAdapter = null;
                }

                _disposed = true;
            }
        }

        #endregion
    }
}
