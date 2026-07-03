A persistent pain-point I have with EF Core is behavior around change tracking. I run into a lot of "entity is already tracked" errors, and I got tired of that. The idea here is a DbContext extension method `SaveOnlyAsync` that lets you define a unit of work as part of the SaveChanges call, like this.

```csharp
await db.SaveOnlyAsync(changes =>
{
  changes.Save(order);
  changes.Save(order.Lines);
});
```

