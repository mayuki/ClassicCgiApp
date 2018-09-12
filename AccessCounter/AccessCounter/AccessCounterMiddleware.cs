using AccessCounter;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AccessCounter
{
    public class AccessCounterOptions
    {
        /// <summary>
        /// カウンターの画像のバリエーションを取得、設定します。
        /// </summary>
        public string CounterType { get; set; } = "gif34";

        /// <summary>
        /// 最低表示桁数を取得、設定します。
        /// </summary>
        public int MinDigits { get; set; } = 6;

        /// <summary>
        /// 画像のベースディレクトリを取得、設定します。
        /// </summary>
        public string ResourcesBaseDirectory { get; set; } = Path.Combine(Path.GetDirectoryName(typeof(AccessCounterMiddleware).Assembly.Location), @"Resources");
    }

    public class AccessCounterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IBackend _backend;
        private readonly AccessCounterOptions _options;

        public AccessCounterMiddleware(RequestDelegate next, IBackend backend, AccessCounterOptions options)
        {
            _next = next;
            _backend = backend;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var counterValue = await _backend.IncrementAsync();

            context.Response.ContentType = "image/png";
            WriteCounterImage(context.Response.Body, counterValue, _options.CounterType, _options.MinDigits);
        }

        private void WriteCounterImage(Stream stream, long counterValue, string counterType, int minDigits)
        {
            var imageBaseDir = Path.Combine(_options.ResourcesBaseDirectory, counterType);

            var counterString = counterValue.ToString();
            var digits = counterString.Length;
            var padDigits = Math.Max(digits, Math.Max(0, minDigits)) - digits;

            // まあ0でサイズを決定しちゃおう
            int digitWidth;
            int digitHeight;
            using (var zeroImage = Image.FromFile(Path.Combine(imageBaseDir, "0.gif")))
            {
                digitWidth = zeroImage.Width;
                digitHeight = zeroImage.Height;
            }

            var width = digitWidth * (digits + padDigits);
            var height = digitHeight;
            using (var bitmap = new Bitmap(width, height))
            using (var g = Graphics.FromImage(bitmap))
            {
                for (var i = 0; i < padDigits; i++)
                {
                    using (var digitImage = Image.FromFile(Path.Combine(imageBaseDir, "0.gif")))
                    {
                        g.DrawImage(digitImage, i * digitWidth, 0);
                    }
                }
                for (var i = 0; i < digits; i++)
                {
                    using (var digitImage = Image.FromFile(Path.Combine(imageBaseDir, $"{counterString[i]}.gif")))
                    {
                        g.DrawImage(digitImage, (i + padDigits) * digitWidth, 0);
                    }
                }

                bitmap.Save(stream, ImageFormat.Png);
            }
        }
    }
}

namespace Microsoft.AspNetCore.Builder
{
    public static class AccessCounterMiddlewareExtensions
    {
        public static void UseAccessCounter(this IApplicationBuilder app)
        {
            app.UseMiddleware<AccessCounterMiddleware>();
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AccessCounterSerivceExtensions
    {
        public static AccessCounterServiceBuilder AddAccessCounter(this IServiceCollection services, Action<AccessCounterOptions> configure = null)
        {
            var options = new AccessCounterOptions();
            configure?.Invoke(options);

            services.AddSingleton(options);
            return new AccessCounterServiceBuilder(services);
        }

        public class AccessCounterServiceBuilder
        {
            private readonly IServiceCollection _services;

            protected internal AccessCounterServiceBuilder(IServiceCollection services)
            {
                _services = services;
            }

            public void UseInMemoryBackend()
            {
                _services.AddSingleton<IBackend>(new InMemoryBackend());
            }

            public void UseRedisBackend(Action<RedisBackend.RedisBackendOptions> configure)
            {
                var options = new RedisBackend.RedisBackendOptions();
                configure(options);

                _services.AddSingleton<IBackend>(new RedisBackend(options));
            }
        }
    }
}