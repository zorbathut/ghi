using Dec;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Ghi.Test
{
    public class StableComparerTest : Base
    {
        public StableComparerTest(EmitMode emitMode) : base(emitMode) { }

        [Dec.StaticReferences]
        public static class Decs
        {
            static Decs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static EntityDec EntityModel;
            public static ProcessDec CreateProcess;
        }

        private const int N = 50;

        public static class CreateEntitiesSystem
        {
            public static List<Entity> created;
            public static List<int> insideSortedHashes;

            public static void Execute()
            {
                var env = Ghi.Environment.Current.Value;
                created = new List<Entity>();
                for (int i = 0; i < N; ++i)
                {
                    created.Add(env.Add(Decs.EntityModel));
                }

                var rng = new System.Random(12345);
                var shuffled = created.OrderBy(_ => rng.Next()).ToList();
                shuffled.Sort(Entity.StableComparer);

                insideSortedHashes = shuffled.Select(e => e.GetHashCode()).ToList();
            }
        }

        private static void SetupBasicDecs()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""CreateEntitiesSystem"">
                        <type>CreateEntitiesSystem</type>
                    </SystemDec>

                    <ProcessDec decName=""CreateProcess"">
                        <order>
                            <li>CreateEntitiesSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();
            Ghi.Environment.Init();
        }

        // Creates entities during a System (deferred state), shuffles + sorts them inside, then grabs them again after the System resolves and verifies the same stable order emerges from a differently-shuffled input.
        [Test]
        public void SortInsideMatchesSortOutside()
        {
            SetupBasicDecs();

            var env = new Ghi.Environment();
            using var envActive = new Ghi.Environment.Scope(env);

            env.Process(Decs.CreateProcess);

            // Same set, now resolved, shuffled differently, sorted: should yield same stable-id sequence.
            var outside = CreateEntitiesSystem.created.ToList();
            var rng = new System.Random(67890);
            var outsideShuffled = outside.OrderBy(_ => rng.Next()).ToList();
            outsideShuffled.Sort(Entity.StableComparer);
            var outsideSortedHashes = outsideShuffled.Select(e => e.GetHashCode()).ToList();

            Assert.AreEqual(N, CreateEntitiesSystem.insideSortedHashes.Count);
            CollectionAssert.AreEqual(CreateEntitiesSystem.insideSortedHashes, outsideSortedHashes);

            // Also verify the captured (initially-deferred) references still equal the resolved tranche entries.
            var listed = env.List.OrderBy(e => e.GetHashCode()).ToArray();
            var capturedSorted = outside.OrderBy(e => e.GetHashCode()).ToArray();
            CollectionAssert.AreEqual(capturedSorted, listed);
        }

        // Every entity, whether immediate or deferred, gets a unique stableId.
        [Test]
        public void StableIdsUnique()
        {
            SetupBasicDecs();

            var env = new Ghi.Environment();
            using var envActive = new Ghi.Environment.Scope(env);

            var immediate = new List<Entity>();
            for (int i = 0; i < N; ++i) immediate.Add(env.Add(Decs.EntityModel));

            env.Process(Decs.CreateProcess);

            var all = immediate.Concat(CreateEntitiesSystem.created).ToList();
            var hashes = all.Select(e => e.GetHashCode()).ToHashSet();

            Assert.AreEqual(all.Count, hashes.Count);
            Assert.AreEqual(2 * N, all.Count);
        }

        // A captured deferred Entity and its resolved tranche entry must share the same stableId (otherwise Dictionary keys break across the transition).
        [Test]
        public void StableIdSurvivesResolution()
        {
            SetupBasicDecs();

            var env = new Ghi.Environment();
            using var envActive = new Ghi.Environment.Scope(env);

            env.Process(Decs.CreateProcess);

            var captured = CreateEntitiesSystem.created;
            var listed = env.List.ToList();
            Assert.AreEqual(captured.Count, listed.Count);

            var listedByHash = listed.ToDictionary(e => e.GetHashCode());
            foreach (var c in captured)
            {
                Assert.IsTrue(listedByHash.ContainsKey(c.GetHashCode()),
                    "Deferred entity's stableId missing from resolved tranche");
                Assert.AreEqual(c, listedByHash[c.GetHashCode()]);
            }
        }

        // Cloning and sort-by-stable produces the same key sequence as sorting the original.
        [Test]
        public void StableOrderPreservedAcrossClone()
        {
            SetupBasicDecs();

            var env = new Ghi.Environment();
            using var envActive = new Ghi.Environment.Scope(env);

            for (int i = 0; i < N; ++i) env.Add(Decs.EntityModel);

            var originalSorted = env.List.OrderBy(e => e, Entity.StableComparer).Select(e => e.GetHashCode()).ToList();

            var clone = Dec.Recorder.Clone(env);
            using (var cloneActive = new Ghi.Environment.Scope(clone))
            {
                var cloneSorted = clone.List.OrderBy(e => e, Entity.StableComparer).Select(e => e.GetHashCode()).ToList();
                CollectionAssert.AreEqual(originalSorted, cloneSorted);
            }
        }

        // Same via disk-style serialization.
        [Test]
        public void StableOrderPreservedAcrossSerialization()
        {
            SetupBasicDecs();

            List<int> originalSorted;
            string serialized;

            {
                var env = new Ghi.Environment();
                using var envActive = new Ghi.Environment.Scope(env);

                for (int i = 0; i < N; ++i) env.Add(Decs.EntityModel);

                originalSorted = env.List.OrderBy(e => e, Entity.StableComparer).Select(e => e.GetHashCode()).ToList();
                serialized = Dec.Recorder.Write(env);
            }

            Clean();
            SetupBasicDecs();

            {
                var env = Dec.Recorder.Read<Ghi.Environment>(serialized);
                using var envActive = new Ghi.Environment.Scope(env);

                var loadedSorted = env.List.OrderBy(e => e, Entity.StableComparer).Select(e => e.GetHashCode()).ToList();
                CollectionAssert.AreEqual(originalSorted, loadedSorted);
            }
        }

        // Two clones that undergo identical operations after divergence must end up with matching stable orders — the strongest determinism guarantee (and the motivating use case: multiplayer).
        [Test]
        public void StableOrderAcrossParallelClones()
        {
            SetupBasicDecs();

            var envA = new Ghi.Environment();
            using (var scope = new Ghi.Environment.Scope(envA))
            {
                for (int i = 0; i < N; ++i) envA.Add(Decs.EntityModel);
            }

            var envB = Dec.Recorder.Clone(envA);

            // Apply identical further ops to each.
            using (var scope = new Ghi.Environment.Scope(envA))
            {
                envA.Process(Decs.CreateProcess);
                for (int i = 0; i < N; ++i) envA.Add(Decs.EntityModel);
            }
            using (var scope = new Ghi.Environment.Scope(envB))
            {
                envB.Process(Decs.CreateProcess);
                for (int i = 0; i < N; ++i) envB.Add(Decs.EntityModel);
            }

            List<int> hashesA, hashesB;
            using (var scope = new Ghi.Environment.Scope(envA))
            {
                hashesA = envA.List.OrderBy(e => e, Entity.StableComparer).Select(e => e.GetHashCode()).ToList();
            }
            using (var scope = new Ghi.Environment.Scope(envB))
            {
                hashesB = envB.List.OrderBy(e => e, Entity.StableComparer).Select(e => e.GetHashCode()).ToList();
            }

            CollectionAssert.AreEqual(hashesA, hashesB);
        }

        // Back-compat: a save that uses the old "hashCode"/"prngState" tags must still load, with the old hashCodes becoming stableIds and new creations not colliding.
        [Test]
        public void LegacyHashCodeFormatLoads()
        {
            SetupBasicDecs();

            string serialized;
            List<int> originalSorted;

            {
                var env = new Ghi.Environment();
                using var envActive = new Ghi.Environment.Scope(env);

                for (int i = 0; i < N; ++i) env.Add(Decs.EntityModel);

                originalSorted = env.List.OrderBy(e => e, Entity.StableComparer).Select(e => e.GetHashCode()).ToList();
                serialized = Dec.Recorder.Write(env);
            }

            // Rewrite tags to simulate a pre-stableId save on disk.
            var legacy = serialized
                .Replace("<stableId>", "<hashCode>")
                .Replace("</stableId>", "</hashCode>")
                .Replace("<stableIdCounter>", "<prngState>")
                .Replace("</stableIdCounter>", "</prngState>");
            Assert.IsFalse(legacy.Contains("stableId"), "legacy-rewrite left stableId tags behind");

            Clean();
            SetupBasicDecs();

            {
                Ghi.Environment env = null;
                ExpectWarnings(() => env = Dec.Recorder.Read<Ghi.Environment>(legacy), wrn => wrn.Contains("Elements specified that don't exist on the object") && wrn.Contains("prngState"));
                using var envActive = new Ghi.Environment.Scope(env);

                // Loaded stableIds should match the pre-rewrite values (old hashCode aliased to stableId).
                var loadedSorted = env.List.OrderBy(e => e, Entity.StableComparer).Select(e => e.GetHashCode()).ToList();
                CollectionAssert.AreEqual(originalSorted, loadedSorted);
            }
        }
    }
}
