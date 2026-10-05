using System.Collections.Generic;
using System.Linq;
using DemoApi.Models;

namespace DemoApi.Services
{
    public interface ITodoService
    {
        Todo CreateTodo(string title);
        Todo GetTodo(int id);
    }

    public class TodoService : ITodoService
    {
        private readonly List<Todo> _todos = new List<Todo>();

        public Todo CreateTodo(string title)
        {
            if (string.IsNullOrEmpty(title) || title.Length < 3 || title.Length > 50)
            {
                throw new ArgumentException("Title must be between 3 and 50 characters.");
            }

            var todo = new Todo
            {
                Id = _todos.Count + 1,
                Title = title,
                IsCompleted = false
            };

            _todos.Add(todo);

            return todo;
        }

        public Todo GetTodo(int id)
        {
            return _todos.FirstOrDefault(t => t.Id == id);
        }
    }
}