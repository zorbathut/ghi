namespace Ghi
{
    using System;
    using System.Linq;

    public struct Entity : Dec.IRecordable
    {
        internal int id;
        internal long gen; // 32-bit gives us 2.1 years, and someone is gonna want to run a server longer than that

        private Environment.EntityDeferred deferred;

        public Entity()
        {
            this.id = 0;
            this.gen = 0;
            this.deferred = null;
        }
        internal Entity(Environment.EntityDeferred deferred)
        {
            this.id = 0;
            this.gen = 0;
            this.deferred = deferred;
        }
        internal Entity(int id, long gen)
        {
            this.id = id;
            this.gen = gen;
            this.deferred = null;
        }

        private void Resolve()
        {
            if (deferred != null && deferred.entity.gen != 0)
            {
                id = deferred.entity.id;
                gen = deferred.entity.gen;
                deferred = null;
            }
        }

        public T Component<T>()
        {
            var env = Environment.Current.Value;
            if (env == null)
            {
                Dbg.Err($"Attempted to get entity while env is unavailable");
                return default;
            }

            Resolve();

            (var dec, var tranche, var index) = env.Get(this);
            if (dec == null)
            {
                Dbg.Err($"Attempted to get dead entity {this}");
                return default;
            }

            // I don't like that this boxes
            var result = dec.GetComponentFrom(typeof(T), tranche, index);
            if (result == null)
            {
                return default;
            }

            return (T)result;
        }

        public T ComponentRO<T>()
        {
            return Component<T>();
        }

        public T TryComponent<T>()
        {
            var env = Environment.Current.Value;
            if (env == null)
            {
                // yes this is still an error
                Dbg.Err($"Attempted to get entity while env is unavailable");
                return default;
            }

            Resolve();

            (var dec, var tranche, var index) = env.Get(this);
            if (dec == null)
            {
                return default;
            }

            // I don't like that this boxes
            var result = dec.GetComponentFrom(typeof(T), tranche, index);
            if (result == null)
            {
                return default;
            }

            return (T)result;
        }

        public T TryComponentRO<T>()
        {
            return TryComponent<T>();
        }

        public override string ToString()
        {
            if (Environment.EntityToString != null)
            {
                return Environment.EntityToString(this);
            }
            else
            {
                return "[Entity]";
            }
        }

        public void Record(Dec.Recorder recorder)
        {
            Resolve();
            Assert.IsTrue(deferred == null);

            recorder.Record(ref id, "id");
            recorder.Record(ref gen, "gen");
        }
    }
}
