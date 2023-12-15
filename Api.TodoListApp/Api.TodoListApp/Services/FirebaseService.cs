using Google.Cloud.Firestore;
using Lib.TodoListApp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Type;
using Query = Google.Cloud.Firestore.Query;

namespace Api.TodoListApp.Services
{
    internal class FirebaseConfig
    {
        public string type;
        public string project_id;
        public string private_key_id;
        public string client_email;
        public string client_id;
        public string auth_uri;
        public string token_uri;
        public string auth_provider_x509_cert_url;
        public string client_x509_cert_url;
    }

    public class FirebaseService
    {
        private readonly FirestoreDb db;
        private readonly FirebaseConfig CONFIG;
        private const string COLLECTION_NAME = "items";
        public FirebaseService()
        {
            var config = JsonConvert.DeserializeObject<FirebaseConfig>(
                File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory +
                                 Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")));
            this.CONFIG = config;
            this.db = FirestoreDb.Create(config?.project_id);
        }

        public async Task<List<TodoItem>> GetAllItems()
        {
            Query query = db.Collection(COLLECTION_NAME);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            List<TodoItem> items = new();
            foreach (var doc in snapshot.Documents) items.Add(ConvertItem(doc));
            return items;
        }

        public async Task<List<TodoItem>> GetTasks()
        {
            Query query = db.Collection(COLLECTION_NAME).WhereNotEqualTo("IsCompleted", null);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            List<TodoItem> tasks = new();
            foreach (var doc in snapshot.Documents) tasks.Add(ConvertItem(doc));
            return tasks;
        }

        public async Task<List<TodoItem>> GetAppointments()
        {
            Query query = db.Collection(COLLECTION_NAME).WhereNotEqualTo("Attendees", null);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            List<TodoItem> tasks = new();
            foreach (var doc in snapshot.Documents) tasks.Add(ConvertItem(doc));
            return tasks;
        }

        public async Task<List<TodoItem>> GetTasksByCompletion(bool isCompleted)
        {
            Query query = db.Collection(COLLECTION_NAME).WhereEqualTo("IsCompleted", isCompleted);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            List<TodoItem> tasks = new();
            foreach (var doc in snapshot.Documents) tasks.Add(ConvertItem(doc));
            return tasks;
        }

        public async Task<bool> RemoveById(string id)
        {
            try
            {
                var doc = await GetDocumentById(id);
                await doc.Reference.DeleteAsync();
                return true;
            }
            catch (Grpc.Core.RpcException)
            {
                return false;
            }
        }

        public async Task<DocumentSnapshot> GetDocumentById(string id)
        {
            Query query = db.Collection("items").WhereEqualTo("Id", id).Limit(1);
            return (await query.GetSnapshotAsync()).Documents[0];
        }

        public async Task CreateItem(TodoItem item)
        {
            try
            {
                if (item is TodoTask t)
                {
                    t.Deadline = t.Deadline.ToUniversalTime();
                    await db.Collection(COLLECTION_NAME).AddAsync(t);
                }
                else if (item is TodoAppointment a)
                {
                    a.Start = a.Start.ToUniversalTime();
                    a.Stop = a.Stop.ToUniversalTime();
                    await db.Collection(COLLECTION_NAME).AddAsync(a);
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex); 
            }
        }

        public async Task UpdateItem(TodoItem newItem, string id)
        {
            var doc = await GetDocumentById(id);
            if (newItem is TodoTask t)
            {
                t.Deadline = t.Deadline.ToUniversalTime();
                await doc.Reference.SetAsync(t);
            }
            else if (newItem is TodoAppointment a)
            {
                a.Start = a.Start.ToUniversalTime();
                a.Stop = a.Stop.ToUniversalTime();
                await doc.Reference.SetAsync(a);
            }
        }

        private static TodoItem ConvertItem(DocumentSnapshot doc)
        {
            var item = doc.ToDictionary();
            var tempId = (string)(item["Id"] ?? item["id"]);
            if (DocIsTask(item))
            {
                var t = doc.ConvertTo<TodoTask>();
                t.Id = tempId;
                return t;
            }
            if (DocIsAppointment(item))
            {
                var a = doc.ConvertTo<TodoAppointment>();
                a.Id = tempId;
                return a;
            }
            var i = doc.ConvertTo<TodoItem>();
            i.Id = tempId;
            return i;
        }


        private static bool DocIsTask(Dictionary<string, object> item) =>
            (item.ContainsKey("Deadline") || item.ContainsKey("deadline")) && (item.ContainsKey("IsCompleted") || item.ContainsKey("isCompleted"));
        private static bool DocIsAppointment(Dictionary<string, object> item) =>
            (item.ContainsKey("Attendees") || item.ContainsKey("attendees")) && (item.ContainsKey("Start") || item.ContainsKey("start")) && (item.ContainsKey("Stop") || item.ContainsKey("stop"));
    }
}
