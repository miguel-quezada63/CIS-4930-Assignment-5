using Api.TodoListApp.Services;
using Lib.TodoListApp.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;

namespace Api.TodoListApp.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Mvc.Route("[controller]")]
    public class TodoItemsController : ControllerBase
    {
        private FirebaseService _databaseService;
        private static List<TodoItem> Items = new List<TodoItem>();
        public async void InitializeController()
        {
            _databaseService = new FirebaseService();
        }

        public TodoItemsController() => InitializeController();

        [Microsoft.AspNetCore.Mvc.HttpGet]
        [Microsoft.AspNetCore.Mvc.Route("all")]
        public async Task<IList<TodoItem>> GetAll() => await _databaseService.GetAllItems();

        [Microsoft.AspNetCore.Mvc.HttpGet]
        [Microsoft.AspNetCore.Mvc.Route("completed")]
        public async Task<List<TodoItem>> GetCompleted() => await _databaseService.GetTasksByCompletion(true);

        [Microsoft.AspNetCore.Mvc.HttpGet]
        [Microsoft.AspNetCore.Mvc.Route("outstanding")]
        public async Task<List<TodoItem>> GetOutstanding() => await _databaseService.GetTasksByCompletion(false);

        [Microsoft.AspNetCore.Mvc.HttpGet]
        [Microsoft.AspNetCore.Mvc.Route("appointments")]
        public async Task<List<TodoItem>> GetAppointments() => await _databaseService.GetAppointments();

        [Microsoft.AspNetCore.Mvc.HttpGet]
        [Microsoft.AspNetCore.Mvc.Route("tasks")]
        public async Task<List<TodoItem>> GetTasks() => await _databaseService.GetTasks();

        [Microsoft.AspNetCore.Mvc.HttpGet]
        [Microsoft.AspNetCore.Mvc.Route("search")]
        public async Task<List<TodoItem>> Search([FromUri] string query) => (await _databaseService.GetAllItems()).FindAll(item => item.Contains(query));

        [Microsoft.AspNetCore.Mvc.HttpDelete]
        [Microsoft.AspNetCore.Mvc.Route("delete/{id}")]
        public async Task<List<TodoItem>> DeleteTodo([FromUri] string id)
        {
            await _databaseService.RemoveById(id);
            return await _databaseService.GetAllItems();
        }

        [Microsoft.AspNetCore.Mvc.HttpPost]
        [Microsoft.AspNetCore.Mvc.Route("create")]
        public async Task<List<TodoItem>> CreateTask([Microsoft.AspNetCore.Mvc.FromBody] TodoItem t)
        {
            await _databaseService.CreateItem(t);
            return await _databaseService.GetAllItems();
        }

        [Microsoft.AspNetCore.Mvc.HttpPut]
        [Microsoft.AspNetCore.Mvc.Route("edit/{id}")]
        public async Task<List<TodoItem>> EditTodoItem([FromUri] string id, [System.Web.Http.FromBody] TodoItem newItem)
        {
            await _databaseService.UpdateItem(newItem, id);
            return await _databaseService.GetAllItems();
        }
    }
}
