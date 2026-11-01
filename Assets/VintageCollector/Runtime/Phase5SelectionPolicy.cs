namespace VintageCollector.Runtime {
 // Shared authority predicate; callers supply live owner state, never passive DTO permission.
 public static class Phase5SelectionPolicy {
  public static bool CanSelectOpening(string stage,bool paused,bool inspecting,bool pending,bool collectionBlocked,bool terminal,bool closed,bool error) {
   return (stage=="ready" || stage=="results") && !paused && !inspecting && !pending && !collectionBlocked && !terminal && !closed && !error;
  }
 }
}
