# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository. Standard sections are merged from claudestd (CLAUDE-general.md, CLAUDE-csharp.md).

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

## Interaction Guidelines

**Answer questions before coding**: When asked a question, provide an actual answer first. Don't leap straight to writing code.

**Never commit unless explicitly told to**: Complete the work and leave it uncommitted in the working tree. Only `git commit` when the current request explicitly asks for it ("commit this", "make a checkin"); a phrase like "let's make that a separate checkin" describes how the work should eventually be grouped, not permission to commit it yourself, and permission granted for one task never carries over to the next.

**Split significant work into small self-contained commits**: A significant change lands as a sequence of minimal commits, not one lump. Split along seams that carry meaning, each commit with a one-sentence story — never mechanically per-file or per-layer. The seams that matter:

- A pure refactor of existing code that the feature merely motivated (extracting an interface, collapsing duplicated lookups) is its own commit, landing *before* the feature that wanted it.
- A pre-existing bug fixed along the way is its own commit, however small.
- A behavior change to an existing system is separate from both the refactor that enabled it and the feature that exposed it — behavior changes are the commits people hunt for later.
- A vendored third-party drop stands alone.
- Conversely, keep together what only works together: the halves of a feature that can't be exercised separately, data plus the code that loads it.

Tests go in the commit that makes them meaningful, written against subjects the series doesn't later mutate. Every commit must build and pass the suite on its own — the history should be bisectable. Late fixes (review feedback included) get folded into the commit they belong to via fixup/autosquash, not appended as cleanup commits.

When I've told you to commit the work, apply this by default. When the work stays uncommitted, still build it as one unit in the working tree — but when you finish, point out that it's a good candidate for splitting and propose the commit sequence.

**Evaluate, don't assume**: "Why don't we X?" is a request for evaluation, not a suggestion to do X. Explain the tradeoffs, potential issues, or reasons why X might or might not be a good idea.

**Debug by evidence, not by guess**: When investigating a bug you don't fully understand, prefer adding diagnostic instrumentation or asking focused questions over making speculative changes. A confident theory backed by reading the code is fine to act on; a vibe is not. If a fix doesn't solve the user's problem, that's a signal that the theory was wrong — gather more data before trying again. Two consecutive failed fixes mean stop guessing entirely: pause, instrument, and ask. Rapid-fire blind changes waste the user's attention and erode trust.

**Err on the side of more diagnostic data, not less**: When you ask the user to run something — a probe build, a manual test, a copy-paste session — the expensive part is the round trip itself. The marginal cost of one more printed value, one more covered code path, one more chapter to click is small. So when you instrument, instrument generously: log every variable that could plausibly disambiguate the bug, exercise every endpoint of the parameter space (V=0, V=0.5, V=1, not just whichever was easy), include both the suspected-correct prediction *and* the alternatives so residuals are immediately visible. A diagnostic that prints 30 lines and answers the question on the first try is far cheaper than three diagnostics that each print 3 lines. Make the round trip pay for itself.

**Don't write creative human-facing text unless explicitly told to** (creative projects, e.g. games): That includes dialogue, flavor text, story text, and similar authored prose. When building something that needs such text (e.g. a dialogue system), use really obvious placeholder text — `[Braider Greeting]` or the like. First impressions of written copy are sticky, and the author wants to write it themselves. This is about creative writing, not utilitarian copy — error messages, labels, and log text are fine to write.

**Waiting on background work is not a tool call**: When you've backgrounded a long command (e.g. the full test suite, which exceeds the foreground timeout) and have nothing else productive to do, just end the turn — its completion notification will re-invoke you automatically. Don't emit no-op commands (`echo "waiting"`, re-reads, status pings) to stay "active"; ending on plain text is the correct way to wait, not a hand-off. Conversely, if a command fits the foreground timeout and you'd only wait for it anyway, run it in the foreground so the result returns in the same call. Backgrounding *and* polling is the worst of both.

## Workflow

**Step 1 — Plan.** Enter plan mode (the actual `EnterPlanMode` tool — not a freeform text plan) and research the task and produce a plan. Skippable for trivial changes (under ~a dozen lines). Include unit tests in the plan whenever they're plausible to add — UI generally can't be tested, most other things can.

