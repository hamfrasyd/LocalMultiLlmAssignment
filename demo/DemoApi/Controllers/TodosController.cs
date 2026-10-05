using System.Threading.Tasks;
using DemoApi.Services;
using DemoApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace DemoApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TodosController : ControllerBase
    {
        private readonly ITodoService _todoService;

        public TodosController(ITodoService todoService)
        {
            _todoService = todoService;
        }

        [HttpPost("api/todos")]
        public async Task<IActionResult> CreateTodo([FromBody] Todo todo)
        {
            if (string.IsNullOrEmpty(todo.Title))
            {
                return BadRequest("Title cannot be empty");
            }

            var createdTodo = await _todoService.CreateTodoAsync(todo);
            return CreatedAtAction(nameof(GetTodo), new { id = createdTodo.Id }, createdTodo);
        }

        [HttpGet("api/todos/{id}")]
        public async Task<IActionResult> GetTodo(int id)
        {
            var todo = await _todoService.GetTodoAsync(id);

            if (todo == null)
            {
                return NotFound();
            }

            return Ok(todo);
        }
    }
}