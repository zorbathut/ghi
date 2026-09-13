namespace Ghi;

// Lifecycle hooks.
//
// Every hook receives a resolved, valid entity whose components are all readable, with this environment as Environment.Current. Hooks fire at latest when the add or remove actually applies, which may be as late as phase end for a call made from inside a system.
//
// Hooks never fire from Record - serialization, deserialization, cloning, and checksumming all leave them silent, including the load-time fill of a component that the save predates. Having-fired is conceptually part of the recorded state, and migrating hook-derived state into an old save is the host's problem. An entity that arrived by load still gets its remove hooks when it goes.
//
// Internal order is currently undefined. An exception thrown by a hook is reported and the remaining hooks still run.
//
// A hook may Add or Remove entities. Removing the entity being added does undefined things with its pending add hooks, but its remove hooks all fire as usual, so a component whose OnAdd never ran may still see OnRemove.
//
// Dispatch currently goes by the ComponentDec's declared type: a subclass instance that adds a hook interface the declared type lacks is never called. Only reference types may implement these; a struct component would receive a boxed copy and lose every write it made.

// Fired on a component when its entity is added.
public interface IOnAdd
{
    public void OnAdd(Entity entity);
}

// Fired on a component when its entity is removed.
public interface IOnRemove
{
    public void OnRemove(Entity entity);
}
