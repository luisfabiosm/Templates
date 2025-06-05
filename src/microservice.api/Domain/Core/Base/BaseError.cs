
using Domain.Core.Enums;

namespace Domain.Core.Base
{
    public record BaseError
    {
        public EnumErrorType type { get; private set; }
        public int code { get; private set; }
        public string message { get; private set; }
        public string? source { get; private set; }


        public BaseError(int code, string mensagem, EnumErrorType type = EnumErrorType.System, string source = null)
        {
            this.code = code;
            this.message = mensagem;
            this.source = source;
            this.type = type;
        }



    }
}
