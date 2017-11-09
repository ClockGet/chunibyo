using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace WebApplication1.Filters
{
    public class ApiExceptionFilterAttribute :ExceptionFilterAttribute
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly ILogger _logger;

        public ApiExceptionFilterAttribute(
            IHostingEnvironment hostingEnvironment,
            IModelMetadataProvider modelMetadataProvider,
            ILogger<ApiExceptionFilterAttribute> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _modelMetadataProvider = modelMetadataProvider;
            _logger = logger;
        }
        public override void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception.Message);
            context.ExceptionHandled = true;
            context.Result = new JsonResult(new { success=false, message="请稍后再试" });
        }
    }
}
