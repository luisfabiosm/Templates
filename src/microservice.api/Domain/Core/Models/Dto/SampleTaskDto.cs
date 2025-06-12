using Domain.Core.Exceptions;
using Domain.Core.Models.Entity;
using Domain.Core.Common.ResultPattern;
using Domain.Core.Models.Entity.Domain.Core.Models.Entity;

namespace Domain.Core.Models.Dto
{

    public readonly record struct SampleTaskDto
    {
        private readonly string _name;
        private readonly bool _isTrigged;
        private readonly int _timer;

        public int Id { get; }
        public string Name => _name;
        public bool IsTimerTrigged => _isTrigged;
        public int TimerOnMilliseconds => _timer;

        public bool IsNameValid => !string.IsNullOrWhiteSpace(Name);
        public bool IsTimerValid => TimerOnMilliseconds > 0;
        public bool IsConfigurationValid => !IsTimerTrigged || TimerOnMilliseconds >= 500;
        public bool IsValid => IsNameValid && IsTimerValid && IsConfigurationValid;

        public SampleTaskDto(string name, bool isTrigged, int timer)
        {
            ValidateInputParameters(name, isTrigged, timer);

            Id = 0;
            _name = name;
            _isTrigged = isTrigged;
            _timer = timer;
        }

        public SampleTaskDto(int id, int timer)
        {
            ValidateTimer(timer);

            Id = id;
            _name = string.Empty;
            _isTrigged = false;
            _timer = timer;
        }

        public static SampleTaskDto CreateNew(string name, bool isTrigged, int timer)
            => new(name, isTrigged, timer);

        public static SampleTaskDto CreateForUpdate(int id, int timer)
            => new(id, timer);

        public static BaseReturn<SampleTaskDto> CreateValidated(string name, bool isTrigged, int timer)
        {
            try
            {
                var dto = new SampleTaskDto(name, isTrigged, timer);
                return BaseReturn<SampleTaskDto>.FromSuccess(dto, "DTO criado e validado com sucesso");
            }
            catch (Exception ex)
            {
                return BaseReturn<SampleTaskDto>.FromException(ex);
            }
        }

        public BaseReturn<SampleTask> ToEntity()
        {
            try
            {
                if (!IsValid)
                {
                    return BaseReturn<SampleTask>.FromError(
                        "DTO não é válido para conversão em entidade",
                        400,
                        errorDetails: GetValidationErrors());
                }

                var entity = new SampleTask(Name, IsTimerTrigged, TimerOnMilliseconds);
                return BaseReturn<SampleTask>.FromSuccess(entity, "Entidade criada com sucesso");
            }
            catch (Exception ex)
            {
                return BaseReturn<SampleTask>.FromException(ex);
            }
        }

        public SampleTask MapSampleTask()
            => new(Name, IsTimerTrigged, TimerOnMilliseconds);

        private static void ValidateInputParameters(string name, bool isTrigged, int timer)
        {
            var businessException = new BusinessException();

            if (string.IsNullOrWhiteSpace(name))
                businessException.AddDetails(new ErrorDetails("Uma Task precisa ter um nome definido"));

            if (isTrigged && timer <= 0)
                businessException.AddDetails(new ErrorDetails("Timer não pode ser 0 quando IsTimerTrigged é true"));

            if (isTrigged && timer < 500)
                businessException.AddDetails(new ErrorDetails("Timer deve ser no mínimo 500 millisegundos quando ativo"));

            if (businessException.ErrorDetails.Count > 0)
                throw businessException;
        }

        private static void ValidateTimer(int timer)
        {
            if (timer <= 0)
                throw new ValidateException("Timer deve ser maior que zero");
        }

        public BaseReturn<SampleTaskDto> Validate()
        {
            var errors = GetValidationErrors();

            return errors.Count == 0
                ? BaseReturn<SampleTaskDto>.FromSuccess(this, "Validação bem-sucedida")
                : BaseReturn<SampleTaskDto>.FromError(
                    "Erros de validação encontrados",
                    400,
                    errorDetails: errors);
        }

        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (!IsNameValid)
                errors.Add("Nome da task é obrigatório");

            if (!IsTimerValid)
                errors.Add("Timer deve ser maior que zero");

            if (IsTimerTrigged && TimerOnMilliseconds < 500)
                errors.Add("Timer deve ser no mínimo 500 millisegundos quando ativo");

            return errors;
        }

        public TResult Map<TResult>(Func<SampleTaskDto, TResult> mapper)
            => mapper(this);

