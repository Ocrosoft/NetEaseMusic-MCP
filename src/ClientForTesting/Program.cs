using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;

var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "Server",
    Command = "dotnet",
    Arguments = ["run", "NetEaseMusic-MCP"],
    WorkingDirectory = Path.GetFullPath("../../../.."),
});

var client = await McpClientFactory.CreateAsync(clientTransport);

// Print the list of tools available from the server, with index from 0.
var tools = await client.ListToolsAsync();
Console.WriteLine("可用 MCP 工具：");
for (int i = 0; i < tools.Count; i++)
{
    Console.WriteLine($"{i}: {tools[i].Name}");
}

while (true)
{
    Console.WriteLine("\n选项:");
    Console.WriteLine("1. 显示工具参数");
    Console.WriteLine("2. 调用工具");
    Console.WriteLine("0. 推出");
    Console.Write("输入选项：");

    var choice = Console.ReadLine();

    if (choice == "0")
    {
        break;
    }
    else if (choice == "1")
    {
        // 查询工具参数
        Console.Write("输入工具序号：");
        if (int.TryParse(Console.ReadLine(), out int toolIndex) && toolIndex >= 0 && toolIndex < tools.Count)
        {
            var tool = tools[toolIndex];
            Console.WriteLine($"\n工具名称：{tool.Name}");
            Console.WriteLine($"工具描述：{tool.Description}");

            // 查询工具的 JSON Schema 来获取参数信息
            var schema = tool.JsonSchema;

            // 检查 schema 是否包含参数信息
            if (schema.TryGetProperty("properties", out var properties))
            {
                Console.WriteLine("参数：");
                foreach (var property in properties.EnumerateObject())
                {
                    Console.WriteLine($"  - {property.Name}");

                    // 获取参数的描述（如果有）
                    if (property.Value.TryGetProperty("description", out var description))
                    {
                        Console.WriteLine($"    描述：{description}");
                    }

                    // 获取参数的类型
                    if (property.Value.TryGetProperty("type", out var type))
                    {
                        Console.WriteLine($"    类型：{type}");
                    }

                    // 如果是数值类型，检查是否有范围约束
                    if (type.GetString() == "integer" || type.GetString() == "number")
                    {
                        if (property.Value.TryGetProperty("minimum", out var min))
                        {
                            Console.WriteLine($"    最小值：{min}");
                        }
                        if (property.Value.TryGetProperty("maximum", out var max))
                        {
                            Console.WriteLine($"    最大值：{max}");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("该工具没有参数。");
            }
        }
        else
        {
            Console.WriteLine("无效序号。");
        }
    }
    else if (choice == "2")
    {
        // 调用工具
        Console.Write("输入工具序号：");
        if (int.TryParse(Console.ReadLine(), out int toolIndex) && toolIndex >= 0 && toolIndex < tools.Count)
        {
            var tool = tools[toolIndex];

            // 查询工具参数
            var schema = tool.JsonSchema;
            Dictionary<string, object?> arguments = [];

            if (schema.TryGetProperty("properties", out var properties))
            {
                foreach (var property in properties.EnumerateObject())
                {
                    Console.Write($"输入参数 '{property.Name}' 的值：");
                    var value = Console.ReadLine();

                    // 根据参数类型转换输入值
                    if (property.Value.TryGetProperty("type", out var type))
                    {
                        switch (type.GetString())
                        {
                            case "integer":
                                if (int.TryParse(value, out var intValue))
                                {
                                    arguments[property.Name] = intValue;
                                }
                                break;
                            case "number":
                                if (float.TryParse(value, out var floatValue))
                                {
                                    arguments[property.Name] = floatValue;
                                }
                                break;
                            case "boolean":
                                if (bool.TryParse(value, out var boolValue))
                                {
                                    arguments[property.Name] = boolValue;
                                }
                                break;
                            default:
                                arguments[property.Name] = value;
                                break;
                        }
                    }
                    else
                    {
                        arguments[property.Name] = value;
                    }
                }
            }

            try
            {
                // 调用工具
                var result = await tool.InvokeAsync(new AIFunctionArguments(arguments));
                Console.WriteLine($"Tool result: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling tool: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Invalid tool index.");
        }
    }
}