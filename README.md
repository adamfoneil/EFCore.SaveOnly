A persistent pain-point I have with EF Core is behavior around change tracking. I run into a lot of "entity is already tracked" errors. I'm sure I'm doing it wrong, but I still feel the experience could be better by not relying on implicit behaviors behind change tracking. The idea here is a DbContext extension method `SaveOnlyAsync` that lets you define a unit of work that you pass as part of the SaveChanges call, like this.

```csharp
await db.SaveOnlyAsync(changes =>
{
  changes.Row(order);
  changes.Rows(order.Lines);
});
```

In this way, you save exactly what you want