        public BaseReturn<TResult> Bind<TResult>(Func<SampleTaskDto, BaseReturn<TResult>> binder)
        {
            try
            {
                return binder(this);
            }
            catch (Exception ex)
            {
                return BaseReturn<TResult>.FromException(ex);
            }
        }

        public BaseReturn<SampleTaskDto> ToResult(string message = "DTO válido")
            => BaseReturn<SampleTaskDto>.FromSuccess(this, message);

        // Métodos para clonagem com alterações (immutable pattern)
        public SampleTaskDto WithName(string newName)
            => new(newName, IsTimerTrigged, TimerOnMilliseconds);

        public SampleTaskDto WithTimer(int newTimer)
            => new(Name, IsTimerTrigged, newTimer);

        public SampleTaskDto WithTimerTrigged(bool triggered)
            => new(Name, triggered, TimerOnMilliseconds);

        // Conversão implícita para facilitar uso
        public static implicit operator SampleTaskDto((string name, bool isTrigged, int timer) tuple)
            => new(tuple.name, tuple.isTrigged, tuple.timer);

        public override string ToString()
            => $"SampleTaskDto(Id: {Id}, Name: {Name}, Timer: {TimerOnMilliseconds}ms, Triggered: {IsTimerTrigged}, Valid: {IsValid})";
    }


    public static class SampleTaskDtoExtensions
    {

        public static BaseReturn<IEnumerable<SampleTask>> ToEntities(
            this IEnumerable<SampleTaskDto> dtos)
        {
            try
            {
                var entities = new List<SampleTask>();
                var errors = new List<string>();

                foreach (var dto in dtos)
                {
                    var entityResult = dto.ToEntity();
                    if (entityResult.IsSuccess)
                    {
                        entities.Add(entityResult.Data);
                    }
                    else
                    {
                        errors.Add($"DTO {dto.Name}: {entityResult.Message}");
                    }
                }

                return errors.Count == 0
                    ? BaseReturn<IEnumerable<SampleTask>>.FromSuccess(
                        entities,
                        $"Convertidos {entities.Count} DTOs para entidades com sucesso")
                    : BaseReturn<IEnumerable<SampleTask>>.FromError(
                        $"Erros na conversão: {string.Join(", ", errors)}",
                        400,
                        errorDetails: errors);
            }
            catch (Exception ex)
            {
                return BaseReturn<IEnumerable<SampleTask>>.FromException(ex);
            }
        }


        public static BaseReturn<IEnumerable<SampleTaskDto>> ValidateBatch(
            this IEnumerable<SampleTaskDto> dtos)
        {
            var validDtos = new List<SampleTaskDto>();
            var errors = new List<string>();

            foreach (var dto in dtos)
            {
                var validationResult = dto.Validate();
                if (validationResult.IsSuccess)
                {
                    validDtos.Add(dto);
                }
                else
                {
                    errors.Add($"DTO {dto.Name}: {validationResult.Message}");
                }
            }

            return errors.Count == 0
                ? BaseReturn<IEnumerable<SampleTaskDto>>.FromSuccess(
                    validDtos,
                    $"Validados {validDtos.Count} DTOs com sucesso")
                : BaseReturn<IEnumerable<SampleTaskDto>>.FromError(
                    $"Erros de validação: {string.Join(", ", errors)}",
                    400,
                    errorDetails: errors);
        }


        public static BaseReturn<TResult> Transform<TResult>(
            this BaseReturn<SampleTaskDto> dtoResult,
            Func<SampleTaskDto, TResult> transformer)
        {
            return dtoResult.Map(transformer);
        }


        public static IEnumerable<SampleTaskDto> FilterValid(
            this IEnumerable<SampleTaskDto> dtos)
        {
            return dtos.Where(dto => dto.IsValid);
        }


        public static BaseReturn<TResult> TransformIfValid<TResult>(
            this SampleTaskDto dto,
            Func<SampleTaskDto, TResult> transformer,
            string errorMessage = "DTO não é válido para transformação")
        {
            return dto.IsValid
                ? BaseReturn<TResult>.FromSuccess(transformer(dto), "Transformação bem-sucedida")
                : BaseReturn<TResult>.FromError(errorMessage, 400, errorDetails: dto.GetValidationErrors());
        }


        public static BaseReturn<SampleTaskDto> CreateFromTuple(
            (string name, bool isTrigged, int timer) data)
        {
            return SampleTaskDto.CreateValidated(data.name, data.isTrigged, data.timer);
        }
    }
}