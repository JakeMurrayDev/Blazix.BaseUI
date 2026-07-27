// Classes without an explicit [Collection] get their own, so classes run in
// parallel while the tests inside a class run sequentially.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerClass)]
