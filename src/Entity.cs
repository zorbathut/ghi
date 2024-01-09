
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

            // this data structure gives me a headache
            this.deferred.tranche.entries.Add(this);
        }
        internal Entity(int id, long gen)
        {
            this.id = id;
            this.gen = gen;
            this.deferred = null;
        }

        private void Resolve()
        {
            if (deferred != null && deferred.replacement.gen != 0)
            {
                id = deferred.replacement.id;
                gen = deferred.replacement.gen;
                deferred = null;
            }
        }

        public bool IsValid()
        {
            var env = Environment.Current.Value;
            if (env == null)
            {
                Dbg.Err($"Attempted to get entity while env is unavailable");
                return default;
            }

            Resolve();

            (var dec, var tranche, var index) = deferred?.Get() ?? env.Get(this);
            return dec != null;
        }

        public bool HasComponent<T>()
        {
            var env = Environment.Current.Value;
            if (env == null)
            {
                Dbg.Err($"Attempted to get entity while env is unavailable");
                return false;
            }

            Resolve();

            (var dec, var tranche, var index) = deferred?.Get() ?? env.Get(this);
            if (dec == null)
            {
                return false;
            }

            return dec.HasComponent(typeof(T));
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

            (var dec, var tranche, var index) = deferred?.Get() ?? env.Get(this);
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

            (var dec, var tranche, var index) = deferred?.Get() ?? env.Get(this);
            if (dec == null)
            {
                return default;
            }

            // I don't like that this boxes
            var result = dec.TryGetComponentFrom(typeof(T), tranche, index);
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

    public struct EntityComponent<T> : Dec.IRecordable
    {
        private Entity entity;

        public EntityComponent()
        {
            entity = new Entity();
        }

        internal EntityComponent(Entity entity)
        {
            this.entity = entity;
        }

        public static EntityComponent<T> From(Entity entity)
        {
            return new EntityComponent<T>(entity);
        }

        public bool IsValid()
        {
            // this does IsValid() also
            return entity.HasComponent<T>();
        }

        public T Get()
        {
            return entity.Component<T>();
        }

        public T GetRO()
        {
            return Get();
        }

        public T TryGet()
        {
            return entity.TryComponent<T>();
        }

        public T TryGetRO()
        {
            return TryGet();
        }

        public override string ToString()
        {
            return entity.ToString();
        }

        public void Record(Dec.Recorder recorder)
        {
            recorder.RecordAsThis(ref entity);
        }
    }
}
