using Domain.Core.Models.Dto;

namespace Domain.Core.Ports.Domain
{
    public interface ISampleService
    {
        Task<bool> SimpleFunc1Asyc(int sampleParam);

        Task<SampleTaskDto> SimpleFunc2Asyc(int sampleParam);
    }
}
