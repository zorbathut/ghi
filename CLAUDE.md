# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Ghi is an experimental Entity Component System (ECS) built on top of the [Dec](http://github.com/zorbathut/dec) library. It provides:
- Data-driven entity definitions via XML (Dec format)
- High-performance component iteration using runtime IL generation
- Copy-on-write (COW) semantics for efficient environment cloning
- System-based processing with automatic component matching
- Serialization support for save/load functionality

**Note**: This is highly experimental and unpolished. It is not recommended for general use.

## Key Commands

### Building
```bash
# Build entire solution
dotnet build

# Build release configuration
dotnet build -c Release
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with code coverage (as used in CI)
dotnet test -f net6.0 --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

# Run a single test
dotnet test --filter "FullyQualifiedName~Ghi.Test.Systems.Null"
```

## Architecture Overview

### Core Concepts

Ghi implements an ECS pattern where:
1. **Entities** are lightweight handles (ID + generation number) that reference component data
2. **Components** are data structures attached to entities
3. **Systems** are static methods that process components
4. **Environment** manages all entities and their component data

### Key Components

1. **Entity** (`src/Entity.cs`): A struct representing an entity with an ID, generation number, and hash code. Provides methods to access and manipulate components:
   - `Component<T>()` / `TryComponent<T>()`: Get component of type T
   - `SetComponent<T>()`: Set component value
   - `HasComponent<T>()`: Check if entity has component
   - `Components()`: Iterate all components on entity
   - `IsValid()`: Check if entity still exists

2. **Environment** (`src/Environment.cs`): The world container that manages all entities and processes systems. Key features:
   - **Tranches**: Internal storage structure grouping entities by type (EntityDec) with component arrays for cache-friendly iteration
   - **Singletons**: Global component instances shared across all entities
   - **Deferred Operations**: Entity creation/deletion during system execution is deferred until phase end
   - **COW Support**: Tracks `UniqueId` to enable copy-on-write semantics for environment cloning
   - Methods:
     - `Add(EntityDec, components)`: Create new entity
     - `Remove(Entity)`: Delete entity
     - `Process(ProcessDec)`: Execute systems in order
     - `Singleton<T>()`: Access singleton component

3. **Dec Types** (data definitions loaded from XML):
   - **EntityDec** (`src/EntityDec.cs`): Defines entity types and their component composition
   - **ComponentDec** (`src/ComponentDec.cs`): Defines component types, supports `singleton` and `cow` flags
   - **SystemDec** (`src/SystemDec.cs`): References a static `Execute()` method that processes components
   - **ProcessDec** (`src/ProcessDec.cs`): Defines execution order of systems

4. **Cow<T>** (`src/Cow.cs`): Copy-on-write wrapper for efficient environment cloning. When cloning an Environment, COW components are shared until modified:
   - `GetRO()`: Read-only access (no copy)
   - `GetRW()`: Read-write access (clones if needed)
   - `Set(T)`: Replace value entirely

### System Execution Model

Systems are **static methods** with an `Execute()` signature. The Environment uses **runtime IL generation** to:
1. Match system parameters to component types and entities
2. Generate optimized loops over tranches (entity groups)
3. Automatically inject Entity, components (by value or ref), and singletons as parameters

**Example System**:
```csharp
public static class MovementSystem
{
    // Parameters are matched automatically:
    // - Entity: the current entity being processed
    // - ref Position: component passed by reference (can modify)
    // - Velocity: component passed by value (read-only)
    public static void Execute(Entity entity, ref Position pos, Velocity vel)
    {
        pos.x += vel.x;
        pos.y += vel.y;
    }
}
```

The IL generator:
- Identifies which EntityDecs have both Position and Velocity components
- Creates tight loops over those tranches only
- Handles exceptions per-entity to prevent one entity from crashing the system
- Supports singleton injection for global state

### Data Flow

1. **Initialization**: XML → Dec.Parser → EntityDec/ComponentDec/SystemDec/ProcessDec → Environment.Init()
2. **Runtime**: Environment.Process(ProcessDec) → Execute systems in order → Modify components
3. **Entity Operations**:
   - During Idle: Immediate add/remove
   - During Processing: Deferred to phase end (prevents iteration invalidation)
4. **Serialization**: Environment.Record() → Dec.Recorder → XML/binary format

### Tranche System (Performance-Critical)

Entities are stored in **tranches** - arrays grouped by EntityDec type:
```
Tranche for "Player" EntityDec:
  entries: [Entity, Entity, Entity, ...]
  components[0]: [Position, Position, Position, ...]  // Position array
  components[1]: [Health, Health, Health, ...]        // Health array
```

This structure-of-arrays layout enables:
- Cache-friendly iteration (systems access contiguous component arrays)
- Efficient bulk operations
- Minimal memory indirection

### Entity Lifecycle

1. **Creation**: `env.Add(entityDec, components)` → Allocates entity ID → Stores in tranche
2. **Resolution**: Entity structs may be "deferred" during system execution, resolved to real IDs at phase end
3. **Deletion**: `env.Remove(entity)` → Calls `IOnRemove` handlers → Swaps with last element (O(1) removal)
4. **Generation Numbers**: Prevent use-after-free by incrementing on deletion

### Important Constraints

- **COW Limitations**: Cannot return `Cow<T>` from `Entity.Component<T>()` - would need ref returns or internal COW analysis
- **System Parameters**: Must be unambiguous - each parameter type must match exactly one component/singleton
- **Thread Safety**: Not currently supported (PRNG for entity hash codes is not thread-safe)
- **Component Requirements**: Components must implement `Dec.IRecordable` for serialization

### Configuration

- **Config.ProfFactory** (`src/Config.cs`): Optional profiler integration hook (called per system)
- **Environment.EntityToString** (`src/Environment.cs`): Custom entity string representation for debugging
- **Dec.Config.UsingNamespaces**: Namespace search paths for Dec type resolution (set in tests)

## Testing Patterns

Tests are in `test/*.cs` and use NUnit. Common patterns:

```csharp
// 1. Define test decs inline
UpdateTestParameters(new Dec.Config.UnitTestParameters {
    explicitStaticRefs = new System.Type[] { typeof(Decs) }
});
var parser = new Dec.Parser();
parser.AddString(Dec.Parser.FileType.Xml, @"
    <Decs>
        <EntityDec decName=""Player"">
            <components><li>Position</li></components>
        </EntityDec>
    </Decs>
");
parser.Finish();

// 2. Initialize environment
Environment.Init();
var env = new Environment();
using var scope = new Environment.Scope(env);

// 3. Test entity operations
var entity = env.Add(Database<EntityDec>.Get("Player"));
Assert.IsTrue(entity.IsValid());
```

Test base class (`test/Base.cs`) provides:
- `Clean()`: SetUp/TearDown to reset Dec database
- Error/warning validation helpers
- `ProcessEnvMode()`: Helper for testing with/without environment cloning

## Common Development Workflows

### Adding a New Component Type

1. Define component class implementing `Dec.IRecordable`
2. Create ComponentDec in XML or test data
3. Add component to relevant EntityDecs
4. Components are automatically available via `Entity.Component<T>()`

### Adding a New System

1. Create static class with `public static void Execute(...)` method
2. Parameters are auto-matched to components/singletons/Entity
3. Create SystemDec referencing the class
4. Add to ProcessDec order

### Working with Entity Handles

- Entity is a struct - cheap to copy, pass by value
- Always check `IsValid()` if entity might have been deleted
- Hash code is stable across entity lifetime (enables Dictionary keys)
- Deferred entities (created during system execution) resolve automatically
