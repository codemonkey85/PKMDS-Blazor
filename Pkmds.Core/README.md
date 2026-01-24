# Pkmds.Core

This library contains PKHeX-related utilities and extensions that are decoupled from the Blazor UI layer. It provides reusable functionality for working with PKHeX.Core.

## Contents

### Extensions

- **PkmExtensions**: Extension methods for PKM objects including:
  - Species validation
  - Marking access and manipulation
  - Type conversion between generations
  - PP (Power Points) management
  - Form argument handling
  - Generation-specific type handling

- **GameInfoExtensions**: Utility methods for game metadata:
  - Move category name lookup

### Utilities

- **ShinyUtils**: Generation-aware shiny Pokémon handling:
  - Safe shiny detection for Gen I/II (DV-based) and Gen III+ (PID-based)
  - Safe shiny setting with proper generation handling

- **MarkingsHelper**: Constants and enums for Pokémon markings:
  - Marking shapes (Circle, Triangle, Square, Heart, Star, Diamond)
  - Unicode symbols for each marking

## Usage

Add a reference to Pkmds.Core in your project:

```xml
<ItemGroup>
  <ProjectReference Include="..\Pkmds.Core\Pkmds.Core.csproj"/>
</ItemGroup>
```

Import the namespaces:

```csharp
using Pkmds.Core.Extensions;
using Pkmds.Core.Utilities;
```

## Dependencies

- PKHeX.Core
- .NET 10.0
