using FluentValidation;
namespace DMS_REST_API.DTO
{
    public class DocumentDtoValidator: AbstractValidator<DocumentDto>
    {
        public DocumentDtoValidator() {
            RuleFor(x => x.FileType)
                    .NotEmpty().WithMessage("The Document name cannot be empty.")
                    .MaximumLength(100).WithMessage("The Document name must not exceed 100 chars.");

            RuleFor(x => x.FileType)
                    .NotNull().WithMessage("The Filetype must be specified.");
        }
    }
}
