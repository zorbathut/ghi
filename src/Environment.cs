using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Dec;

namespace Ghi
{
    public class Environment : Dec.IRecordable, Dec.IPostCloneNew, Dec.IPostCloneOriginal
    {
        private const int BaseArraySize = 16;

        public static System.Threading.ThreadLocal<Environment> Current = new();
        public struct Scope : IDisposable
        {
            private Environment old;
            private Environment current;
            public Scope(Environment env)
            {
                old = Current.Value;
                Current.Value = env;
                current = env;
            }

            public void Dispose()
            {
                Assert.AreSame(Current.Value, current);
                Current.Value = old;
            }
        }

        internal struct Tranche : Dec.IRecordable
        {
            public List<Entity> entries;    // this length is canonical
            public Array[] components;  // these grow as needed, but often include padding

            // metadata
            public EntityDec entity;
            public Type[] componentTypes;

            public void Record(Dec.Recorder recorder)
            {
                if (recorder.Intent == Recorder.Purpose.Cloning)
                {
                    // just duplicate; null entries means this tranche was never initialized
                    recorder.Record(ref entries, "entries");
                    recorder.Record(ref components, "components");

                    recorder.Record(ref entity, "entity");
                    recorder.Record(ref componentTypes, "componentTypes");
                }
                else if (recorder.Intent == Recorder.Purpose.Checksum || recorder.Mode == Recorder.Direction.Write)
                {
                    // put at the top just to make the savefile more readable
                    recorder.Record(ref entity, "entity");

                    if (entries == null)
                    {
                        // uninitialized tranche, write nulls
                        recorder.Record(ref components, "components");
                        recorder.Record(ref entries, "entries");
                        recorder.Record(ref componentTypes, "componentTypes");
                    }
                    else
                    {
                        // compile it down into an actual array
                        Array[] writeComponents = new Array[components.Length];
                        for (int j = 0; j < components.Length; ++j)
                        {
                            var compArray = Array.CreateInstance(components[j].GetType().GetElementType(), entries.Count);
                            for (int i = 0; i < entries.Count; ++i)
                            {
                                compArray.SetValue(components[j].GetValue(i), i);
                            }
                            writeComponents[j] = compArray;
                        }
                        recorder.Record(ref writeComponents, "components");

                        // we want to write only up to the active components length
                        recorder.Record(ref entries, "entries");
                        recorder.Record(ref componentTypes, "componentTypes");  // this is kind of redundant with the component arrays honestly
                    }
                }
                else if (recorder.Mode == Recorder.Direction.Read)
                {
                    // and the opposite of that. we're not actually padding the array because I'm lazy
                    recorder.Record(ref entries, "entries");
                    recorder.Record(ref components, "components");

                    recorder.Record(ref entity, "entity");
                    recorder.Record(ref componentTypes, "componentTypes");
                }
            }
        }
        private Tranche[] tranches;

