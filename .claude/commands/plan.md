Planner - Break features into small work cycles.

**Purpose:** After /spec approval, split work into small deliverable chunks. No big bangs.

**Rules:**
- Each cycle should be completable in one session
- Each cycle produces something testable/visible
- User approves after each cycle before continuing

**Process:**

1. **Take the approved spec**

2. **Break into cycles**
   Each cycle should:
   - Be small (1-3 components or 1 service)
   - Have a clear "done" state
   - Be independently testable
   - Build on previous cycles

3. **Present the plan**
   ```
   ## Work Cycles for: [Feature Name]

   ### Cycle 1: [Name]
   - Build: ...
   - Deliverable: User can see X / System does Y
   - Verify: How to test it works

   ### Cycle 2: [Name]
   - Build: ...
   - Depends on: Cycle 1
   - Deliverable: ...

   (etc.)
   ```

4. **Get approval on the plan**

5. **Execute ONE cycle at a time**
   - Build cycle 1
   - Present result
   - Wait for approval
   - Then cycle 2
   - Never jump ahead

**Example breakdown:**
"Add withdrawal request feature" becomes:
- Cycle 1: MoneyRequest entity + migration (backend only)
- Cycle 2: RequestService with create/approve methods
- Cycle 3: Child request form UI
- Cycle 4: Parent pending requests list
- Cycle 5: Approval flow + balance update
- Cycle 6: Tests

Each cycle: build → show → approve → next.
