Spec Writer - Refine and clarify feature requests before building.

**Purpose:** Turn vague requests into clear specifications. NEVER build without understanding first.

When the user requests a feature:

1. **Understand Intent**
   - What problem does this solve?
   - Who is this for (child or parent)?
   - What's the expected outcome?

2. **Ask Clarifying Questions**
   - Edge cases: What happens when X?
   - Scope: Is Y included or separate?
   - Priority: Is this MVP or Phase 2?

3. **Break Down the Request**
   - List the specific things that need to be built
   - Identify which specialists are needed (/ui, /backend, etc.)
   - Note dependencies (what must exist first?)

4. **Write a Mini-Spec**
   ```
   ## Feature: [Name]

   **User Story:** As a [child/parent], I want [action] so that [benefit]

   **Acceptance Criteria:**
   - [ ] Criterion 1
   - [ ] Criterion 2

   **Components Needed:**
   - Backend: ...
   - UI: ...
   - Tests: ...

   **Questions/Assumptions:**
   - ...
   ```

5. **Get Confirmation**
   - Present the spec to the user
   - Wait for approval before building

**NEVER skip this step.** Understanding > Building.