        private struct EntityLookup : Dec.IRecordable
        {
            public Ghi.EntityDec dec;
            public int index;
            public int gen;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref dec, "dec");
                recorder.Record(ref index, "index");
                recorder.Record(ref gen, "gen");
            }
        }
        private List<EntityLookup> entityLookup = new();
        private List<int> entityFreeList = new();

        private static Ghi.EntityDec[] indexToEntityDec;

        private object[] singletons;
        private Dictionary<Type, int> singletonLookup = new();

        // Monotonic counter used to assign Entity.stableId at creation. Deterministic across cloned environments because it's serialized with env state.
        // Starts at an arbitrary non-zero value so default(Entity) (stableId == 0) doesn't collide with any real entity's hash.
        // Not thread-safe; will need revisiting alongside multithread support.
        private int stableIdCounter = 0x7e117a1e;

        // if someone makes more than 64 bits of Environments then I salute you
        // we start at 1 because Cow, as a struct, will sometimes initialize to 0.
        // I *think* that's okay and would not cause actual problems
        // but, uh, it might!
        private static long s_LastUniqueId = 1;
        private long uniqueId = System.Threading.Interlocked.Increment(ref s_LastUniqueId);
        public long UniqueId
        {
            get
            {
                return uniqueId;
            }
        }

        // bump!
        // this allows our COW structures to recognize that they may now be shared
        // this effectively locks every existing COW class at its current state for eternity; if it changes it, it'll be by cloning it first
        public void PostCloneOriginal()
        {
            uniqueId = System.Threading.Interlocked.Increment(ref s_LastUniqueId);
        }
        public void PostCloneNew()
        {
            uniqueId = System.Threading.Interlocked.Increment(ref s_LastUniqueId);
        }

        // Status
        //
        // Two things vary independently here and both matter, so the reachable combinations are spelled out rather than tracked as separate flags: whether a system is mid-execution (which is what makes entity operations defer to phase end), and whether the running process declared itself constant (which is what makes recording safe, and makes any change to recorded state a declaration violation).
        private enum Status
        {
            // No process running. Entity operations apply immediately, recording is safe.
            Idle,

            // A constant process is running, but no system is mid-execution - we're before the first system, after the last, or in the phase-end action loop. Entity operations apply immediately.
            ProcessConstant,

            // As ProcessConstant, for a process that may change recorded state.
            ProcessMutating,

            // A system belonging to a constant process is executing. Entity operations defer to phase end.
            SystemConstant,

            // As SystemConstant, for a process that may change recorded state.
            SystemMutating,
        }

        // Deliberately not unwound with try/finally, so don't add one. Every callout Process makes to host code - systems, IOnRemove handlers, the profiler scope - routes its exceptions to Dbg.Ex, so the only remaining way out of Process is a host error handler that throws instead of returning. A host that does that has chosen to abort mid-process, and the environment stays wedged in whatever state it was in, refusing to run further processes, until it's discarded.
        private Status status = Status.Idle;

        // True for the entire Process() call, including the windows between systems where the phase-end actions run.
        public bool IsProcessing
        {
            get
            {
                return status != Status.Idle;
            }
        }

        // True while a process that hasn't declared itself constant is running; i.e. true when the recorded state of this environment may be partway through changing, and recording it would catch that.
        //
        // This is a snapshot, not a lock: reading false doesn't stop another thread from starting a mutating process a moment later, and a record is not instantaneous. A caller reading this from off-thread is responsible for arranging that no mutating process can start while it works. Note also that this only covers processes - a direct Add/Remove/SetComponent/SingletonSet outside of one mutates the environment with this unset.
        public bool IsMutating
        {
            get
            {
                // one read, so a process boundary partway through can't produce an answer that was never true
                var current = status;
                return current == Status.ProcessMutating || current == Status.SystemMutating;
            }
        }

        // Whether the running process declared itself constant, and therefore whether changing recorded state right now is a violation of that declaration.
        internal bool IsConstantProcess
        {
            get
            {
                return status == Status.ProcessConstant || status == Status.SystemConstant;
            }
        }

        // Whether a system is mid-execution, and therefore whether entity operations have to defer to phase end rather than editing tranches we're in the middle of iterating.
        private bool IsInSystem
        {
            get
            {
                return status == Status.SystemConstant || status == Status.SystemMutating;
            }
        }

        // Phase-end deferral
        internal class EntityDeferred
        {
            public EntityDec dec;
            public Tranche tranche;

            public Entity replacement;

            public (EntityDec dec, Tranche tranche, int index) Get()
            {
                return (dec, tranche, 0);
            }
        }
        private List<Action> phaseEndActions = new();

        // The update data that we use for List<>
        private List<Entity> currentEntityAdded = new();
        private HashSet<Entity> currentEntityRemoved = new();

        // Config
        internal static Func<Entity, string> EntityToString = null;

        public int Count
        {
            get
            {
                return tranches.Select(t => t.entries?.Count ?? 0).Sum();
            }
        }

        public IEnumerable<Entity> List
        {
            get
            {
                return tranches.Where(t => t.entries != null).SelectMany(t => t.entries).Concat(currentEntityAdded).Except(currentEntityRemoved);
            }
        }

        public static void Init()
        {
            var DbgEx = typeof(Dbg).GetMethod("Ex", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            // Decided once, here, so a single Init() is internally consistent: every system either emits or doesn't.
            bool useEmit = Config.ShouldEmit;

            indexToEntityDec = Dec.Database<Ghi.EntityDec>.List.OrderBy(dec => dec.DecName).ToArray();
            foreach((var dec, int i) in indexToEntityDec.Select((dec, i) => (dec, i)))
            {
                dec.index = i;
            }

            // cache some data we can use
            var allEntities = Dec.Database<EntityDec>.List.OrderBy(dec => dec.index).ToArray();
            var allComponents = Dec.Database<ComponentDec>.List.OrderBy(cd => cd.DecName).ToArray();
            var allSingletons = Dec.Database<ComponentDec>.List.Where(cd => cd.singleton).OrderBy(cd => cd.DecName).ToArray();

            // set up SystemDec processes
            foreach (var dec in Dec.Database<SystemDec>.List)
            {
                var method = dec.method;
                if (method == null)
                {
                    // we have presumably already generated an error; just stub it out
                    dec.process = (tranches, singletons, action) => { };
                    continue;
                }

                var parameters = method.GetParameters().Select(param => param.ParameterType).ToArray();
                var parametersBare = parameters.Select(param => param.IsByRef ? param.GetElementType() : param).ToArray();

                {
                    // accumulate our entire match DB
                    var parameterDirectMatches = parametersBare
                        .Select(param => allComponents
                            .Select((c, i) => ( c, i ))
                            .Where(c => param.IsAssignableFrom(c.c.GetComputedType()))
                            .ToArray())
                        .ToArray();

                    // first, see if this can be mapped to all-singleton
                    if (parameterDirectMatches.All(matches => matches.Length == 1 && matches[0].c.singleton))
                    {
                        // it can!
                        // somewhat surprised tbqh

                        // figure out, per parameter, which singleton slot it reads from
                        var singletonSources = new int[parameters.Length];
                        for (int i = 0; i < parameters.Length; ++i)
                        {
                            singletonSources[i] = allSingletons.FirstIndexOf(singleton => singleton == parameterDirectMatches[i][0].c);
                        }

                        dec.process = useEmit
                            ? BuildSingletonProcessEmit(dec.DecName, method, singletonSources, DbgEx)
                            : BuildSingletonProcessReflect(method, singletonSources);

                        // NEXT.
                        continue;
                    }
                    else if (parameterDirectMatches.Any(matches => matches.All(m => !m.c.singleton)))
                    {
                        // definitely can't be a singleton match, so we're OK with it
                    }
                    else if (parameterDirectMatches.Any(matches => matches.Length > 1))
                    {
                        var ambiguity = string.Join("; ", parameters.Zip(parameterDirectMatches, (param, matches) => ( param, matches ))
                            .Where(x => x.matches.Length > 1)
                            .Select(x => $"{x.param} matches [{string.Join(", ", x.matches.Select(m => m.c.GetComputedType().ToString()))}]"));

                        Dbg.Err($"{dec}: Ambiguity in singleton scan! {ambiguity}");
                    }
                }

                // for each tranche, we need to see if it applies . . .
                var trancheDat = new List<(int trancheId, (int from, int to)[] singletonRemap, (int from, int to)[] trancheRemap)>();

                for (int trancheId = 0; trancheId < allEntities.Length; ++trancheId)
                {
                    // see if we can find an unambiguous mapping, including all singletons and every one of our component types
                    var availableComponents = allSingletons.Concat(allEntities[trancheId].components).ToArray();
                    var parameterTrancheMatches = parametersBare
                        .Select(param => availableComponents
                            .Select((c, i) => (c.GetComputedType(), i))
                            .Concat(Enumerable.Repeat((typeof(Entity), -1), 1))
                            .Where(c => param.IsAssignableFrom(c.Item1))
                            .Select(c => c.Item2)
                            .ToArray())
                        .ToArray();

                    if (parameterTrancheMatches.All(matches => matches.Length == 1))
                    {
                        // our singletons are first, followed by possible components, so we need to map these up as appropriate
                        // as we do this, we build a remap array for us to rapidly remap things
                        List<(int from, int to)> singletonRemaps = new List<(int from, int to)>();
                        List<(int from, int to)> trancheRemaps = new List<(int from, int to)>();
                        for (int j = 0; j < parameters.Length; ++j)
                        {
                            if (parameterTrancheMatches[j][0] == -1)
                            {
                                // this is Entity
                                trancheRemaps.Add((-1, j));
                            }
                            else if (parameterTrancheMatches[j][0] < allSingletons.Length)
                            {
                                // this is a singleton
                                singletonRemaps.Add(( parameterTrancheMatches[j][0], j ));
                            }
                            else
                            {
                                // this is a component
                                trancheRemaps.Add(( parameterTrancheMatches[j][0] - allSingletons.Length, j ));
                            }
                        }

                        // compile this down for efficiency
                        var singletonRemapArray = singletonRemaps.OrderBy(remap => remap.from).ToArray();
                        var trancheRemapArray = trancheRemaps.OrderBy(remap => remap.from).ToArray();

                        trancheDat.Add((trancheId, singletonRemapArray, trancheRemapArray));
                    }
                    else if (parameterTrancheMatches.Any(matches => matches.Length > 1))
                    {
                        var ambiguity = string.Join("; ", parameters.Zip(parameterTrancheMatches, (param, matches) => ( param, matches ))
                            .Where(x => x.matches.Length > 1)
                            .Select(x => $"{x.param} matches [{string.Join(", ", x.matches.Select(m => m.ToString()))}]"));

                        Dbg.Err($"{dec}: Ambiguity in entity scan! {ambiguity}");
                    }
                }

                if (trancheDat.Count != 0)
                {
                    // verify that singletons match on each one
                    for (int i = 1; i < trancheDat.Count; ++i)
                    {
                        Assert.AreEqual(trancheDat[0].singletonRemap, trancheDat[i].singletonRemap);
                    }

                    // we put singletons in a single array so we can do it exactly once
                    var singletonLookup = trancheDat[0].singletonRemap;
                    var trancheLookups = trancheDat.Select(tdo => (tdo.trancheId, tdo.trancheRemap)).ToArray();

                    dec.process = useEmit
                        ? BuildTrancheProcessEmit(dec.DecName, method, parameters, singletonLookup, trancheLookups, allEntities, DbgEx)
                        : BuildTrancheProcessReflect(method, parameters, singletonLookup, trancheLookups);
                }
                else
                {
                    // this can really be refined more
                    Dbg.Err($"No entity type matches when attempting to run system {dec}!");

                    // give it a no-op function
                    dec.process = (tranches, singletons, action) => { };
                }
            }
        }

        // Singleton-only systems: every parameter is satisfied by a singleton, so there's no per-entity iteration.
        // Emitted variant: read each singleton out of the array and call straight through.
        private static Action<Tranche[], object[], Action> BuildSingletonProcessEmit(
            string systemName,
            System.Reflection.MethodInfo method,
            int[] singletonSources,
            System.Reflection.MethodInfo dbgEx)
        {
            // build our artificial IL function
            var dynamicMethod = new DynamicMethod($"ExecuteSystem{systemName}",
                typeof(void),
                new Type[] { typeof(Tranche[]), typeof(object[]), typeof(Action) },
                true);
            System.Reflection.Emit.ILGenerator il = dynamicMethod.GetILGenerator();

            // needs to start and end with the same number of parameters, so let's just do this within the exception block
            il.BeginExceptionBlock();

            // read all the singletons
            for (int i = 0; i < singletonSources.Length; ++i)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, singletonSources[i]);
                il.Emit(OpCodes.Ldelem_Ref);
            }
            il.Emit(OpCodes.Call, method);

            il.BeginCatchBlock(typeof(Exception));
            // whoops something went wrong
            il.Emit(OpCodes.Call, dbgEx);
            il.EndExceptionBlock();

            // clean up
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Callvirt, typeof(Action).GetMethod("Invoke"));

            // and we're done with the singleton-only path!
            il.Emit(OpCodes.Ret);

            return (Action<Tranche[], object[], Action>)dynamicMethod.CreateDelegate(typeof(Action<Tranche[], object[], Action>));
        }

        // Reflection variant of the singleton-only path; equivalent to BuildSingletonProcessEmit without dynamic code.
        private static Action<Tranche[], object[], Action> BuildSingletonProcessReflect(
            System.Reflection.MethodInfo method,
            int[] singletonSources)
        {
            return (tranches, singletons, action) =>
            {
                try
                {
                    var args = new object[singletonSources.Length];
                    for (int i = 0; i < singletonSources.Length; ++i)
                    {
                        args[i] = singletons[singletonSources[i]];
                    }
                    method.Invoke(null, args);
                }
                catch (System.Reflection.TargetInvocationException e)
                {
                    // unwrap so we report the system's own exception, matching the emitted path
                    Dbg.Ex(e.InnerException ?? e);
                }
                catch (Exception e)
                {
                    Dbg.Ex(e);
                }

                action();
            };
        }

        // General systems: iterate every matching tranche and call the system once per entity, feeding it singletons,
        // components (by value or ref), and the Entity handle as appropriate.
        // Emitted variant: specialize a tight loop per tranche.
        private static Action<Tranche[], object[], Action> BuildTrancheProcessEmit(
            string systemName,
            System.Reflection.MethodInfo method,
            Type[] parameters,
            (int from, int to)[] singletonLookup,
            (int trancheId, (int from, int to)[] trancheRemap)[] trancheLookups,
            EntityDec[] allEntities,
            System.Reflection.MethodInfo dbgEx)
        {
            // build our artificial IL function
            var dynamicMethod = new DynamicMethod($"ExecuteSystem{systemName}",
                typeof(void),
                new Type[] { typeof(Tranche[]), typeof(object[]), typeof(Action) },
                true);
            System.Reflection.Emit.ILGenerator il = dynamicMethod.GetILGenerator();

            // yank singletons out and apply appropriate casting
            // we're making an array based on our parameter order so we can fill it in later
            Action<int>[] singletonLookups = new Action<int>[parameters.Length];
            for (int i = 0; i < singletonLookup.Length; ++i)
            {
                il.Emit(OpCodes.Ldarg_1);

                // this can be more optimized for size
                il.Emit(OpCodes.Ldc_I4, singletonLookup[i].from);

                // use a ref so we're not copying structs around
                il.Emit(OpCodes.Ldelem_Ref);

                var local = il.DeclareLocal(parameters[singletonLookup[i].to]);
                il.Emit(OpCodes.Stloc, local);

                singletonLookups[singletonLookup[i].to] = index =>
                {
                    il.Emit(OpCodes.Ldloc, local);
                };
            }

            // singletons should now be in an appropriate type, and local, which is probably the fastest solution
            // but there might be better options!

            // now loop through all the tranches, we'll generate IL for each one
            for (int i = 0; i < trancheLookups.Length; ++i)
            {
                // we'll be using temp values that we want to eliminate after this, so we'll just use a scope for it
                // whoops we can't do that
                // well uh
                // figure this out later
                //il.BeginScope();

                int trancheId = trancheLookups[i].trancheId;

                // skip this tranche if it hasn't been created yet (entries is null)
                var trancheEnd = il.DefineLabel();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, trancheId);
                il.Emit(OpCodes.Ldelema, typeof(Tranche));
                il.Emit(OpCodes.Ldfld, typeof(Tranche).GetField("entries"));
                il.Emit(OpCodes.Brfalse, trancheEnd);

                // first set up the arrays
                var trancheRemapArray = trancheLookups[i].trancheRemap;

                // remap the components
                Action<int>[] lookups = (Action<int>[])singletonLookups.Clone();
                for (int j = 0; j < trancheRemapArray.Length; ++j)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, trancheId);
                    il.Emit(OpCodes.Ldelema, typeof(Tranche));

                    int from = trancheRemapArray[j].from;
                    if (from == -1)
                    {
                        // grab the entries field
                        il.Emit(OpCodes.Ldfld, typeof(Tranche).GetField("entries"));

                        // shove this into another local
                        var local = il.DeclareLocal(typeof(List<Entity>));
                        il.Emit(OpCodes.Stloc, local);

                        // and then we'll just use a lambda to grab it later
                        lookups[trancheRemapArray[j].to] = index =>
                        {
                            il.Emit(OpCodes.Ldloc, local);
                            il.Emit(OpCodes.Ldloc, index);
                            il.Emit(OpCodes.Callvirt, typeof(List<Entity>).GetMethod("get_Item"));
                        };
                    }
                    else
                    {
                        // grab the appropriate components array
                        il.Emit(OpCodes.Ldfld, typeof(Tranche).GetField("components"));
                        il.Emit(OpCodes.Ldc_I4, from);
                        il.Emit(OpCodes.Ldelem_Ref);

                        // get the appropriate array type so we can avoid casts at runtime
                        // we pull this out of the entity type, not our parameter types; implicit casting on the function call is (probably?) cheaper than messing around with arrays
                        var itemType = allEntities[trancheId].components[from].GetComputedType();
                        var arrayType = itemType.MakeArrayType();
                        var local = il.DeclareLocal(arrayType);
                        il.Emit(OpCodes.Castclass, arrayType);
                        il.Emit(OpCodes.Stloc, local);

                        // and then we'll just use a lambda to grab it later
                        var parameter = parameters[trancheRemapArray[j].to];
                        lookups[trancheRemapArray[j].to] = index =>
                        {
                            il.Emit(OpCodes.Ldloc, local);
                            il.Emit(OpCodes.Ldloc, index);

                            if (parameter.IsByRef)
                            {
                                il.Emit(OpCodes.Ldelema, itemType);
                            }
                            else
                            {
                                il.Emit(OpCodes.Ldelem, itemType);
                            }
                        };
                    }
                }

                // Store the length of the entries array, this is our loop length

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, trancheId);
                il.Emit(OpCodes.Ldelema, typeof(Tranche));
                il.Emit(OpCodes.Ldfld, typeof(Tranche).GetField("entries"));
                il.Emit(OpCodes.Callvirt, typeof(List<Entity>).GetProperty("Count").GetGetMethod());
                var entitylistlen = il.DeclareLocal(typeof(int));
                il.Emit(OpCodes.Stloc, entitylistlen);

                // working index
                var index = il.DeclareLocal(typeof(int));
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc, index);

                // loop positions
                var loopStart = il.DefineLabel();
                var loopEnd = il.DefineLabel();

                // start of the loop
                il.MarkLabel(loopStart);

                // Compare index with entitylistlen
                il.Emit(OpCodes.Ldloc, index);
                il.Emit(OpCodes.Ldloc, entitylistlen);

                il.Emit(OpCodes.Bge, loopEnd); // If index >= entitylistlen, jump to loopEnd

                // Get ready to make the actual call
                // needs to start and end with the same number of parameters, so let's just do this within the exception block
                il.BeginExceptionBlock();

                // read all the parameters
                for (int paramIndex = 0; paramIndex < lookups.Length; ++paramIndex)
                {
                    lookups[paramIndex](index.LocalIndex);
                }
                il.Emit(OpCodes.Call, method);

                il.BeginCatchBlock(typeof(Exception));
                // whoops something went wrong
                il.Emit(OpCodes.Call, dbgEx);
                il.EndExceptionBlock();

                // clean up our notes on which objects exist
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, typeof(Action).GetMethod("Invoke"));

                // Increment the loop index
                il.Emit(OpCodes.Ldloc, index);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, index);

                // Jump back to the start of the loop
                il.Emit(OpCodes.Br, loopStart);

                // Mark the end of the loop
                il.MarkLabel(loopEnd);

                // Mark the end of the tranche (skip target for null entries)
                il.MarkLabel(trancheEnd);
            }

            // we done!
            il.Emit(OpCodes.Ret);

            return (Action<Tranche[], object[], Action>)dynamicMethod.CreateDelegate(typeof(Action<Tranche[], object[], Action>));
        }

        // Reflection variant of the general path; equivalent to BuildTrancheProcessEmit without dynamic code.
        private static Action<Tranche[], object[], Action> BuildTrancheProcessReflect(
            System.Reflection.MethodInfo method,
            Type[] parameters,
            (int from, int to)[] singletonLookup,
            (int trancheId, (int from, int to)[] trancheRemap)[] trancheLookups)
        {
            // Which parameters are by-ref, so we know whether to copy a (possibly mutated) component back into its array.
            var byRef = new bool[parameters.Length];
            for (int i = 0; i < parameters.Length; ++i)
            {
                byRef[i] = parameters[i].IsByRef;
            }

            return (tranches, singletons, action) =>
            {
                var args = new object[parameters.Length];

                // singletons are the same for every entity, so resolve them once
                for (int i = 0; i < singletonLookup.Length; ++i)
                {
                    args[singletonLookup[i].to] = singletons[singletonLookup[i].from];
                }

                for (int t = 0; t < trancheLookups.Length; ++t)
                {
                    var tranche = tranches[trancheLookups[t].trancheId];

                    // skip this tranche if it hasn't been created yet (entries is null)
                    if (tranche.entries == null)
                    {
                        continue;
                    }

                    var trancheRemap = trancheLookups[t].trancheRemap;
                    int count = tranche.entries.Count;
                    for (int index = 0; index < count; ++index)
                    {
                        // fill in this entity's per-entity parameters (Entity handle or component value)
                        for (int j = 0; j < trancheRemap.Length; ++j)
                        {
                            int from = trancheRemap[j].from;
                            if (from == -1)
                            {
                                args[trancheRemap[j].to] = tranche.entries[index];
                            }
                            else
                            {
                                args[trancheRemap[j].to] = tranche.components[from].GetValue(index);
                            }
                        }

                        try
                        {
                            method.Invoke(null, args);

                            // copy back any by-ref components so ref mutations stick, matching the emitted path's in-place writes
                            for (int j = 0; j < trancheRemap.Length; ++j)
                            {
                                int from = trancheRemap[j].from;
                                if (from != -1 && byRef[trancheRemap[j].to])
                                {
                                    tranche.components[from].SetValue(args[trancheRemap[j].to], index);
                                }
                            }
                        }
                        catch (System.Reflection.TargetInvocationException e)
                        {
                            // unwrap so we report the system's own exception, matching the emitted path
                            Dbg.Ex(e.InnerException ?? e);
                        }
                        catch (Exception e)
                        {
                            Dbg.Ex(e);
                        }

                        action();
                    }
                }
            };
        }

        public Environment()
        {
            // a lot of this stuff really shouldn't happen if we're being dec-constructed; worry about that later

            // I'm not worried about singleton inheritance yet
            var singletonTypes = Dec.Database<ComponentDec>.List.Where(cd => cd.singleton).OrderBy(cd => cd.DecName).ToArray();
            singletonLookup = singletonTypes.Select((cd, i) => (type: cd.GetComputedType(), i)).ToDictionary(x => x.type, x => x.i);

            singletons = new object[singletonTypes.Length];
            foreach ((var dec, var i) in singletonTypes.Select((cd, i) => (cd, i)))
            {
                singletons[i] = Activator.CreateInstance(dec.GetComputedType());
            }

            // create tranches array; individual tranches are created lazily on first entity add
            tranches = new Tranche[Dec.Database<EntityDec>.List.Length];

            // create status
            status = Status.Idle;
        }

        private Tranche CreateNewTranche(EntityDec dec)
        {
            var tranche = new Tranche();
            tranche.entries = new List<Entity>();
            tranche.components = new Array[dec.components.Count];
            tranche.entity = dec;
            tranche.componentTypes = dec.components.Select(c => c.GetComputedType()).ToArray();

            for (int i = 0; i < dec.components.Count; ++i)
            {
                // arbitrarily hardcoded starting size; should this be bigger? smaller? who can say! it is a mystery
                // probably shouldn't actually matter tbqh
                tranche.components[i] = Array.CreateInstance(dec.components[i].GetComputedType(), BaseArraySize);
            }

            return tranche;
        }

        private object[] FillComponents(EntityDec dec, object[] providedComponents)
        {
            bool[] used = null;
            if (providedComponents != null)
            {
                used = new bool[providedComponents.Length];
            }

            // this is kind of inefficient but I'm just living with the perf hit for the time being
            var components = new object[dec.components.Count];
            for (int i = 0; i < dec.components.Count; ++i)
            {
                // see if we have a provided component
                int match = -1;

                if (providedComponents != null)
                {
                    for (int j = 0; j < providedComponents.Length; ++j)
                    {
                        if (dec.components[i].GetComputedType().IsAssignableFrom(providedComponents[j].GetType()))
                        {
                            if (match == -1)
                            {
                                match = j;
                            }
                            else
                            {
                                Dbg.Err($"Ambiguity in component match for {dec.DecName}; {dec.components[i].GetComputedType()} matches both {providedComponents[match].GetType()} and {providedComponents[j].GetType()}");
                            }
                        }
                    }
                }

                if (match != -1)
                {
                    components[i] = providedComponents[match];
                    used[match] = true;
                }
                else
                {
                    components[i] = Activator.CreateInstance(dec.components[i].GetComputedType());
                }
            }

            if (used != null)
            {
                for (int i = 0; i < used.Length; ++i)
                {
                    if (!used[i])
                    {
                        Dbg.Err($"Unused component {providedComponents[i].GetType()} provided for {dec.DecName}");
                    }
                }
            }

            return components;
        }

        public Entity Add(EntityDec dec, object[] providedComponents = null)
        {
            if (IsConstantProcess)
            {
                // non-fatal; we go ahead and do it, but the process's declaration is now a lie
                Dbg.Err($"Adding entity {dec} during a constant process; this violates the process's constant declaration");
            }

            var resultComponents = FillComponents(dec, providedComponents);

            if (!IsInSystem)
            {
                return AddNow(dec, resultComponents, ++stableIdCounter);
            }

            var entityDeferred = new EntityDeferred();
            entityDeferred.dec = dec;

            var tranche = new Tranche();
            tranche.entries = new List<Entity>();
            tranche.components = new Array[dec.components.Count];
            // we ignore the metadata because this should never be serialized

            // create a new set of components
            for (int i = 0; i < dec.components.Count; ++i)
            {
                // currently not worrying about minmaxing efficiency here
                tranche.components[i] = Array.CreateInstance(dec.components[i].GetComputedType(), 1);
                tranche.components[i].SetValue(resultComponents[i], 0);
            }

            // do this late because it's a struct
            entityDeferred.tranche = tranche;

            // Assign stableId once; phase-end AddNow reuses it so the deferred struct and its resolved tranche entry share the same stableId (and therefore the same sort key / hash).
            int deferredStableId = ++stableIdCounter;
            phaseEndActions.Add(() =>
            {
                var currentComponents = new object[dec.components.Count];
                // copy components back from the tranche
                for (int i = 0; i < dec.components.Count; ++i)
                {
                    currentComponents[i] = tranche.components[i].GetValue(0);
                }
                entityDeferred.replacement = AddNow(dec, currentComponents, deferredStableId);
            });
            var resultEntity = new Entity(entityDeferred, deferredStableId);
            currentEntityAdded.Add(resultEntity);
            return resultEntity;
        }

        private Entity AddNow(EntityDec dec, object[] components, int stableId)
        {
            if (tranches[dec.index].entries == null)
            {
                tranches[dec.index] = CreateNewTranche(dec);
            }

            var tranche = tranches[dec.index];
            var trancheId = tranche.entries.Count();

            for (int i = 0; i < dec.components.Count; ++i)
            {
                // we want to add this to the end, but it's a static array so (1) that's not "the end", and (2) we may need to realloc
                if (tranche.components[i].Length <= trancheId)
                {
                    // we need to realloc :(
                    // we might have a small array thanks to deserialization; if we do, pad it up to the base array size at least
                    var newArray = Array.CreateInstance(dec.components[i].GetComputedType(), Math.Max(trancheId * 2, BaseArraySize));
                    Array.Copy(tranche.components[i], newArray, trancheId);
                    tranche.components[i] = newArray;
                }

                tranche.components[i].SetValue(components[i], trancheId);
            }

            // now allocate the actual entity ID
            int id;
            if (entityFreeList.Count > 0)
            {
                id = entityFreeList[entityFreeList.Count - 1];
                entityFreeList.RemoveAt(entityFreeList.Count - 1);
                entityLookup[id] = new EntityLookup() { dec = dec, index = trancheId, gen = entityLookup[id].gen + 1 };
            }
            else
            {
                id = entityLookup.Count;
                entityLookup.Add(new EntityLookup() { dec = dec, index = trancheId, gen = 1 });
            }

            var entity = new Entity(id, entityLookup[id].gen, stableId);
            tranche.entries.Add(entity);

            return entity;
        }

        public void Remove(Entity entity)
        {
            if (entity == default)
            {
                Dbg.Err("Attempted to remove default entity");
                return;
            }

            if (IsConstantProcess)
            {
                // non-fatal; we go ahead and do it, but the process's declaration is now a lie
                Dbg.Err($"Removing entity {entity} during a constant process; this violates the process's constant declaration");
            }

            if (!IsInSystem)
            {
                RemoveNow(entity);
            }
            else
            {
                phaseEndActions.Add(() =>
                {
                    RemoveNow(entity);
                });
                currentEntityRemoved.Add(entity);
            }
        }

        private void RemoveNow(Entity entity)
        {
            (EntityLookup lookup, int id) = LookupFromEntity(entity);
            if (id == -1)
            {
                // double-deleting is permitted, deleting null or invalid isn't
                if (entity.GetStatus() != Entity.Status.Deleted)
                {
                    Dbg.Err($"Attempted to remove entity {entity} that doesn't exist");
                }
                return;
            }

            // send appropriate messages
            entity.OnRemove();

            // we want to keep each tranche contiguous

            var tranche = tranches[lookup.dec.index];
            if (lookup.index == tranche.entries.Count - 1)
            {
                // if we're removing from the end, we just remove it; easy!
                tranche.entries.RemoveAt(lookup.index);

                // also, the same for every list
                // we kinda just reset it in order to ensure we "garbage-collect" stuff
                for (int i = 0; i < tranche.components.Length; ++i)
                {
                    tranche.components[i].SetValue(lookup.dec.components[i].GetComputedType().CreateDefault(), lookup.index);
                }
            }
            else
            {
                // if we're removing from the center, we move the end item into the removed item's place
                // three cheers for O(1)
                int endEntry = tranche.entries.Count - 1;
                tranche.entries[lookup.index] = tranche.entries[endEntry];
                tranche.entries.RemoveAt(endEntry);

                // also, the same for every list
                for (int i = 0; i < tranche.components.Length; ++i)
                {
                    tranche.components[i].SetValue(tranche.components[i].GetValue(endEntry), lookup.index);
                    tranche.components[i].SetValue(lookup.dec.components[i].GetComputedType().CreateDefault(), endEntry);
                }

                // now patch up the entity lookup table for the item we just swapped in
                var replacedId = tranche.entries[lookup.index].id;
                entityLookup[replacedId] = new EntityLookup() { dec = lookup.dec, index = lookup.index, gen = entityLookup[replacedId].gen };
            }

            // wipe the entity we deleted from the lookup table
            // important that we bump the generation to ensure we never repeat generations!
            entityLookup[id] = new EntityLookup() { dec = null, index = -1, gen = entityLookup[id].gen + 1 };
            entityFreeList.Add(id);
        }

        private (EntityLookup lookup, int id) LookupFromEntity(Entity entity)
        {
            entity.Resolve();

            if (entity.id < 0 || entity.id >= entityLookup.Count)
            {
                return (new EntityLookup(), -1);
            }

            // compare generations; if this isn't us, it's not real
            var lookup = entityLookup[entity.id];
            if (lookup.gen != entity.gen)
            {
                return (new EntityLookup(), -1);
            }

            return (lookup, entity.id);
        }

        internal (EntityDec dec, Tranche tranche, int index) Get(Entity entity)
        {
            var (lookup, id) = LookupFromEntity(entity);
            if (id == -1)
            {
                return (null, default, -1);
            }

            return (lookup.dec, tranches[lookup.dec.index], lookup.index);
        }

        public T Singleton<T>()
        {
            if (!singletonLookup.ContainsKey(typeof(T)))
            {
                Dbg.Err($"Attempted to access singleton {typeof(T)} that doesn't exist");
                return default;
            }

            return (T)singletons[singletonLookup[typeof(T)]];
        }

        public T SingletonRO<T>()
        {
            return Singleton<T>();
        }

        public T SingletonRW<T>()
        {
            return Singleton<T>();
        }

        public void SingletonSet<T>(T newSingleton)
        {
            if (!singletonLookup.ContainsKey(typeof(T)))
            {
                Dbg.Err($"Attempted to set singleton {typeof(T)} that doesn't exist");
                return;
            }

            if (IsConstantProcess)
            {
                // singletons are recorded state, so swapping one out is the same kind of violation as an add or remove
                Dbg.Err($"Setting singleton {typeof(T)} during a constant process; this violates the process's constant declaration");
            }

            singletons[singletonLookup[typeof(T)]] = newSingleton;
        }

        private void CleanCurrentEntityDeferred()
        {
            currentEntityAdded.Clear();
            currentEntityRemoved.Clear();
        }
        public void Process(ProcessDec process)
        {
            if (Current.Value != this && Current.Value != null)
            {
                Dbg.Wrn("Started Environment.Process with a different Environment active; this is probably a mistake");
            }
            using var scope = new Scope(this);

            if (status != Status.Idle)
            {
                Dbg.Err($"Trying to run process while the world is in {status} state; should be {Status.Idle} state");

                // running it anyway would end the outer process when this one finishes, leaving the rest of the outer systems iterating tranches that Add and Remove are now editing in place
                return;
            }

            if (process == null)
            {
                Dbg.Err("Process is null!");
                return;
            }

            if (process.order == null)
            {
                Dbg.Err("Process Order is null!");
                return;
            }

            var statusProcess = process.constant ? Status.ProcessConstant : Status.ProcessMutating;
            var statusSystem = process.constant ? Status.SystemConstant : Status.SystemMutating;

            status = statusProcess;

            foreach (var system in process.order)
            {
                status = statusSystem;

                IDisposable prof = null;
                try
                {
                    prof = Config.ProfFactory(system.DecName);
                }
                catch (Exception e)
                {
                    Dbg.Ex(e);
                }

                system.process(tranches, singletons, CleanCurrentEntityDeferred);

                try
                {
                    prof?.Dispose();
                }
                catch (Exception e)
                {
                    Dbg.Ex(e);
                }

                status = statusProcess;

                if (phaseEndActions.Count != 0)
                {
                    var actions = new List<Action>(phaseEndActions);
                    phaseEndActions.Clear();

                    foreach (var action in actions)
                    {
                        action();
                    }

                    Assert.IsEmpty(phaseEndActions);
                }
            }

            status = Status.Idle;
        }

        public void Record(Dec.Recorder recorder)
        {
            // A process that has declared itself constant isn't touching anything we're about to record, so recording right through one is fine - that's the whole point of the declaration - but anything else risks catching component arrays partway through an update
            Assert.IsFalse(IsMutating, $"Attempting to record an environment during a non-constant process; status is {status}");
            Assert.AreEqual(0, phaseEndActions.Count);

            // so that our children can use cows
            using var scope = new Scope(this);

            recorder.Record(ref tranches, "tranches");
            recorder.Record(ref entityLookup, "entityLookup");
            recorder.Record(ref entityFreeList, "entityFreeList");
            recorder.Record(ref singletons, "singletons");
            recorder.Record(ref stableIdCounter, "stableIdCounter");

            if (recorder.Intent == Dec.Recorder.Purpose.Cloning)
            {
                // copy this over if we're cloning, but we don't worry about it otherwise
                recorder.Record(ref singletonLookup, "singletonLookup");
            }

            if (recorder.Intent == Dec.Recorder.Purpose.Serialization && recorder.Mode == Recorder.Direction.Read)
            {
                var entityDecs = indexToEntityDec;

                // gotta rebuild the tranches based on the expected types
                var oldTranches = tranches;
                tranches = new Tranche[entityDecs.Length];

                // remap every tranche that we can
                for (int i = 0; i < entityDecs.Length; ++i)
                {
                    var originalTranche = oldTranches.FirstOrDefault(t => t.entity == entityDecs[i]);

                    if (originalTranche.components == null || originalTranche.entries == null)
                    {
                        // uninitialized tranche, leave it uninitialized
                        continue;
                    }
                    else
                    {
                        // remap components within the tranche
                        var oldComponentTypes = originalTranche.componentTypes;
                        var oldComponents = originalTranche.components;

                        var newComponentTypes = entityDecs[i].components.Select(c => c.GetComputedType()).ToArray();
                        var newComponents = new Array[entityDecs[i].components.Count];

                        for (int j = 0; j < newComponentTypes.Length; ++j)
                        {
                            // find the old component type
                            int oldIndex = Array.IndexOf(oldComponentTypes, newComponentTypes[j]);
                            if (oldIndex == -1)
                            {
                                // this is a new component type, so we need to create a new array. duplicate our current component sizes I guess
                                newComponents[j] = Array.CreateInstance(newComponentTypes[j], oldComponents[0].Length);

                                // and now fill it with the component
                                for (int k = 0; k < originalTranche.entries.Count; ++k)
                                {
                                    newComponents[j].SetValue(Activator.CreateInstance(newComponentTypes[j]), k);
                                }
                            }
                            else
                            {
                                // this is an existing component type, so we can just copy it over
                                newComponents[j] = oldComponents[oldIndex];
                            }
                        }

                        originalTranche.components = newComponents;
                        originalTranche.componentTypes = newComponentTypes;
                    }

                    tranches[i] = originalTranche;
                }

                // update singletons
                object[] newSingletons = new object[singletonLookup.Count];
                foreach (var kvp in singletonLookup)
                {
                    // see if we can find it in our old singletons
                    int index = Array.FindIndex(singletons, s => s != null && kvp.Key.IsAssignableFrom(s.GetType()));
                    if (index != -1)
                    {
                        newSingletons[kvp.Value] = singletons[index];
                    }
                    else
                    {
                        // we don't have this singleton, so we need to create it
                        newSingletons[kvp.Value] = Activator.CreateInstance(kvp.Key);
                    }
                }
                singletons = newSingletons;

                // go through and GC the entity lookup table
                for (int i = 0; i < entityLookup.Count; ++i)
                {
                    if (entityLookup[i].dec == null && entityLookup[i].index != -1)
                    {
                        // deleted entry; add it to the freelist and clear properly
                        entityFreeList.Add(i);

                        // bump the gen so anything pointing at this ends up marked as deleted
                        entityLookup[i] = new EntityLookup() { dec = null, index = -1, gen = entityLookup[i].gen + 1 };
                    }
                }

                // make sure we don't have duplicates in the freelist!
                Assert.IsTrue(entityFreeList.Distinct().Count() == entityFreeList.Count);
            }
        }
    }
}
