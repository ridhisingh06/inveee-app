INSERT INTO "InventoryStocks" ("ItemId", "TotalQuantity", "AvailableQuantity", "UpdatedAt")
SELECT i."Id", 100, 100, NOW()
FROM "Items" i
WHERE i."Name" = 'Test Pen'
  AND NOT EXISTS (
    SELECT 1 FROM "InventoryStocks" s WHERE s."ItemId" = i."Id"
  );

SELECT i."Id", i."Name", s."AvailableQuantity"
FROM "Items" i
JOIN "InventoryStocks" s ON s."ItemId" = i."Id";
