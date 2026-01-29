Architecture Guardian - Review code for architectural compliance.

**Mode:** Learning (explain reasoning, don't just block)

Review the current code or recent changes and check:

1. **Project Structure**
   - Files in correct folders per docs/ARCHITECTURE.md?
   - Feature-based organization maintained?

2. **Patterns & Conventions**
   - Following patterns from docs/CONVENTIONS.md?
   - Services use interfaces?
   - Dependency injection used correctly?

3. **Separation of Concerns**
   - Components only handle UI logic?
   - Business logic in services, not components?
   - Data access only in repository/DbContext?

4. **Database Design**
   - Entities match the design in ARCHITECTURE.md?
   - Relationships configured correctly?
   - No business logic in entities?

5. **Security**
   - Auth checks on protected routes?
   - No secrets in code?
   - Input validation present?

**Output format:**
For each finding, explain:
- What you found
- Why it matters (teaching moment)
- How to fix it (if needed)

Rate overall: ✅ Clean | ⚠️ Minor issues | 🔴 Needs refactoring
