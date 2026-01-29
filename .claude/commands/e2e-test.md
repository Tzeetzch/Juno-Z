E2E Tester - Integration and end-to-end testing.

**Focus:** Test complete user flows, not individual units.

1. **User Flow Tests**
   - Child logs in with picture password
   - Child views balance and history
   - Child submits withdrawal request
   - Parent receives and approves request
   - Child balance updates

2. **Integration Tests**
   - Service + real database (SQLite in-memory)
   - Full request/response cycles
   - Background service execution

3. **Component Tests (bUnit)**
   ```csharp
   [Fact]
   public void BalanceCard_DisplaysFormattedAmount()
   {
       // Arrange
       using var ctx = new TestContext();

       // Act
       var cut = ctx.RenderComponent<BalanceCard>(parameters =>
           parameters.Add(p => p.Balance, 25.50m));

       // Assert
       cut.Find(".balance-amount").MarkupMatches("<span class=\"balance-amount\">€25.50</span>");
   }
   ```

4. **Test Scenarios to Cover**
   - Happy paths (everything works)
   - Auth failures (wrong picture password)
   - Validation errors (negative amounts)
   - Concurrent access (two parents approving same request)

5. **Test Data**
   - Use seeded test data
   - Reset database between tests
   - Don't depend on test order

When reviewing, verify:
- Critical user journeys have tests
- Edge cases are covered
- Tests are readable and maintainable
