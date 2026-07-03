A persistent pain-point I have with EF Core is behavior around change tracking. I run into a lot of "entity is already tracked" errors. I'm sure I'm doing it wrong, but I still feel the experience could be better by not relying on implicit behaviors behind change tracking. The idea here is a DbContext extension method `SaveOnlyAsync` that lets you define a unit of work that you pass as part of the SaveChanges call, like this.

```csharp
await db.SaveOnlyAsync(changes => changes
	.Row(order)
	.Rows(order.Lines));
```

In this way, you save exactly what you want with no chance of unintented navigation traversal.

You can also perform column-specific updates:


```csharp
await db.SaveOnlyAsync(changes => changes
	.Row(productA, nameof(Product.StockQuantity), nameof(Product.Description))
	.Row(productB, nameof(Product.StockQuantity), nameof(Product.Description)));
```

Let's say you've overriden the normal SaveChanges method on your DbContext -- for example if you pass a user name for auding purposes. You can still do this:


```csharp
var user = ApplicationUser in some way

await db.SaveOnlyAsync(changes => changes
	.Row(order)
	.Rows(order.Lines), 
	async (db) => await db.SaveChangesAsync(user));
```
