﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using System.IO;
using System.Threading.Tasks;
using System;
using MvcBlazorDemo.Data;
using MvcBlazorDemo.Models;
using System.Security.Claims;

namespace MvcBlazorDemo.Logging
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public RequestResponseLoggingMiddleware(RequestDelegate next,
                                                ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory
                      .CreateLogger<RequestResponseLoggingMiddleware>(); 
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public async Task Invoke(HttpContext context, ApplicationDbContext dbContext)
        {
            await LogRequest(context, dbContext);
            await LogResponse(context, dbContext);
        }

        private async Task LogRequest(HttpContext context, ApplicationDbContext dbContext)
        {
            context.Request.EnableBuffering();

            await using var requestStream = _recyclableMemoryStreamManager.GetStream();
            await context.Request.Body.CopyToAsync(requestStream);
            //_logger.LogInformation($"Http Request Information: {Environment.NewLine}" +
            //                       $"User: {context.User.Identity.Name} " +
            //                       $"Schema: {context.Request.Scheme} " +
            //                       $"Host: {context.Request.Host} " +
            //                       $"Local Ip: {context.Connection.LocalIpAddress} " +
            //                       $"Remote Ip: {context.Connection.RemoteIpAddress} " +
            //                       $"Path: {context.Request.Path} " +
            //                       $"QueryString: {context.Request.QueryString} " +
            //                       $"Request Body: {ReadStreamInChunks(requestStream)}");

            string username = String.Empty;
            //if (signInManager.IsSignedIn(context.User))
            //{
            //ExternalLoginInfo info = await signInManager.GetExternalLoginInfoAsync();

            //IdentityUser external = null;

            //if (info != null)
            //    external = await userManager.FindByLoginAsync("Microsoft", userManager.GetUserId(info.Principal));
            ////var user = await AppUserService.GetAppUserAsync(User.Identity.Name, false);
            //var user = await userManager.FindByNameAsync(context.User.Identity.Name);

            //username = $"{external?.UserName ?? user.UserName}" ?? string.Empty;  //$"{external?.FirstName ?? user.FirstName} {external?.LastName ?? user.LastName}";
            //}

            username = context.User.Identity.Name ?? string.Empty;
            var x = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            TraceLog trace = new()
            {
                TraceIdentifier = context.TraceIdentifier,
                User = username, //$"{context.User}",
                RemoteIpAddress = $"{context.Connection.RemoteIpAddress}",
                DateTime = DateTime.Now,
                Schema = context.Request.Scheme,
                Host = $"{context.Request.Host}",
                Path = context.Request.Path,
                QueryString = $"{context.Request.QueryString}",
                RequestBody = ReadStreamInChunks(requestStream)
                
            };
            dbContext.Add(trace);
            await dbContext.SaveChangesAsync();

            context.Request.Body.Position = 0;
        }

        private static string ReadStreamInChunks(Stream stream)
        {
            const int readChunkBufferLength = 4096;

            stream.Seek(0, SeekOrigin.Begin);

            using var textWriter = new StringWriter();
            using var reader = new StreamReader(stream);

            var readChunk = new char[readChunkBufferLength];
            int readChunkLength;

            do
            {
                readChunkLength = reader.ReadBlock(readChunk,
                                                   0,
                                                   readChunkBufferLength);
                textWriter.Write(readChunk, 0, readChunkLength);
            } while (readChunkLength > 0);

            return textWriter.ToString();
        }

        private async Task LogResponse(HttpContext context, ApplicationDbContext dbContext)
        {
            var originalBodyStream = context.Response.Body;

            await using var responseBody = _recyclableMemoryStreamManager.GetStream();
            context.Response.Body = responseBody;

            await _next(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            //_logger.LogInformation($"Http Response Information:{Environment.NewLine}" +
            //                       $"Schema:{context.Request.Scheme} " +
            //                       $"Host: {context.Request.Host} " +
            //                       $"Path: {context.Request.Path} " +
            //                       $"QueryString: {context.Request.QueryString} " +
            //                       $"Response Body: {text}");

            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}
