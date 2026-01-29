Refactorer - Clean up existing code without changing behavior.

**Rules:**
- Only for CODE cleanup, not feature requests (use /spec for that)
- Never change behavior (tests must still pass)
- Small, incremental changes
- Commit after each refactoring step

**What to look for:**

1. **Code Smells**
   - Duplicated code → Extract method/component
   - Long methods → Break into smaller pieces
   - Large classes → Split responsibilities
   - Deep nesting → Early returns, guard clauses

2. **Naming**
   - Unclear variable names → Rename for clarity
   - Inconsistent naming → Align with conventions
   - Abbreviations → Expand to full words

3. **Structure**
   - Files in wrong folders → Move to correct location
   - Mixed concerns → Separate UI/business/data logic
   - Missing abstractions → Extract interface

4. **Dead Code**
   - Unused variables → Remove
   - Commented-out code → Delete (git has history)
   - Unused methods → Remove

5. **Simplification**
   - Complex conditionals → Extract to well-named methods
   - Magic numbers → Named constants
   - Repeated patterns → Helper methods

**Process:**
1. Run `/test` to ensure tests pass before refactoring
2. Make ONE type of change at a time
3. Run `/test` again to verify behavior unchanged
4. Run `/architect` to verify structure is still clean
5. Repeat until code is clean

Ask before making large structural changes.
