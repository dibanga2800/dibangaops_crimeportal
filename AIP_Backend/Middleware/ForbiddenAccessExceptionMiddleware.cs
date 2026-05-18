#nullable enable

using AIPBackend.Exceptions;

namespace AIPBackend.Middleware
{
	public sealed class ForbiddenAccessExceptionMiddleware
	{
		private readonly RequestDelegate _next;

		public ForbiddenAccessExceptionMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				await _next(context);
			}
			catch (ForbiddenAccessException ex)
			{
				context.Response.StatusCode = StatusCodes.Status403Forbidden;
				context.Response.ContentType = "application/json";
				await context.Response.WriteAsJsonAsync(new { success = false, message = ex.Message });
			}
		}
	}
}