**Step 2 — Hostile-review the plan.** Before leaving plan mode, spawn a hostile-review agent (run it on Opus — `model: "opus"`) against the plan itself. Brief it like a design reviewer: explain the problem being solved, point it at CLAUDE.md (and the rest of the tree — it can read whatever it needs to research), give it the plan, but do not justify the plan's choices. Give it enough feedback space to actually push back on the approach. Apply the same adjudication rules as the final review (below). Fold valid objections into the plan, then exit plan mode.

**Step 3 — Tests first (when applicable).** For bugfixes, or any feature whose tests can be sensibly written before the implementation exists, write the tests first and verify they fail. Then complete the implementation.

**Step 4 — Run all tests.** Always, even when the change seems unrelated. If anything breaks, return to step 3 — or step 1 if the fix requires significant redesign. For UI changes that can't be unit-tested, explicitly say so rather than claiming success.

Don't treat a failing test as a hard veto on the change. Tests exist to catch *unintentional* drift — a test that pins behavior the change deliberately replaced should be updated alongside the code, not worked around to preserve the old behavior. Fix the test to match the new intent; only fall back to step 3 / step 1 when the failure exposes an actual regression.

**Step 5 — Update CHANGELOG (projects that keep one).** For every even-slightly-user-facing change — new/changed/removed APIs, behavior changes, bugfixes, diagnostics the user sees, doc comments on public members, performance characteristics — add an entry under `[unreleased]` in the appropriate section (Added / Breaking / Improved / Fixed). Purely internal cleanup with no outward effect (private helpers, test-only code, internal comments) can be skipped. When in doubt, add the entry.

**Step 6 — Hostile review.** Spawn a hostile-review agent (run it on Opus — `model: "opus"`). Brief it like a PR reviewer: explain the problem being solved, point it at CLAUDE.md (and the rest of the tree — it can read whatever it needs to research), but do not explain or justify the implementation. Explicitly ask it to **review the general architecture** too, not just the diff — does the chosen approach fit the surrounding code, are there cleaner factorings, does it introduce abstractions that don't pay rent, etc. Give it enough feedback space to cover both the local change and the architectural read effectively (don't cap it to a terse response). Then:
  - If it raises valid objections, fix them. Significant redesign → back to step 1; code changes → back to step 3.
  - If I disagree with an objection, push back once. If it still objects and I'm still confident, surface the disagreement to the user for adjudication rather than looping.
  - Either way — adjudication needed or not — give the user a quick summary of the review at the end.

## Coding Guidelines

**KISS / YAGNI / MVP**: Keep it simple. Write the simplest code that solves the current problem. Include what's necessary, not more. Don't build abstractions, features, or speculative generality that aren't immediately needed. Three similar lines is better than a premature abstraction.

**No backwards compatibility for its own sake**: Remove stubs and dead code completely. If something is unused or being replaced, delete it outright — don't leave shims, renamed `_unused` vars, `// removed` comments, or compatibility re-exports behind. The git history is the backwards compatibility.

**Error handling**:
- Don't add excessive or preemptive error handling. Don't validate everything before it's ever been an issue. Trust internal code and framework guarantees; only validate at system boundaries (user input, external APIs).
- **Silent error handling is banned.** Never swallow exceptions or ignore error conditions. In C# terms: an empty `catch` is a bug. If something fails, it must be reported (via the project's logging facility) or thrown.
- For services that face users, distinguish bugs from user mistakes in your status codes / error types. A user submitting bad input should get a specific, helpful error — not a generic 500-equivalent. Reserve "internal error" responses for actual bugs and infrastructure failures. When adding new features, ask: "Can a user trigger this exception through normal usage?" If yes, return a specific error with a helpful message.

**Don't hand-wrap lines**: One thought, one line — however long. Editors soft-wrap; you don't need to. The only exceptions are:
- **Distinct paragraphs** in a comment: separate with a **blank line** (true paragraph break), not just a `\n`.
- **Structurally-aligned expressions**: one argument per line, one chained call per line, etc.

A multi-sentence single-thought comment is still one line. "It reads better wrapped" is not an exception — that's the rule talking.

**Composition over inheritance**: Prefer building behavior out of small composable pieces (functions, components, properties, modules) over deep class hierarchies. Inheritance is a tool, not a default.

**Data-driven where it pays**: When a category of behavior is open-ended (content, configuration, content variants), prefer data files and a small interpreter over hardcoded code paths. When it's closed and unlikely to grow, just write the code.

