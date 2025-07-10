using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiniWorldNode.Models;
using MiniWorldNode.Services;

namespace MiniWorldNode;

class Program
{
    static async Task Main(string[] args)
    {
        // 设置控制台编码为UTF-8以支持中文显示
        SetConsoleEncoding();

        Console.WriteLine("=================================");
        Console.WriteLine("   MiniWorld 节点启动中...");
        Console.WriteLine("=================================");

        try
        {
            var host = CreateHostBuilder(args).Build();

            // 注册应用退出事件
            var serverManager = host.Services.GetRequiredService<GameServerManager>();
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                Console.WriteLine("程序正在退出，停止所有服务器...");
                serverManager.StopAllServers();
            };

            // 控制台取消事件（Ctrl+C）
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\n收到退出信号，正在优雅关闭...");
                serverManager.StopAllServers();
                Environment.Exit(0);
            };

            // 启动应用程序
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"应用程序启动失败: {ex.Message}");
            Console.WriteLine($"详细错误: {ex}");
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// 设置控制台编码
    /// </summary>
    private static void SetConsoleEncoding()
    {
        try
        {
            // 设置控制台输入和输出编码为UTF-8
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            // 在Windows上设置代码页为UTF-8
            if (OperatingSystem.IsWindows())
            {
                // 尝试设置控制台代码页为UTF-8 (65001)
                try
                {
                    var handle = GetStdHandle(-11); // STD_OUTPUT_HANDLE
                    SetConsoleOutputCP(65001); // UTF-8 code page
                    SetConsoleCP(65001); // UTF-8 input code page
                }
                catch
                {
                    // 如果P/Invoke失败，忽略错误，继续使用.NET的编码设置
                }
            }

            Console.WriteLine("控制台编码已设置为UTF-8");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"设置控制台编码时发生错误: {ex.Message}");
        }
    }

    // Windows API调用，用于设置控制台代码页
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool SetConsoleOutputCP(uint wCodePageID);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool SetConsoleCP(uint wCodePageID);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    /// <summary>
    /// 创建主机构建器
    /// </summary>
    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // 配置选项
                services.Configure<ServerSettings>(
                    context.Configuration.GetSection("ServerSettings"));

                // 注册服务
                services.AddSingleton<GameServerManager>();
                services.AddSingleton<NodeInfoService>();
                services.AddHostedService<RpcServerService>();
                services.AddHostedService<ConsoleCommandService>();

                // 配置日志
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddConsole(options =>
                    {
                        options.LogToStandardErrorThreshold = LogLevel.None; // 所有日志都输出到stdout
                    });
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            })
            .UseConsoleLifetime();
}
