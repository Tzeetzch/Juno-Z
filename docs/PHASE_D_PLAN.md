# Phase D: Child Features - Work Cycles

**Goal:** Build the child user interface for viewing balance, history, and requesting transactions.

---

## Cycle 1: Child Dashboard Layout + Balance Display ✅ READY

**Build:**
- Create `Components/Pages/Child/Dashboard.razor`
- Add route `/child` with `[Authorize(Roles = "Child")]`
- Display current balance (large, visual, kid-friendly)
- Use neumorphic card styling from existing CSS

**Deliverable:**
Child can login and see their balance prominently displayed

**Verify:**
1. Login as Junior (🐱🐶⭐🌙)
2. See balance (€10) displayed large and clear
3. Page is protected (redirect to login if not authenticated as Child)

---

## Cycle 2: Transaction History Component

**Build:**
- Create `Components/Shared/TransactionList.razor` component
- Add to child dashboard
- Display transactions in reverse chronological order (newest first)
- Show: amount (+ or -), description, date
- Kid-friendly styling (large text, visual indicators for +/-)

**Depends on:** Cycle 1

**Deliverable:**
Child can see their transaction history below the balance

**Verify:**
1. Login as Junior
2. See transaction history (should show initial €10 deposit from seed data)
3. Visual distinction between deposits (+) and withdrawals (-)

---

## Cycle 3: Request Withdrawal Form

**Build:**
- Create `Components/Pages/Child/RequestWithdrawal.razor`
- Add route `/child/request-withdrawal`
- Form fields: Amount (number), Reason (text, e.g., "I want €5 for candy")
- Submit creates MoneyRequest (status: Pending, type: Withdrawal)
- Add navigation button from dashboard
- Success message after submission

**Depends on:** Cycle 2

**Deliverable:**
Child can request to withdraw money with a reason

**Verify:**
1. Login as Junior
2. Click "Request Money" button
3. Fill amount (€5) and reason ("For candy")
4. Submit and see success message
5. Check database: MoneyRequest exists with Pending status

---

## Cycle 4: Request Deposit Form

**Build:**
- Create `Components/Pages/Child/RequestDeposit.razor`
- Add route `/child/request-deposit`
- Form fields: Amount (number), Reason (text, e.g., "Got €10 from grandma")
- Submit creates MoneyRequest (status: Pending, type: Deposit)
- Add navigation button from dashboard
- Success message after submission

**Depends on:** Cycle 3

**Deliverable:**
Child can request to deposit money they received

**Verify:**
1. Login as Junior
2. Click "Add Money" button
3. Fill amount (€10) and reason ("From grandma")
4. Submit and see success message
5. Check database: MoneyRequest exists with Pending status

---

## Cycle 5: Visual Feedback & Polish

**Build:**
- Add pending requests indicator to dashboard ("1 request waiting for Mom/Dad")
- Add icons (piggy bank, coins, etc.) to make UI more kid-friendly
- Ensure large touch targets (buttons min 60px height)
- Responsive design check (works on tablet)
- Loading states for form submissions

**Depends on:** Cycle 4

**Deliverable:**
Polished, kid-friendly child interface with visual feedback

**Verify:**
1. Submit a request, see loading indicator
2. Return to dashboard, see pending request count
3. Test on different screen sizes (responsive)
4. All buttons are easy to tap

---

## Progress Tracking

- [x] Cycle 1: Child Dashboard + Balance
- [ ] Cycle 2: Transaction History
- [ ] Cycle 3: Request Withdrawal
- [ ] Cycle 4: Request Deposit
- [ ] Cycle 5: Polish

**Current Status:** In progress - Cycle 2

**Note:** Parent approval features (Phase E) come after Phase D is complete.

---

## Future Enhancements (Nice to Have)

### Transaction History Pagination
**Why:** Currently shows last 50 transactions. After years of use, child may want to see older history.

**Implementation:**
- Add "Load More" button at bottom of transaction list
- Click loads next 50 transactions
- Or implement infinite scroll (load more as user scrolls)
- Track current page/offset in component state

**Technical:**
```csharp
// Service method with pagination support
Task<List<Transaction>> GetTransactionsAsync(int userId, int page = 1, int pageSize = 20)
{
    return await _db.Transactions
        .Where(t => t.UserId == userId && t.IsApproved)
        .OrderByDescending(t => t.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
}
```

**Priority:** Low - only needed after child has 50+ transactions (6+ months of use)