## Commenting

A comment earns its place by saying something the code cannot. That's usually one of: a non-obvious "why", a subtle constraint, a surprising choice or tradeoff — or signposting the flow of a long linear process. A one-line summary of what the next chunk of a long function is doing ("Accumulate the asymptotes" over ten lines of dense math; "Resolve overlaps, nearest first" over a loop) is genuinely useful, and often cleaner than extracting that chunk into a function called exactly once. A comment that restates what the name or a single line of code already makes plain is noise.

**Write for a reader who never saw the old code.** A comment that earns its place only by contrast with a previous version — reassuring that a value isn't what it once meant, noting the code no longer does X, explaining that something is "now" done differently — is history in disguise. The tell: it answers a question a fresh reader would never think to ask (nobody wonders whether a `0` expectation is "really a skip" unless they know it once was). State only what's true now, and delete the rest. The one exception is history that constrains the present — a warning against a change someone might actually make ("don't revert this to the double-precision form; it loses the low bits at fixed-point scale") or a deliberate deviation to reconcile later (a clearly-marked local patch to vendored code). That is a load-bearing *why*, not nostalgia; the test is whether the history guards against a real regression or merely explains away a non-question. Police this mainly in your own new comments — by the removal asymmetry below, don't strip others' on suspicion alone.

**Adding — be conservative, but signpost freely.** Don't narrate the obvious: a `bool allowFlips` field needs no `// when false, flipping is disabled`; a `// set the flip` above `flip = …` adds nothing. Reach first for self-explanatory code (good names, clear structure), and comment the part code can't carry — usually the *why*, not the *what*. The exception is flow signposting in long procedures, where a sparse trail of one-line "what next" headers is a real readability win; use them. When you explain a "why", keep it tight; one good line beats a paragraph. Don't reference the current task, fix, or callers ("used by X", "added for the Y flow", "handles the case from issue #123") — those belong in the commit message and rot as the codebase evolves.

**Removing — be generous about keeping.** Existing comments are there for a reason. If one is out of date or actively misleading, fix or remove it. Otherwise leave it alone — don't strip a comment just because it explains the "what", or because you wouldn't have written it yourself. The asymmetry is deliberate: conservative about adding your own, generous about keeping others'.

## Naming

**C# conventions**:
- **PascalCase**: Public members, types, static fields
- **camelCase**: Private/protected fields, parameters
- **Interface prefix**: `I` (e.g., `IComponent`)

**Category-instance prefix**: When a name combines a category with an instance, put the category first so related names group alphabetically and the category reads as the classification. `SpawnerBurst`, `ShapeRadial`, `AttackStart()` — not `BurstSpawner`, `RadialShape`, `StartAttack()`. The category is the "kind of thing"; the instance is the specific variant. Apply this to types, functions, files, and config keys alike.

## C# Style

**Always use braces**: Always include `{}` for `if`, `else`, `for`, `foreach`, `while`, etc., even for single-line bodies.
```csharp
// Good
if (condition)
{
    return;
}

// Bad
if (condition) return;
if (condition)
    return;
```

**Avoid expression-bodied members (`=>`)**: Prefer block bodies with explicit `return` statements for methods and properties. Expression bodies obscure control flow.
```csharp
// Good
public int GetValue()
{
    return value;
}

// Bad
public int GetValue() => value;
```

**Usings**: Where implicit usings are disabled, all `using` directives must be explicit and alphabetized at the top of each file.

## Default Parameters and Overloads

The deciding axis is the *nature of the parameter*, not the mechanism. A default parameter is the right tool for a conceptually optional thing; an overload is for a signature that is genuinely different, not "a default parameter wearing a funny hat."

