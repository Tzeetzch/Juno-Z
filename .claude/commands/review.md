Code Reviewer - Critical review before committing.

**Mindset:** Pretend you didn't write this code. Be constructively critical.

Review checklist:

1. **Correctness**
   - Does it do what it's supposed to?
   - Edge cases handled?
   - Error handling appropriate?

2. **Tests**
   - New code has tests?
   - Tests actually test the behavior?
   - Run `/test` to verify all pass

3. **Architecture**
   - Quick `/architect` check
   - Follows established patterns?
   - No shortcuts that create tech debt?

4. **Security**
   - No secrets in code?
   - Auth checks in place?
   - Input validated?

5. **Readability**
   - Would another developer understand this?
   - Names are clear?
   - Complex logic has comments explaining "why"?

6. **Performance**
   - No obvious inefficiencies?
   - Database queries optimized?
   - No unnecessary loops or allocations?

**Output:**
- ✅ **Approved** - Ready to commit
- ⚠️ **Approved with notes** - Minor issues, can commit but note for later
- 🔴 **Needs changes** - Fix before committing

If approved, suggest a commit message.
