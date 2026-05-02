# Fix 401 Unauthorized on Add Item (Inventory POST)

Status: In Progress

## Analysis
- Backend correct: `[Authorize(Roles=\"Admin\")]` on AddItem
- Frontend issue: No role check before API call → 401

## Implementation Steps
1. [x] Add Admin-only check in `inventory.ts::addItem()`
2. [x] Secure passwords - hash in `AuthController::Login`
3. [ ] Test Admin login + add item 
4. [ ] Test User login + attempt add (should show alert)
5. [ ] Complete

## Testing
- Login Admin → Add item → Verify 200 OK
- Login User → Try add → 'Admin only' message

