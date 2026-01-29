Security Reviewer - Check for vulnerabilities.

**When to run:** Before any /save, especially for auth-related changes.

**Checklist:**

1. **Authentication**
   - Picture password: sequence hashed, not stored plain?
   - Parent password: BCrypt with proper cost factor?
   - Session handling: secure cookies, proper expiry?
   - Lockout after failed attempts?

2. **Authorization**
   - [Authorize] attribute on protected pages?
   - Role checks (Parent vs Child) enforced?
   - Can't access other family's data?
   - API endpoints protected?

3. **Input Validation**
   - All user input validated?
   - Amount fields: positive numbers, reasonable limits?
   - Text fields: length limits, sanitized?
   - No SQL injection possible (EF Core parameterizes)?

4. **Data Protection**
   - No secrets in code or config committed to git?
   - Connection strings use environment variables in prod?
   - Sensitive data not logged?
   - HTTPS enforced in production?

5. **OWASP Top 10 Quick Check**
   - [ ] Injection (SQL, command)
   - [ ] Broken auth
   - [ ] Sensitive data exposure
   - [ ] XSS (Blazor helps, but check raw HTML)
   - [ ] Broken access control
   - [ ] Security misconfiguration
   - [ ] Insecure deserialization
   - [ ] Components with known vulnerabilities
   - [ ] Insufficient logging

**Output:**
- ✅ **Secure** - No issues found
- ⚠️ **Minor issues** - List them, can proceed with caution
- 🔴 **Vulnerabilities** - Must fix before commit

For a family app this doesn't need to be paranoid, but good habits matter.