- **A new parameter the function genuinely needs is mandatory.** Add it without a default and update the call sites. Don't reflexively give every new parameter a default value just to avoid touching callers — that's the main thing this rule exists to prevent.
- **A default value is for a *conceptually optional* parameter** — one with a principled "absent" value: a nullable callback or override (`Action onDone = null`, `Func<MapRect2Q, bool> filter = null`), or a natural identity like `double steepness = 1.0`. It is *not* for an arbitrary tuning constant that merely happens to suit most callers — something like `attemptsPerIteration = 30` should be mandatory or a named constant, not a default.
- **Overloads are for genuinely different signatures** — different parameter *types* or *shapes* that can't collapse into one signature (`Lerp(float …)` vs `Lerp(Q32 …)`; `From(map, Vector2Q)` vs `From(map, Q32 x, Q32 y)`), or a meaningfully different operation. Do not write an overload pair whose only difference is that one omits a trailing optional argument — use a default parameter instead. (For example, `Sigmoid(x)` + `Sigmoid(x, steepness)` should collapse to one `Sigmoid(double x, double steepness = 1.0)` — the natural-identity case above.)
- **A behavior-switching bool may be a default-`false` parameter only when it's a rider on the same operation** — the result is the same kind of thing, the flag just tweaks a side aspect, and it's almost always off ("sweep, *and while you're at it* ignore platforms": `Sweep(…, bool ignorePlatforms = false)`). When the flag changes *what the function fundamentally means* — the question it answers — it shouldn't be a flag: make it a separate, differently-named function, or handle it at the call site. Name any split function category first, per Naming above.
- **Hard exception**: compiler-attribute parameters (`[CallerFilePath]`, `[CallerLineNumber]`, `[CallerMemberName]`) must be default parameters — there is no overload form, so these don't count against the rule.

## Critical Rules

1. **Always use absolute paths** in file operations. Relative paths break under tooling that runs from a different working directory than expected.
2. **Tests live next to the code they test** in spirit even if not in directory layout. New code without tests is a debt that compounds.

## Testing

Run the full test suite on every change, not just the tests you think are related — that's the whole point of having a suite. If the project ships with a watch mode or a fast subset, prefer that during the inner loop, but the final pre-commit step is the full run.

Write tests against the *seam* you actually want to defend — pure functions, deterministic state machines, parsers, classifiers — and don't try to retrofit unit tests around UI, rendering, or process-orchestration code that has no testable seam. For those, say so explicitly when reporting status, and rely on a manual smoke instead of pretending coverage you don't have.

Integration tests that spin up real dependencies (databases, message brokers, container runtimes) are usually worth the slowness over mocks: mocks pass when the contract drifts, real dependencies fail loudly. When mocking is unavoidable, mock at the *outermost* boundary you reasonably can.

**Don't pin user-facing copy in tests.** Test text-producing logic relationally instead of asserting exact strings: capture outputs and assert relations between them — non-empty, distinct across states that should read differently, equal across states that should read the same (priority/stability), exact match only for sentinels like `""`. This lets copy be rewritten (or moved to data files) without touching tests. Exact-match assertions remain correct when the exact output *is* the contract (serialization, parsers, formatters) or when the string is fixture data the test itself authored — this rule is about production copy shown to end users. Apply when writing or modifying tests; don't mass-convert existing tests unless asked.

## Executing Actions with Care

Carefully consider the reversibility and blast radius of actions. Local, reversible actions (editing files, running tests, running ephemeral scripts) are fine to take freely. But for actions that are hard to reverse, affect shared systems beyond the local environment, or touch production, **confirm first**.

Examples that warrant confirmation:
- Destructive: deleting files/branches, dropping tables, killing processes, `rm -rf`, overwriting uncommitted changes.
- Hard to reverse: force-push, `git reset --hard`, amending published commits, dependency downgrades, CI/CD changes.
- Visible to others: pushing code, creating/closing PRs or issues, sending messages, posting to external services.
- Uploading content to third-party tools (pastebins, diagram renderers): may be cached or indexed even if later deleted.

When you encounter an obstacle, do not use destructive actions as a shortcut to make it go away. Identify root causes; don't bypass safety checks (`--no-verify`, `--no-gpg-sign`) unless the user has explicitly asked. If you discover unexpected state — unfamiliar files, branches, locks — investigate before deleting; it may be the user's in-progress work.

## Tone for Updates

Match response length to the task. A simple question gets a direct answer, not headers and sections. End-of-turn summaries should be one or two sentences — what changed and what's next. Don't narrate internal deliberation; state results and decisions directly. Brief is good; silent is not.

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
   - **Process spans**: `IsProcessing` / `IsMutating` report, across the whole `Process()` call, whether a process is running and whether it may be changing recorded state. `Record()` reports an error mid-`IsMutating` but stays quiet during a process declared `constant`, which is what makes mid-frame checksumming possible.

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
