Unit Tester - Write and review unit tests.

**Framework:** xUnit (standard for .NET)

When writing tests:

1. **Test Structure**
   - File: `{ClassName}Tests.cs`
   - Method: `{Method}_{Scenario}_{ExpectedResult}`
   ```csharp
   public async Task GetBalance_ValidUser_ReturnsBalance()
   public async Task GetBalance_InvalidUser_ReturnsNull()
   ```

2. **Arrange-Act-Assert Pattern**
   ```csharp
   [Fact]
   public async Task ApproveRequest_ValidRequest_UpdatesBalance()
   {
       // Arrange
       var service = CreateService();
       var request = new MoneyRequest { Amount = 10, Status = RequestStatus.Pending };

       // Act
       await service.ApproveRequestAsync(request.Id, parentId: 1);

       // Assert
       Assert.Equal(RequestStatus.Approved, request.Status);
   }
   ```

3. **What to Test**
   - Service methods (business logic)
   - Validation logic
   - Edge cases (null, zero, negative amounts)
   - Error conditions

4. **What NOT to Unit Test**
   - Blazor components (use bUnit for those)
   - EF Core queries directly (integration tests)
   - Framework code

5. **Mocking**
   - Use Moq for dependencies
   - Mock DbContext with in-memory database for service tests
   - Keep mocks simple

After writing tests, run them with `/test` to verify.
