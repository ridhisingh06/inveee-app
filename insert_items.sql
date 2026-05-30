DO $$
DECLARE
    laptop_id INT;
    keyboard_id INT;
    monitor_id INT;
BEGIN
    -- Check if Laptop exists, otherwise insert
    IF NOT EXISTS (SELECT 1 FROM "Items" WHERE "Name" = 'Laptop') THEN
        INSERT INTO "Items" ("Name", "CategoryId", "Description", "IsActive", "CreatedAt")
        VALUES ('Laptop', 2, 'High performance laptop', true, NOW())
        RETURNING "Id" INTO laptop_id;
        
        INSERT INTO "InventoryStocks" ("ItemId", "TotalQuantity", "AvailableQuantity", "UpdatedAt")
        VALUES (laptop_id, 50, 50, NOW());
    END IF;

    -- Check if Keyboard exists, otherwise insert
    IF NOT EXISTS (SELECT 1 FROM "Items" WHERE "Name" = 'Keyboard') THEN
        INSERT INTO "Items" ("Name", "CategoryId", "Description", "IsActive", "CreatedAt")
        VALUES ('Keyboard', 2, 'Mechanical keyboard', true, NOW())
        RETURNING "Id" INTO keyboard_id;
        
        INSERT INTO "InventoryStocks" ("ItemId", "TotalQuantity", "AvailableQuantity", "UpdatedAt")
        VALUES (keyboard_id, 50, 50, NOW());
    END IF;

    -- Check if Monitor exists, otherwise insert
    IF NOT EXISTS (SELECT 1 FROM "Items" WHERE "Name" = 'Monitor') THEN
        INSERT INTO "Items" ("Name", "CategoryId", "Description", "IsActive", "CreatedAt")
        VALUES ('Monitor', 2, '4K UltraHD monitor', true, NOW())
        RETURNING "Id" INTO monitor_id;
        
        INSERT INTO "InventoryStocks" ("ItemId", "TotalQuantity", "AvailableQuantity", "UpdatedAt")
        VALUES (monitor_id, 50, 50, NOW());
    END IF;
END $$;
