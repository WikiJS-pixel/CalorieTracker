using System.ComponentModel.DataAnnotations;

namespace CalorieTracker.Data.Extensions
{
    public static class ModelValidationExtensions
    {
        public static List<ValidationResult> ValidateModel(this object model)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model);
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            return validationResults;
        }

        public static string GetValidationErrors(this object model)
        {
            var errors = model.ValidateModel();
            return string.Join(Environment.NewLine, errors.Select(e => e.ErrorMessage));
        }
    }
}
