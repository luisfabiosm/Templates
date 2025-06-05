using Domain.Core.ResultPattern;
using System.ComponentModel.DataAnnotations;

namespace Domain.Core.Base
{
    /// <summary>
    /// Interface para validadores seguindo SRP
    /// </summary>
    public interface IValidator<in T>
    {
        Task<BSValidationResult> ValidateAsync(T item, CancellationToken cancellationToken = default);
    }
}
