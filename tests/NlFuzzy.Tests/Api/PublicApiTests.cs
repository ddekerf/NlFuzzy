using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NlFuzzy.Tests.Api;

/// <summary>
/// Guards the public API surface. When this test fails, either the change was unintended, or
/// <c>Api/PublicApi.approved.txt</c> must be updated (and the change documented in CHANGELOG.md).
/// The generated surface is written to <c>PublicApi.received.txt</c> next to the test assembly.
/// </summary>
public class PublicApiTests
{
    [Fact]
    public void PublicApiSurfaceIsUnchanged()
    {
        var actual = GenerateApiSurface(typeof(DutchFuzzy).Assembly);
        var receivedPath = Path.Combine(AppContext.BaseDirectory, "PublicApi.received.txt");
        var approvedPath = Path.Combine(AppContext.BaseDirectory, "Api", "PublicApi.approved.txt");

        File.WriteAllText(receivedPath, actual);

        Assert.True(File.Exists(approvedPath), $"Approved API file not found at '{approvedPath}'. Copy '{receivedPath}' over it.");

        var expected = Normalize(File.ReadAllText(approvedPath));

        Assert.True(
            string.Equals(expected, Normalize(actual), StringComparison.Ordinal),
            $"The public API surface changed. Compare '{approvedPath}' with '{receivedPath}'.");
    }

    [Fact]
    public void EveryPublicTypeLivesInTheNlFuzzyNamespace()
    {
        foreach (var type in typeof(DutchFuzzy).Assembly.GetExportedTypes())
        {
            Assert.StartsWith("NlFuzzy", type.Namespace, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AssemblyIsStrongNamedAgnosticAndDependencyFree()
    {
        var referenced = typeof(DutchFuzzy).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => !name.StartsWith("System", StringComparison.Ordinal)
                && !string.Equals(name, "netstandard", StringComparison.Ordinal)
                && !string.Equals(name, "mscorlib", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(referenced);
    }

    private static string Normalize(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n');

    private static string GenerateApiSurface(Assembly assembly)
    {
        var builder = new StringBuilder();

        foreach (var type in assembly.GetExportedTypes().OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            builder.Append(Describe(type)).Append('\n');

            var members = type
                .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(IsVisible)
                .Select(DescribeMember)
                .Where(text => text.Length > 0)
                .OrderBy(text => text, StringComparer.Ordinal);

            foreach (var member in members)
            {
                builder.Append("    ").Append(member).Append('\n');
            }
        }

        return builder.ToString();
    }

    private static bool IsVisible(MemberInfo member) => member switch
    {
        MethodBase method => !method.IsSpecialName || method is ConstructorInfo,
        FieldInfo field => field.IsPublic,
        _ => true,
    };

    private static string Describe(Type type)
    {
        var kind = type.IsEnum ? "enum"
            : type.IsInterface ? "interface"
            : type.IsValueType ? "struct"
            : "class";

        var modifiers = new List<string> { "public" };

        if (type is { IsAbstract: true, IsSealed: true })
        {
            modifiers.Add("static");
        }
        else if (type.IsSealed && !type.IsValueType && !type.IsEnum)
        {
            modifiers.Add("sealed");
        }

        return $"{string.Join(" ", modifiers)} {kind} {FormatType(type)}";
    }

    private static string DescribeMember(MemberInfo member) => member switch
    {
        ConstructorInfo constructor =>
            $".ctor({FormatParameters(constructor.GetParameters())})",
        MethodInfo method =>
            $"{FormatType(method.ReturnType)} {method.Name}{FormatGenericArguments(method)}({FormatParameters(method.GetParameters())}){(method.IsStatic ? " [static]" : string.Empty)}",
        PropertyInfo property =>
            $"{FormatType(property.PropertyType)} {property.Name} {{ {(property.CanRead ? "get; " : string.Empty)}{(property.CanWrite ? "set; " : string.Empty)}}}",
        FieldInfo field =>
            $"{FormatType(field.FieldType)} {field.Name}{(field.IsLiteral ? " = " + Convert.ToString(field.GetRawConstantValue(), CultureInfo.InvariantCulture) : string.Empty)}",
        EventInfo eventInfo =>
            $"event {FormatType(eventInfo.EventHandlerType!)} {eventInfo.Name}",
        Type => string.Empty,
        _ => string.Empty,
    };

    private static string FormatGenericArguments(MethodInfo method) =>
        method.IsGenericMethodDefinition
            ? "<" + string.Join(", ", method.GetGenericArguments().Select(a => a.Name)) + ">"
            : string.Empty;

    private static string FormatParameters(IEnumerable<ParameterInfo> parameters) =>
        string.Join(", ", parameters.Select(p => $"{FormatType(p.ParameterType)} {p.Name}{(p.HasDefaultValue ? " = ..." : string.Empty)}"));

    private static string FormatType(Type type)
    {
        if (type.IsByRef)
        {
            return "ref " + FormatType(type.GetElementType()!);
        }

        if (type.IsArray)
        {
            return FormatType(type.GetElementType()!) + "[]";
        }

        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        var name = (type.GetGenericTypeDefinition().FullName ?? type.Name);
        var tick = name.IndexOf('`', StringComparison.Ordinal);

        if (tick >= 0)
        {
            name = name.Substring(0, tick);
        }

        return name + "<" + string.Join(", ", type.GetGenericArguments().Select(FormatType)) + ">";
    }
}
