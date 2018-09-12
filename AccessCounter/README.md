# アクセスカウンター

アクセス数を画像で返します。

![](docs/Screenshot-01.png)

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...

    services
        .AddAccessCounter(options =>
        {
            options.MinDigits = 7;
        })
        
        // Redis をバックエンドにつかう場合
        .UseRedisBackend(options =>
        {
            options.ConnectionString = "localhost";
        });

        // In-Memory バックエンドを使う場合
        .UseInMemoryBackend();

    ...
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    ...

    app.Map("/counter.cgi", x => x.UseAccessCounter());
}
```