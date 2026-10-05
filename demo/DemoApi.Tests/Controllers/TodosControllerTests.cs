using System.Net.Http;
using System.Threading.Tasks;
using DemoApi.Controllers;
using DemoApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DemoApi.Tests.Controllers
{
    public class TodosControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public TodosControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateTodo_WithValidTitle_Returns201()
        {
            var client = _factory.CreateClient();

            var todo = new Todo
            {
                Title = "Sample Todo"
            };

            var response = await client.PostAsJsonAsync("/api/todos", todo);

            response.EnsureSuccessStatusCode();
            var createdTodo = await response.Content.ReadFromJsonAsync<Todo>();

            Assert.NotNull(createdTodo);
            Assert.Equal("Sample Todo", createdTodo.Title);
            Assert.False(createdTodo.IsCompleted);
        }

        [Fact]
        public async Task CreateTodo_WithInvalidTitle_Returns400()
        {
            var client = _factory.CreateClient();

            var todo = new Todo
            {
                Title = "S"
            };

            var response = await client.PostAsJsonAsync("/api/todos", todo);

            response.EnsureSuccessStatusCode();
            var createdTodo = await response.Content.ReadFromJsonAsync<Todo>();

            Assert.Null(createdTodo);
        }

        [Fact]
        public async Task GetTodo_WithValidId_ReturnsTodo()
        {
            var client = _factory.CreateClient();

            var todo = new Todo
            {
                Title = "Sample Todo"
            };

            var response = await client.PostAsJsonAsync("/api/todos", todo);

            response.EnsureSuccessStatusCode();
            var createdTodo = await response.Content.ReadFromJsonAsync<Todo>();

            var getResponse = await client.GetAsync($"/api/todos/{createdTodo.Id}");

            getResponse.EnsureSuccessStatusCode();
            var retrievedTodo = await getResponse.Content.ReadFromJsonAsync<Todo>();

            Assert.NotNull(retrievedTodo);
            Assert.Equal(createdTodo.Id, retrievedTodo.Id);
            Assert.Equal(createdTodo.Title, retrievedTodo.Title);
            Assert.False(retrievedTodo.IsCompleted);
        }

        [Fact]
        public async Task GetTodo_WithNonExistentId_Returns404()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/todos/1");

            response.EnsureSuccessStatusCode();
            var retrievedTodo = await response.Content.ReadFromJsonAsync<Todo>();

            Assert.Null(retrievedTodo);
        }
    }
}
