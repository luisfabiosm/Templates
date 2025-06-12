using Domain.Core.Base;
using Domain.Core.Models.Dto;
using Domain.Core.Ports.Domain;

namespace Domain.Services
{
    public class SampleService : BaseService, ISampleService
    {

        public SampleService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            
        }

        public async Task<bool> SimpleFunc1Asyc(int sampleParam)
        {
            _loggingAdapter.LogInformation("Executando SimpleFunc1Asyc");
            return true;
        }

        public async Task<SampleTaskDto> SimpleFunc2Asyc(int sampleParam)
        {
            _loggingAdapter.LogInformation("Executando SimpleFunc2Asyc");

            return new SampleTaskDto();
         
        }
    }
}
