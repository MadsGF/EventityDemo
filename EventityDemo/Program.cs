using Eventity;
using Eventity.EventStorage.Providers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventityDemo
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            /*
             * Select how data should be stored using a IStorageProvider
             */

            /*
             * Remember to put the file in a location that is accessible by the user of this app - both for reading and writing
             */

            IStorageProvider provider = new JsonFileStorageProvider(
                "eventity_playground.json"
            );

            /* Use this instead if you want to store data in a sql server
             Use Docker:
             docker run -d -p 1433:1433 -e sa_password=1234!asd -e ACCEPT_EULA=Y microsoft/mssql-server-windows-developer*/
            
            //var provider = new MSSqlStorageProvider (
            //    "your_connectionstring_here"
            //);

            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''


            // Only needs to be called once, but is idempotent
            // For JsonFiles, the path is validated and a file is created. For Sql server, the DB specified as InitialCatalog (from the connectionstring) is created and tables are created.
            provider.Initialize();

            var store = new EventityStore(provider);

            var todo = await store.GetEntity<TodoList>("EventityTodo");
            if (todo == null)
            {
                store.StageEvent("EventityTodo", new CreateTodoListEvent("EventityTodo", "Stuff to do for Eventity"));
                store.StageEvent("EventityTodo", new CreateTodoEvent("Check if anyone uses this package"));
                store.StageEvent("EventityTodo", new CreateTodoEvent("Update readme with better code examples"));
                store.StageEvent("EventityTodo", new CreateTodoEvent("Set up web site / github repo with docs"));
                store.StageEvent("EventityTodo", new CreateTodoEvent("Add snapshot capabilites"));
                store.StageEvent("EventityTodo", new CreateTodoEvent("Add postgres StorageProvider"));
                store.StageEvent("EventityTodo", new CreateTodoEvent("Add Tag querying without Entity- and Event workload"));
                await store.SaveChanges();

                // This is a new entity
                // Note that it is assigned a tag "Private"
                // This allows for querying entities
                store.StageEvent("Groceries", new CreateTodoListEvent("Groceries", "Groceries from the supermarket"), Tags.Set("Private"));
                store.StageEvent("Groceries", new CreateTodoEvent("Milk"));
                store.StageEvent("Groceries", new CreateTodoEvent("Diapers"));
                await store.SaveChanges();

                todo = await store.GetEntity<TodoList>("EventityTodo");
            }

            Console.WriteLine(todo.Title);
            foreach (var item in todo.Items)
                Console.WriteLine($" - {item}");

            var todoevents = await store.GetEvents<TodoList>("EventityTodo");
            Console.WriteLine($"{System.Environment.NewLine}{todoevents.Count} ...events stored{System.Environment.NewLine}");

            var privateTaggedLists = await store.GetByTags<TodoList>(new EntityTag[] { "Private" });
            Console.WriteLine("Private todo lists");
            foreach (var list in privateTaggedLists)
                Console.WriteLine($" - {list.Title}");

            Console.ReadLine();
        }
    }


    // This is the object wrapping a series of events - it must implement a Id property of type string.
    // MUST have a parameterless constructor!!!
    public class TodoList : IEntity
    {
        public TodoList()
        {

        }
        public string Id { get; set; }
        public string Title { get; set; }
        public List<string> Items { get; set; }
    }

    // Events that mutate the state of an entity.
    // MUST be serializable (using Newtonsoft.Json).
    // Ie. either have a parameterless constructor and public get/set properties and/or a constructor with parameters for all public properties.
    public class CreateTodoListEvent : EntityEvent<TodoList>
    {
        public CreateTodoListEvent(string id, string title)
        {
            Id = id;
            Title = title;
        }

        public string Id { get; private set; }
        public string Title { get; private set; }

        public override TodoList Apply(TodoList previousState)
        {
            return new TodoList()
            {
                Id = Id,
                Title = Title,
                Items = new List<string>()
            };
        }
    }

    public class CreateTodoEvent : EntityEvent<TodoList>
    {
        public CreateTodoEvent(string todo)
        {
            Todo = todo;
        }

        public string Todo { get; private set; }

        public override TodoList Apply(TodoList previousState)
        {
            var updatedItems = previousState.Items;
            updatedItems.Add(Todo);

            return new TodoList()
            {
                Id = previousState.Id,
                Title = previousState.Title,
                Items = updatedItems
            };
        }
    }
}
