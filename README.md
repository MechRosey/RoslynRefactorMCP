# RefactorMCP

RefactorMCP is a Model Context Protocol server that exposes Roslyn-based refactoring tools for C#.

## Usage

Run the console application directly or host it as an MCP server:

```bash
dotnet run --project RefactorMCP.ConsoleApp
```

For usage examples see [EXAMPLES.md](./EXAMPLES.md).

## Available Refactorings

- **Extract Method** – create a new method from selected code and replace the original with a call (expression-bodied methods are not supported).
- **Introduce Field/Parameter/Variable** – turn expressions into new members; fails if a field already exists.
- **Convert to Static** – make instance methods static using parameters or an instance argument.
- **Move Static Method** – relocate a static method and keep a wrapper in the original class.
- **Move Instance Method** – move one or more instance methods to another class and delegate from the source. If a moved method no longer accesses instance members, it is made static automatically. Provide a `methodNames` list along with optional `constructor-injections` and `parameter-injections` to control dependencies.
- **Move Multiple Methods (instance)** – move several methods and keep them as instance members of the target class. The source instance is injected via the constructor when required.
- **Move Multiple Methods (static)** – move multiple methods and convert them to static, adding a `this` parameter.
- **Make Static Then Move** – convert an instance method to static and relocate it to another class in one step.
- **Move Type to Separate File** – move a top-level type into its own file named after the type.
- **Make Field Readonly** – move initialization into constructors and mark the field readonly.
- **Transform Setter to Init** – convert property setters to init-only and initialize in constructors.
- **Constructor Injection** – convert method parameters to constructor-injected fields or properties.
- **Safe Delete** – remove fields or variables only after dependency checks.
- **Extract Class** – create a new class from selected members and compose it with the original.
- **Inline Method** – replace calls with the method body and delete the original.
- **Extract Decorator** – create a decorator class that delegates to an existing method.
- **Create Adapter** – generate an adapter class wrapping an existing method.
- **Add Observer** – introduce an event and raise it from a method.
- **Use Interface** – change a method parameter type to one of its implemented interfaces.
- **List Tools** – display all available refactoring tools as kebab-case names.

Metrics and summaries are also available via the `metrics://` and `summary://` resource schemes.

## Code Quality Tools (Roslynator)

Five Roslynator CLI tools are exposed as MCP tools for diagnostics, formatting, and metrics. These are one-shot operations and do not require `load-solution` first; pass the `.sln` or `.csproj` path directly.

| Tool | Description |
|------|-------------|
| `roslynator-analyze` | Run diagnostics and report issues |
| `roslynator-fix` | Apply auto-fixes (run `roslynator-analyze` first to preview) |
| `roslynator-format` | Whitespace and style formatting |
| `roslynator-spellcheck` | Spelling of identifiers and comments |
| `roslynator-lloc` | Count logical lines of code |
| `roslynator-loc` | Count physical lines of code |
| `roslynator-find-symbol` | Find symbols by kind/visibility; use `unused` flag for dead code detection |
| `roslynator-rename-symbol` | Bulk rename symbols using C# match/newName expressions; dry-run by default |

The Roslynator CLI is included as a git submodule (`Roslynator/`) and is built and published into the output directory automatically when the project is built. No separate installation is required.

If the local binary is not found (e.g. running without a prior build), the tools fall back to a globally installed `roslynator` tool if one is present.

## Contributing

* Run `dotnet test` to ensure all tests pass.
* Format the code with `dotnet format` before opening a pull request.

## License

Licensed under the [Mozilla Public License 2.0](https://www.mozilla.org/MPL/2.0/).
